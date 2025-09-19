using Apps.Appname;
using Apps.Appname.Api;
using Apps.PhraseLanguageAI.Models.Request;
using Apps.PhraseLanguageAI.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.PhraseLanguageAI.Actions;

[ActionList("Translation")]
public class TranslateActions(InvocationContext invocationContext, IFileManagementClient fileManagerClient) : Invocable(invocationContext)
{

    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Translates text from source language to target language")]
    public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextInput input)
    {
        var client = new PhraseLanguageAiClient(invocationContext.AuthenticationCredentialsProviders);

        var request = new RestRequest("v2/textTranslations", Method.Post);

        var body = new Dictionary<string, object>
        {
            { "consumerId", "BLACKBIRD" },
            { "sourceTexts", new[] { new { key = "text", source = input.Text } } },
            { "targetLang", new { code = input.TargetLanguage } }
        };

        if (!string.IsNullOrEmpty(input.Uid))
        {
            body.Add("mtSettings", new { profile = new { uid = input.Uid } });
        }

        if (!string.IsNullOrEmpty(input.SourceLang))
        {
            body.Add("sourceLang", new { code = input.SourceLang });
        }

        request.AddJsonBody(body);

        var response = await client.ExecuteWithErrorHandling<TranslateTextDto>(request);
        return new TranslateTextResponse(response);
    }

    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate", Description = "Translates file with action type MT_GENERIC_PRETRANSLATE")]
    public async Task<FileResponse> TranslateFileGenericPretranslate([ActionParameter] TranslateFileInput input,
        [ActionParameter] TransMemoriesConfig? memories)
    {
        var strategy = input.FileTranslationStrategy?.ToLowerInvariant() ?? "plai";

        if (strategy == "blackbird")
        {
            return await TranslateWithBlackbird(input);
        }
        else // "plai"
        {
            return await TranslateWithPhraseLanguageAINative(input, memories);
        }
    }

    private async Task<FileResponse> TranslateWithPhraseLanguageAINative(TranslateFileInput input, TransMemoriesConfig? memories)
    {
        var originalFileName = input.File.Name;

        var uploadResponse = await UploadFileForTranslation(input, "MT_GENERIC_PRETRANSLATE", memories);
        var uid = uploadResponse.Uid;
        if (string.IsNullOrEmpty(uid))
            throw new PluginApplicationException("No UID returned after file upload.");

        while (true)
        {
            await Task.Delay(5000);
            var statusResponse = await GetFileTranslationStatus(uid);

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

        var downloadedFile = await DownloadFileTranslation(uid, "MT_GENERIC_PRETRANSLATE", input.TargetLanguage, originalFileName);
        downloadedFile.Name = originalFileName;

        return new FileResponse { File = downloadedFile, Uid = uid };
    }

    private async Task<FileResponse> TranslateWithBlackbird(TranslateFileInput input)
    {
        try
        {
            using var stream = await fileManagerClient.DownloadAsync(input.File);
            var content = await Transformation.Parse(stream, input.File.Name);

            return await HandleInteroperableTransformation(content, input);
        }
        catch (Exception e) when (e.Message.Contains("not supported"))
        {
            throw new PluginMisconfigurationException(
                "The file format is not supported by the Blackbird interoperable setting. " +
                "Try setting the file translation strategy to Phrase Language AI native.");
        }
    }

    private async Task<FileResponse> HandleInteroperableTransformation(Transformation content, TranslateFileInput input)
    {
        content.SourceLanguage ??= input.SourceLang;
        content.TargetLanguage ??= input.TargetLanguage;

        if (content.SourceLanguage == null || content.TargetLanguage == null)
            throw new PluginMisconfigurationException("Source or target language not defined.");

        async Task<IEnumerable<string>> BatchTranslate(IEnumerable<Segment> batch)
        {
            var request = new RestRequest("v2/textTranslations", Method.Post);

            var body = new Dictionary<string, object>
                {
                    { "consumerId", "BLACKBIRD" },
                    { "sourceTexts", batch.Select(s => new { key = s.Id, source = s.GetSource() }).ToArray() },
                    { "targetLang", new { code = input.TargetLanguage ?? content.TargetLanguage } }
                };

            if (!string.IsNullOrEmpty(input.Uid))
            {
                body.Add("mtSettings", new { profile = new { uid = input.Uid } });
            }

            if (!string.IsNullOrEmpty(input.SourceLang))
            {
                body.Add("sourceLang", new { code = input.SourceLang });
            }

            request.AddJsonBody(body);

            var client = new PhraseLanguageAiClient(InvocationContext.AuthenticationCredentialsProviders);

            var response = await client.ExecuteWithErrorHandling<TranslateTextDto>(request);
            return response.TranslatedTexts.Select(t => t.Target);
        }

        var segments = content.GetSegments()
            .Where(s => !s.IsIgnorbale && s.IsInitial)
            .ToList();

        var segmentTranslations = await segments.Batch(100).Process(BatchTranslate);

        foreach (var (segment, translatedText) in segmentTranslations)
        {
            if (!string.IsNullOrEmpty(translatedText))
            {
                segment.SetTarget(translatedText);
                segment.State = SegmentState.Translated;
            }
        }

        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            var outFile = await fileManagerClient.UploadAsync(targetContent.Serialize().ToStream(),targetContent.OriginalMediaType,targetContent.OriginalName);
            return new FileResponse { File = outFile, Uid = input.Uid };
        }

        content.SourceLanguage ??= input.SourceLang;
        content.TargetLanguage ??= input.TargetLanguage;
        var xliffFile = await fileManagerClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff,content.XliffFileName);
        return new FileResponse { File = xliffFile, Uid = input.Uid };
    }


    [Action("Translate file with quality estimation", Description = "Translate file with quality estimation")]
    public async Task<TranslationScoreResponse> TranslateFileWithQualityEstimation([ActionParameter] TranslateFileInput input,
        [ActionParameter] TransMemoriesConfig? memories)
    {
        var originalFileName = input.File.Name;

        var uploadResponse = await UploadFileForTranslation(input, "QUALITY_ESTIMATION", memories);
        var uid = uploadResponse.Uid;
        if (string.IsNullOrEmpty(uid))
            throw new PluginApplicationException("No UID returned after file upload.");

        while (true)
        {
            await Task.Delay(5000);
            var statusResponse = await GetFileTranslationStatus(uid);

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

        var score = await GetTranslationScore(uid, "QUALITY_ESTIMATION", input.TargetLanguage);
        var file = await DownloadFileTranslation(uid, "MT_GENERIC_PRETRANSLATE", input.TargetLanguage, originalFileName);
        return new TranslationScoreResponse { Score = score.Score, Uid = uid, File=file };
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

    public async Task<FileTranslationResponse> UploadFileForTranslation(TranslateFileInput input, string actionType, TransMemoriesConfig? memories)
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

        var metadata = new Dictionary<string, object>
        {
            { "targetLangs", new[] { new { code = input.TargetLanguage } } },
            { "actionTypes", new[] { actionType } },
        };

        if (memories != null)
        {
            var transMemoriesConfig = new[]
            {
                new {
                    targetLang = new
                    {
                        code = memories.TargetLanguage
                    },
                    transMemories = new[]
                    {
                        new
                        {
                            transMemory = new
                            {
                                uid = memories.TransMemoryUid
                            },
                            tmSourceLocale = new
                            {
                                code = memories.TmSourceLanguage
                            },
                            tmTargetLocale = new
                            {
                                code = memories.TmTargetLanguage
                            }
                        }
                    }
                }
            };

            metadata.Add("transMemoriesConfig", transMemoriesConfig);
        }

        if (!string.IsNullOrEmpty(input.SourceLang))
        {
            metadata.Add("sourceLang", new { code = input.SourceLang });
        }

        if (!string.IsNullOrEmpty(input.Uid))
        {
            metadata.Add("mtSettings", new
            {
                usePhraseMTSettings = true,
                profile = new { uid = input.Uid }
            });
        }

        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);

        request.AddFile("metadata", metadataBytes, "metadata.json", "application/json");

        var response = await client.ExecuteWithErrorHandling<FileTranslationResponse>(request);
        return response;
    }
}