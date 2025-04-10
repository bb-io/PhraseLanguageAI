using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class TranslationScoreResponse
    {
        [JsonProperty("score")]
        public double Score { get; set; }

        [Display("Operation ID")]
        public string Uid { get; set; }

        [Display("File")]
        public FileReference File { get; set; }
    }
}
