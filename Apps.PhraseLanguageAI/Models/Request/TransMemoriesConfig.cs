using Apps.Appname.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.PhraseLanguageAI.Models.Request;

public class TransMemoriesConfig
{
    [Display("Target language")]
    [StaticDataSource(typeof(LanguageCodesHandler))]
    public string? TargetLanguage { get; set; }

    [Display("Translation memory UID")]
    public string? TransMemoryUid { get; set; }

    [Display("Translation memory source language")]
    [StaticDataSource(typeof(LanguageCodesHandler))]
    public string? TmSourceLanguage { get; set; }

    [Display("Translation memory target language")]
    [StaticDataSource(typeof(LanguageCodesHandler))]
    public string? TmTargetLanguage { get; set; }
}
