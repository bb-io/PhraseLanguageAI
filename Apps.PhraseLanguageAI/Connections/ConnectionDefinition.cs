using Apps.Appname.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Appname.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.UserName) { DisplayName = "User name"},
                new(CredsNames.Password) { DisplayName = "Password" , Sensitive=true},
                new(CredsNames.Url) { DisplayName = "Url",
                Description="Select the base URL according to your Phrase data center",
                 DataItems= [new("https://eu.phrase-staging.com/smt/api/", "EU data center(Staging)"),
                             new("https://us.phrase-staging.com/smt/api/", "US data center(Staging)"),
                             new("https://eu.phrase.com/smt/api/", "EU data center(Production)"),
                             new("https://us.phrase.com/smt/api/", "US data center(Production)"),
                 ]
                },
                 new(CredsNames.OrganizationId) { DisplayName = "Organization ID", Description = "Enter the organization ID" }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {

        var userName = values.First(v => v.Key == CredsNames.UserName);
        yield return new AuthenticationCredentialsProvider(
            userName.Key,
            userName.Value
        );

        var password = values.First(v => v.Key == CredsNames.Password);
        yield return new AuthenticationCredentialsProvider(
            password.Key,
            password.Value
        );

        var url = values.First(v => v.Key == CredsNames.Url);
        yield return new AuthenticationCredentialsProvider(
             url.Key,
             url.Value
        );
        var projectId = values.First(v => v.Key == CredsNames.OrganizationId);
        yield return new AuthenticationCredentialsProvider(
            projectId.Key,
            projectId.Value
        );
    }
}