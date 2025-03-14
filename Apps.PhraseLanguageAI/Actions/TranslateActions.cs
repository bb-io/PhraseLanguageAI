using Apps.Appname.Api;
using Apps.PhraseLanguageAI.Models.Request;
using Apps.PhraseLanguageAI.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Appname.Actions;

[ActionList]
public class TranslateActions(InvocationContext invocationContext, IFileManagementClient fileManagerClient) : Invocable(invocationContext)
{

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


    [Action("Translate file", Description = "Translates file with action type MT_GENERIC_PRETRANSLATE")]
    public async Task<FileResponse> TranslateFileGenericPretranslate([ActionParameter] TranslateFileInput input)
    {
        var originalFileName = input.File.Name;

        var uploadResponse = await UploadFileForTranslation(input, "MT_GENERIC_PRETRANSLATE");
        var uid = uploadResponse.Uid;
        if (string.IsNullOrEmpty(uid))
            throw new PluginApplicationException("No UID returned after file upload.");

        while (true)
        {
            await Task.Delay(5000);
            var statusResponse = await GetFileTranslationStatus(uid);

            //Console.WriteLine("Status Response JSON: " + JsonConvert.SerializeObject(statusResponse, Formatting.Indented));

            if (statusResponse.Actions != null &&
                statusResponse.Actions.Any(a => a.Results != null && a.Results.Any(r => r.Status == "FAILED")))
            {
                throw new PluginApplicationException("File translation failed (status=FAILED).");
            }

            bool allOk = statusResponse.Actions != null &&
                         statusResponse.Actions.All(a => a.Results != null && a.Results.All(r => r.Status == "OK"));
            if (allOk)
                break;
        }

        var downloadedFile = await DownloadFileTranslation(uid, "MT_GENERIC_PRETRANSLATE", input.TargetLang, originalFileName);
        downloadedFile.Name = originalFileName;

        return new FileResponse { File = downloadedFile, Uid = uid, };
    }

    [Action("Get quality estimation", Description = "Get quality estimation of translation file")]
    public async Task<TranslationScoreResponse> TranslateFileWithQualityEstimation([ActionParameter] TranslateFileInput input)
    {
        var originalFileName = input.File.Name;

        var uploadResponse = await UploadFileForTranslation(input, "QUALITY_ESTIMATION");
        var uid = uploadResponse.Uid;
        if (string.IsNullOrEmpty(uid))
            throw new PluginApplicationException("No UID returned after file upload.");

        while (true)
        {
            await Task.Delay(5000);
            var statusResponse = await GetFileTranslationStatus(uid);

            //Console.WriteLine("Status Response JSON: " + JsonConvert.SerializeObject(statusResponse, Formatting.Indented));

            if (statusResponse.Actions != null &&
                statusResponse.Actions.Any(a => a.Results != null && a.Results.Any(r => r.Status == "FAILED")))
            {
                throw new PluginApplicationException("File estimation failed (status=FAILED).");
            }

            bool allOk = statusResponse.Actions != null &&
                         statusResponse.Actions.All(a => a.Results != null && a.Results.All(r => r.Status == "OK"));
            if (allOk)
                break;
        }

        var score = await GetTranslationScore(uid, "QUALITY_ESTIMATION", input.TargetLang);

        return new TranslationScoreResponse { Score = score.Score, Uid = uid };
    }


    public async Task<FileTranslationResponse> GetFileTranslationStatus(string fileTranslationUid)
    {
        var client = new PhraseLanguageAiClient(InvocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest($"v1/fileTranslations/{fileTranslationUid}", Method.Get);

        var response = await client.ExecuteWithErrorHandling<FileTranslationResponse>(request);
        return response;
    }

    public async Task<TranslationScoreResponse> GetTranslationScore(string uid, string actionType, string language)
    {
        var client = new PhraseLanguageAiClient(InvocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest($"/v1/fileTranslations/{uid}/{actionType}/{language}", Method.Get);

        request.AddHeader("Accept", "application/json");

        var restResponse = await client.ExecuteWithErrorHandling(request);
        if (!restResponse.IsSuccessful)
            throw new PluginApplicationException($"Error: {restResponse.Content}");

        var scoreResponse = JsonConvert.DeserializeObject<TranslationScoreResponse>(restResponse.Content);
        return new TranslationScoreResponse { Score = scoreResponse.Score, Uid=uid };
    }


    public async Task<FileReference> DownloadFileTranslation(string uid, string actionType, string language, string originalFileName)
    {
        var client = new PhraseLanguageAiClient(InvocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest($"/v1/fileTranslations/{uid}/{actionType}/{language}", Method.Get);


        request.AddHeader("Accept", "application/octet-stream");

        var restResponse = await client.ExecuteWithErrorHandling(request);
        if (!restResponse.IsSuccessful)
            throw new PluginApplicationException($"Error: {restResponse.Content}");

        var contentType = restResponse.ContentType?.ToLowerInvariant() ?? "";
        if (!contentType.StartsWith("application/octet-stream"))
            throw new PluginApplicationException($"Server didn't return application/octet-stream. Content-Type: {contentType}");

        var fileBytes = restResponse.RawBytes ?? Array.Empty<byte>();

        var contentDisposition = restResponse.Headers
            .FirstOrDefault(x => x.Name.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase))
            ?.Value?.ToString();
        string fileName = originalFileName;
        if (!string.IsNullOrEmpty(contentDisposition))
        {
            var match = System.Text.RegularExpressions.Regex.Match(contentDisposition, @"filename=""([^""]+)""");
            if (match.Success)
                fileName = match.Groups[1].Value;
        }

        var localRef = new FileReference
        {
            Name = fileName,
            ContentType = contentType
        };


        using var ms = new MemoryStream(fileBytes);
        var uploaded = await fileManagerClient.UploadAsync(ms, localRef.ContentType, localRef.Name);
        return uploaded;
    }

    public async Task<FileTranslationResponse> UploadFileForTranslation(TranslateFileInput input, string actionType)
    {
        var client = new PhraseLanguageAiClient(InvocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest("v1/fileTranslations", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        var fileStream = await fileManagerClient.DownloadAsync(input.File);

        var fileName = string.IsNullOrEmpty(input.File.Name) ? "file" : input.File.Name;
        var contentType = string.IsNullOrEmpty(input.File.ContentType)
            ? "application/octet-stream"
            : input.File.ContentType;

        request.AddFile("file", () => fileStream, fileName, contentType);

        var metadata = new
        {
            sourceLang = new { code = input.SourceLang },
            targetLangs = new[] { new { code = input.TargetLang } },
            actionTypes = new[] { actionType },
        };
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);

        request.AddFile("metadata", metadataBytes, "metadata.json", "application/json");

        var response = await client.ExecuteWithErrorHandling<FileTranslationResponse>(request);
        return response;
    }
}