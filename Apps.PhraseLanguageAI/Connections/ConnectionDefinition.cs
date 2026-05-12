using Apps.Appname.Constants;
using Apps.PhraseLanguageAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Appname.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = ConnectionTypes.ApiKey,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.UserName) { DisplayName = "User name"},
                new(CredsNames.Password) { DisplayName = "Password" , Sensitive = true },
                new(CredsNames.Url) { DisplayName = "Data center URL",
                Description="Select the base URL according to your Phrase data center",
                DataItems = 
                    [
                        new("https://eu.phrase.com/smt/api/", "EU data center (Production)"),
                        new("https://us.phrase.com/smt/api/", "US data center (Production)"),
                        new("https://eu.phrase-staging.com/smt/api/", "EU data center (Staging)"),
                        new("https://us.phrase-staging.com/smt/api/", "US data center (Staging)"),
                    ]
                },
                new(CredsNames.OrganizationId) { DisplayName = "Organization ID", Description = "Enter the organization ID" }
            }
        },
        new()
        {
            Name = ConnectionTypes.ApiToken,
            DisplayName = "Platform API Token (recommended)",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.Url) 
                { 
                    DisplayName = "Data center URL",
                    Description = "Select the base URL according to your Phrase data center",
                    DataItems = 
                    [
                        new("https://eu.phrase.com/smt/api/", "EU data center (Production)"),
                        new("https://us.phrase.com/smt/api/", "US data center (Production)"),
                        new("https://eu.phrase-staging.com/smt/api/", "EU data center (Staging)"),
                        new("https://us.phrase-staging.com/smt/api/", "US data center (Staging)"),
                    ]
                },
                new(CredsNames.ApiToken) { DisplayName = "API Token", Sensitive = true }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        var providers = values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();
        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            var ct when ConnectionTypes.SupportedConnectionTypes.Contains(ct) => ct,
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };

        providers.Add(new AuthenticationCredentialsProvider(CredsNames.ConnectionType, connectionType));
        return providers;
    }
}