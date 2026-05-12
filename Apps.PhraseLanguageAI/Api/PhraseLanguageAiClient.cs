using System.Net;
using Apps.Appname.Constants;
using Apps.PhraseLanguageAI.Constants;
using Apps.PhraseLanguageAI.Models.Auth;
using Apps.PhraseLanguageAI.Models.Errors;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Appname.Api;

public class PhraseLanguageAiClient(IEnumerable<AuthenticationCredentialsProvider> creds) : BlackBirdRestClient(new()
{
    BaseUrl = GetUri(creds),
    MaxTimeout = MaxTimeout,
    ThrowOnAnyError = false
})
{
    private const int MaxTimeout = 900000;

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        var token = await GetAuthenticationToken();
        this.AddDefaultHeader("Authorization", token);
        
        var response = await ExecuteAsync(request);

        if (response.StatusCode is 
            HttpStatusCode.TooManyRequests or 
            HttpStatusCode.ServiceUnavailable or 
            HttpStatusCode.InternalServerError or 
            HttpStatusCode.RequestTimeout ||
            response.ResponseStatus == ResponseStatus.TimedOut)
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

    private async Task<string> GetAuthenticationToken()
    {
        string token = string.Empty;
        
        switch (creds.Get(CredsNames.ConnectionType).Value)
        {
            case ConnectionTypes.ApiKey:
                var userName = creds.First(p => p.KeyName == CredsNames.UserName).Value;
                var password = creds.First(p => p.KeyName == CredsNames.Password).Value;
                var organizationId = creds.First(p => p.KeyName == CredsNames.OrganizationId).Value;
                var authorizeCredsResult = await AuthorizeUsingCredentials(userName, password, organizationId);
                token = $"Bearer {authorizeCredsResult}";
                break;
            case ConnectionTypes.ApiToken:
                var apiToken = creds.Get(CredsNames.ApiToken).Value;
                var baseUrl = creds.Get(CredsNames.Url).Value;
                var jwt = await AuthorizeUsingApiToken(baseUrl, apiToken);
                token = $"Bearer {jwt}";
                break;
        }

        return token;
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        var response = await ExecuteWithErrorHandling(request);
        return JsonConvert.DeserializeObject<T>(response.Content, JsonSettings);
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.ResponseStatus == ResponseStatus.TimedOut)
        {
            throw new PluginApplicationException("The request to Phrase Language AI timed out. The operation took longer than the allowed time limit. Please try again later");
        }

        if (string.IsNullOrEmpty(response.Content))
        {
            if (string.IsNullOrEmpty(response.ErrorMessage))
            {
                return new PluginApplicationException($"Request failed with status code {response.StatusCode}. {response.StatusDescription}");
            }
            
            return new PluginApplicationException(response.ErrorMessage);
        }
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new PluginApplicationException("Access to Phrase Language AI or the language profile is restricted based on your current permissions. Please check and validate your credentials");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new PluginApplicationException("Access to Phrase Language AI or the language profile is restricted based on your current permissions. Please check and validate your credentials");
        }
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            throw new PluginApplicationException("Your request contains invalid input or the server encountered an error. Please review your data and try again");
        }

        try
        {
            var error = JsonConvert.DeserializeObject<PhraseError>(response.Content, JsonSettings);
            if (error?.Arguments.Count > 0)
            {
                return new PluginApplicationException(string.Join(' ', error.Arguments.Select(x => x.Value)));
            }
            if (!string.IsNullOrEmpty(error?.Detail))
            {
                return new PluginApplicationException(error.Detail);
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
    
    private async Task<string> AuthorizeUsingApiToken(string baseUrl, string apiToken)
    {
        string oauthBaseUrl = baseUrl switch
        {
            not null when baseUrl.StartsWith("https://eu.phrase") => "https://eu.phrase.com/idm/oauth/token",
            not null when baseUrl.StartsWith("https://us.phrase") => "https://us.phrase.com/idm/oauth/token",
            _ => throw new Exception($"Unsupported base URL for API token exchange: {baseUrl}")
        };

        using var tokenClient = new RestClient();
        
        var request = new RestRequest(oauthBaseUrl, Method.Post);
        request.AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange", ParameterType.GetOrPost);
        request.AddParameter("subject_token", apiToken, ParameterType.GetOrPost);
        request.AddParameter("subject_token_type", "urn:phrase:params:oauth:token-type:api_token", ParameterType.GetOrPost);
        request.AddParameter("requested_token_type", "urn:ietf:params:oauth:token-type:access_token", ParameterType.GetOrPost);
        
        var response = await ExecuteWithErrorHandling<AccessTokenResponse>(request);
        if (response?.AccessToken == null)
            throw new PluginApplicationException("No token returned from login response.");

        return response.AccessToken;
    }

    public async Task<string> AuthorizeUsingCredentials(string userName, string password, string organizationId)
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

        var response = await ExecuteWithErrorHandling<TokenResponse>(request);

        if (response?.Token == null)
            throw new PluginApplicationException("No token returned from login response.");

        return response.Token;
    }
}