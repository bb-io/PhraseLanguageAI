using Apps.Appname.Handlers;
using Apps.Appname.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PhraseLanguageAI.Models.Request
{
    public class TranslateTextInput : ITranslateTextInput
    {
        public string Text { get; set; }

        [Display("Source language", Description = "Optional and can be autodetected only if the text is longer than 50 characters.")]
        [StaticDataSource(typeof(LanguageCodesHandler))]
        public string? SourceLang { get; set; }


        [Display("Target language")]
        [StaticDataSource(typeof(LanguageCodesHandler))]
        public string TargetLanguage { get; set; }

        [Display("Language profile ID")]
        [DataSource(typeof(LanguageAiProfilesDataHandler))]
        public string? Uid { get; set; }
    }
}
