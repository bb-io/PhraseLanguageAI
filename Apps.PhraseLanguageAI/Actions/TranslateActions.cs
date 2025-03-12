using System.Threading;
using Apps.Appname.Api;
using Apps.PhraseLanguageAI.Models.Request;
using Apps.PhraseLanguageAI.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Appname.Actions;

[ActionList]
public class TranslateActions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [Action("Search list of language profiles", Description = "Search list of language profiles")]
    public async Task<ListTranslationProfilesResponse> ListTranslationProfiles()
    {
        var client = new PhraseLanguageAiClient(invocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest("v1/translationProfiles", Method.Get);

        var response = await client.ExecuteWithErrorHandling<ListTranslationProfilesResponse>(request);
        return response;
    }

    [Action("Translate text", Description = "Translates text from source language to target language")]
    public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextInput input)
    {
        var client = new PhraseLanguageAiClient(invocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest("v2/textTranslations", Method.Post);

        request.AddJsonBody(new
        {
            consumerId = "BLACKBIRD",
            sourceTexts = new[]
            {
                new
                {
                    key = "text",
                    source  = input.Text
                }
            },
            sourceLang = new
            {
                code = input.SourceLang
            },
            targetLang = new
            {
                code = input.TargetLang
            }
        });

        var response = await client.ExecuteWithErrorHandling<TranslateTextDto>(request);
        return new TranslateTextResponse(response);
    }
}