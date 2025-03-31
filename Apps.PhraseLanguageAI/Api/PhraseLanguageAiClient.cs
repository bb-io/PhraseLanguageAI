using System.Linq;
using System.Net;
using Apps.Appname.Constants;
using Apps.PhraseLanguageAI.Models;
using Apps.PhraseLanguageAI.Models.Errors;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Appname.Api;

public class PhraseLanguageAiClient : BlackBirdRestClient
{
    public PhraseLanguageAiClient(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new()
    {
        BaseUrl = GetUri(creds),
        MaxTimeout = 180000
    })
    {
        var userName = creds.First(p => p.KeyName == CredsNames.UserName).Value;
        var password = creds.First(p => p.KeyName == CredsNames.Password).Value;
        var organizationId = creds.First(p => p.KeyName == CredsNames.OrganizationId).Value;

        var token = Login(userName, password, organizationId);

        this.AddDefaultHeader("Authorization", $"Bearer {token}");
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        var response = await ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            const int scalingFactor = 2;
            var retryAfterMilliseconds = 1000;

            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(retryAfterMilliseconds);
                response = await ExecuteAsync(request);

                if (response.IsSuccessStatusCode)
                    break;

                retryAfterMilliseconds *= scalingFactor;
            }
        }

        if (!response.IsSuccessStatusCode)
            throw ConfigureErrorException(response);

        return response;
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        var response = await ExecuteWithErrorHandling(request);
        return JsonConvert.DeserializeObject<T>(response.Content, JsonSettings);
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new PluginApplicationException("You are unauthorized to use Phrase Language AI. Please validate your credentials.");
        }

        try
        {
            var error = JsonConvert.DeserializeObject<PhraseError>(response.Content, JsonSettings);
            if (error.Arguments.Count > 0)
            {
                return new PluginApplicationException(string.Join(' ', error.Arguments.Select(x => x.Value)));
            }
            return new PluginApplicationException(error.Title);
            
        } catch(Exception ex)
        {
            return new PluginApplicationException(response.Content);
        }
        
    }

    private static Uri GetUri(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
    {
        var url = authenticationCredentialsProviders.First(p => p.KeyName == "url").Value;
        return new(url.TrimEnd('/'));
    }

    public string Login(string userName, string password, string organizationId)
    {
        var request = new RestRequest("v1/auth/login", Method.Post);

        request.AddJsonBody(new
        {
            userName,
            password,
            organization = new
            {
                uid = organizationId
            }
        });

        var response = this.ExecuteWithErrorHandling<TokenResponse>(request).GetAwaiter().GetResult();

        if (response?.Token == null)
            throw new PluginApplicationException("No token returned from login response.");

        return response.Token;
    }
}