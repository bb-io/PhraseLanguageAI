using Apps.Appname.Handlers;
using Apps.Appname.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PhraseLanguageAI.Models.Request
{
    public class TranslateFileInput
    {
        [Display("Source language")]
        [StaticDataSource(typeof(LanguageCodesHandler))]
        public string? SourceLang { get; set; }

        [Display("Target language")]
        [StaticDataSource(typeof(LanguageCodesHandler))]
        public string TargetLang { get; set; }

        [Display("File")]
        public FileReference File { get; set; }

        [Display("Language profile ID")]
        [DataSource(typeof(LanguageAiProfilesDataHandler))]
        public string? Uid { get; set; }
    }
}
