using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class TranslateTextDto
    {
        [JsonProperty("sourceLang")]
        [Display("Source language")]
        public Language? SourceLang { get; set; }

        [JsonProperty("translatedTexts")]
        [Display("Translated text")]
        public List<TranslatedText>? TranslatedTexts { get; set; }

        [JsonProperty("targetLang")]
        [Display("Target language")]
        public Language? TargetLang { get; set; }

        [JsonProperty("consumerId")]
        [Display("Consumer ID")]
        public string? ConsumerId { get; set; }
    }
    public class Language
    {
        [JsonProperty("code")]
        [Display("Language code")]
        public string? Code { get; set; }
    }

    public class TranslatedText
    {
        [JsonProperty("key")]
        [Display("Key")]
        public string? Key { get; set; }

        [JsonProperty("target")]
        [Display("Target")]
        public string? Target { get; set; }
    }
}
