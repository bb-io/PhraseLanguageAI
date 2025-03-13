using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PhraseLanguageAI.Models.Request
{
    public class TranslateFileInput
    {
        [Display("Source language")]
        public string SourceLang { get; set; }

        [Display("Target language")]
        public string TargetLang { get; set; }

        [Display("File")]
        public FileReference File { get; set; }
    }
}
