using Blackbird.Applications.Sdk.Common;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class TranslateTextResponse
    {

        [Display("Source language")]
        public string SourceLang { get; set; }

        [Display("Target language")]
        public string TargetLang { get; set; }

        [Display("Translated text")]
        public string TranslatedTexts { get; set; }

        public TranslateTextResponse(TranslateTextDto response)
        {
            SourceLang = response.SourceLang?.Code ?? string.Empty;
            TargetLang = response.TargetLang?.Code ?? string.Empty;
            TranslatedTexts = response.TranslatedTexts != null
                                ? string.Join(", ", response.TranslatedTexts.Select(t => t.Target))
                                : string.Empty;
        }
    }
}
