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
using System.Diagnostics;
using static Blackbird.Applications.SDK.Blueprints.BlueprintIcons;

namespace Apps.Appname.Actions;

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
        [ActionParameter] TransMemoriesConfig memories)
    {
        var swTotal = Stopwatch.StartNew();
        InvocationContext.Logger?.LogInformation("[PLAI] Translate.start | strategy={0} | file='{1}' | src={2} | trg={3}",
            [input.FileTranslationStrategy ?? "plai", input.File?.Name, input.SourceLang ?? "-", input.TargetLanguage ?? "-"]);

        try
        {
            var strategy = input.FileTranslationStrategy?.ToLowerInvariant() ?? "blackbird";

            if (strategy == "blackbird")
            {
                try
                {
                    using var stream = await fileManagerClient.DownloadAsync(input.File);
                    var content = await Transformation.Parse(stream, input.File.Name);

                    return await HandleInteroperableTransformation(content, input);
                }
                catch (NotImplementedException)
                {
                    return await TranslateWithPhraseLanguageAINative(input, memories);
                }
                
            } 
            else
            {
                return await TranslateWithPhraseLanguageAINative(input, memories);
            }
        }
        finally
        {
            swTotal.Stop();
            InvocationContext.Logger?.LogInformation("[PLAI] translate.end | totalMs={0}",new object?[] { swTotal.ElapsedMilliseconds });
        }
    }

    private async Task<FileResponse> TranslateWithPhraseLanguageAINative(TranslateFileInput input, TransMemoriesConfig memories)
    {
        var originalFileName = input.File.Name;

        var swUpload = Stopwatch.StartNew();
        var uploadResponse = await UploadFileForTranslation(input, "MT_GENERIC_PRETRANSLATE", memories);
        swUpload.Stop();

        var uid = uploadResponse.Uid;
        if (string.IsNullOrEmpty(uid))
            throw new PluginApplicationException("No UID returned after file upload.");

        InvocationContext.Logger?.LogInformation("[PLAI] upload.end | uid={0} | elapsedMs={1}",new object?[] { uid, swUpload.ElapsedMilliseconds });

        var pollTimes = new List<long>();
        var polls = 0;

        while (true)
        {
            polls++;
            await Task.Delay(2000);  // Used for large files, and to prevent many requests

            var swPoll = Stopwatch.StartNew();
            var statusResponse = await GetFileTranslationStatus(uid);
            swPoll.Stop();
            pollTimes.Add(swPoll.ElapsedMilliseconds);    

            if (statusResponse.Actions != null && statusResponse.Actions.Any(a => a.Results != null && a.Results.Any(r => r.Status == "ERROR")))
            {              
                throw new PluginApplicationException($"File translation failed. status=[ERROR]");
            }

            bool allOk = statusResponse.Actions != null &&
                         statusResponse.Actions.All(a => a.Results != null && a.Results.All(r => r.Status == "OK"));
            if (allOk)
                break;

            InvocationContext.Logger?.LogInformation("[PLAI] poll | uid={0} | i={1} | elapsedMs={2}",new object?[] { uid, polls, swPoll.ElapsedMilliseconds});
        }

        var swDownload = Stopwatch.StartNew();
        var downloadedFile = await DownloadFileTranslation(uid, "MT_GENERIC_PRETRANSLATE", input.TargetLanguage, originalFileName);
        swDownload.Stop();

        var pollTotal = pollTimes.Sum();
        var pollAvg = pollTimes.Count > 0 ? (long)pollTimes.Average() : 0L;
        var pollMax = pollTimes.Count > 0 ? pollTimes.Max() : 0L;

        InvocationContext.Logger?.LogInformation("[PLAI] summary | uid={0} | polls={1} | pollTotalMs={2} | pollAvgMs={3} | pollMaxMs={4} | uploadMs={5} | downloadMs={6} | src={7} | trg={8} | hasTM={9} | hasMT={10}",
            new object?[] { uid, polls, pollTotal, pollAvg, pollMax, swUpload.ElapsedMilliseconds, swDownload.ElapsedMilliseconds,
            input.SourceLang ?? "-", input.TargetLanguage ?? "-", memories?.TransMemoryUid!=null, input?.Uid!=null });

        downloadedFile.Name = originalFileName;

        return new FileResponse { File = downloadedFile, Uid = uid };
    }

    private async Task<FileResponse> HandleInteroperableTransformation(Transformation content, TranslateFileInput input)
    {
        content.SourceLanguage ??= input.SourceLang;
        content.TargetLanguage ??= input.TargetLanguage;

        if (content.SourceLanguage == null || content.TargetLanguage == null)
            throw new PluginMisconfigurationException("Source or target language not defined.");

        async Task<IEnumerable<TranslatedText>> BatchTranslate(IEnumerable<(Unit Unit, Segment Segment)> batch)
        {
            var request = new RestRequest("v2/textTranslations", Method.Post);

            var idSegments = batch.Select((x, i) => new { Id = i + 1, Value = x }).ToDictionary(x => x.Id.ToString(), x => x.Value.Segment);

            var body = new Dictionary<string, object>
                {
                    { "consumerId", "BLACKBIRD" },
                    { "sourceTexts", idSegments.Select(s => new { key = s.Key, source = s.Value.GetSource() }).ToArray() },
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
            return response.TranslatedTexts!;
        }

        var units = content.GetUnits().Where(x => x.IsInitial);
        var processedBatches = await units.Batch(6).ProcessParallel(BatchTranslate);

        foreach (var (unit, results) in processedBatches)
        {
            foreach (var (segment, translation) in results)
            {
                var shouldTranslateFromState = segment.State == null || segment.State == SegmentState.Initial;
                if (!shouldTranslateFromState || string.IsNullOrEmpty(translation.Target))
                {
                    continue;
                }

                if (segment.GetTarget() != translation.Target)
                {
                    segment.SetTarget(translation.Target);
                    segment.State = SegmentState.Translated;
                }
            }

            var uidPart = string.IsNullOrEmpty(input.Uid) ? "" : $"-{input.Uid}";
            unit.Provenance.Translation.Tool = "plai" + uidPart;
            unit.Provenance.Translation.ToolReference = "https://phrase.com/platform/ai/";
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
    public async Task<TranslationScoreResponse> TranslateFileWithQualityEstimation([ActionParameter] TranslateWithQualityRequest input,
        [ActionParameter] TransMemoriesConfig? memories)
    {
        var originalFileName = input.File.Name;

        var uploadResponse = await UploadFileForTranslation(
            new TranslateFileInput {SourceLang = input.SourceLang ,
                TargetLanguage = input.TargetLanguage,
                File = input.File,
                Uid = input.Uid
            }, "QUALITY_ESTIMATION", memories);
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

    public async Task<FileTranslationResponse> UploadFileForTranslation(TranslateFileInput input, string actionType, TransMemoriesConfig memories)
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

        if (memories.TransMemoryUid != null)
        {
            var transMemoriesConfig = new[]
            {
                new {
                    targetLang = new
                    {
                        code = input.TargetLanguage
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
                                code = input.SourceLang
                            },
                            tmTargetLocale = new
                            {
                                code = input.TargetLanguage
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