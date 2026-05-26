using Apps.Appname.Api;
using Apps.PhraseLanguageAI.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Appname.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new PhraseLanguageAiClient(authenticationCredentialsProviders);

            var request = new RestRequest("v1/translationProfiles", Method.Get);
            var response = await client.ExecuteWithErrorHandling<PagedLanguageAiProfilesResponse>(request);

            return new ConnectionValidationResponse
            {
                IsValid = true
            };

        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }

    }
}