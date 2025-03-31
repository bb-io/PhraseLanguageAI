using Apps.PhraseLanguageAI.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Appname.Handlers;
public class LanguageAiProfilesDataHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceHandler
{

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
       CancellationToken cancellationToken)
    {
        var request = new RestRequest("v1/translationProfiles", Method.Get);
        var response = await Client.ExecuteWithErrorHandling<PagedLanguageAiProfilesResponse>(request);
        var profiles = response.Content ?? new List<LanguageAiProfile>();
        var filtered = profiles
            .Where(x => string.IsNullOrEmpty(context.SearchString)
                        || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase));

        return filtered.ToDictionary(x => x.Uid, x => x.Name);

    }
}
