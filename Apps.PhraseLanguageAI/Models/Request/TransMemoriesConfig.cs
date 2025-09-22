using Blackbird.Applications.Sdk.Common;

namespace Apps.PhraseLanguageAI.Models.Request;

public class TransMemoriesConfig
{
    [Display("Translation memory UID")]
    public string? TransMemoryUid { get; set; }
}
