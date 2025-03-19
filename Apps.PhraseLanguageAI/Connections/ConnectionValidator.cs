using Apps.Appname.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

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

            var userName = authenticationCredentialsProviders
               .First(a => a.KeyName == "userName").Value;
            var password = authenticationCredentialsProviders
                .First(a => a.KeyName == "password").Value;

            var projectId = authenticationCredentialsProviders
               .First(a => a.KeyName == "uid").Value;

            client.Login(userName, password, projectId);


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