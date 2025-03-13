using Newtonsoft.Json;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class FileTranslationResponse
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("actions")]
        public IEnumerable<FileTranslationAction> Actions { get; set; }

        [JsonProperty("metadata")]
        public FileTranslationMetadata Metadata { get; set; }
    }

    public class FileTranslationAction
    {
        [JsonProperty("actionType")]
        public string ActionType { get; set; }

        [JsonProperty("results")]
        public IEnumerable<FileTranslationResult> Results { get; set; }
    }

    public class FileTranslationResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sourceLang")]
        public string SourceLang { get; set; }

        [JsonProperty("targetLang")]
        public string TargetLang { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class FileTranslationMetadata
    {
        [JsonProperty("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonProperty("actionTypes")]
        public IEnumerable<string> ActionTypes { get; set; }
    }
}
