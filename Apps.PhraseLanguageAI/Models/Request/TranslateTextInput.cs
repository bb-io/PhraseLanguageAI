using Blackbird.Applications.Sdk.Common;

namespace Apps.PhraseLanguageAI.Models.Request
{
    public class TranslateTextInput
    {
        public string Text { get; set; }

        [Display("Source language")]
        public string SourceLang { get; set; }


        [Display("Target language")]
        public string TargetLang { get; set; }
    }
}
