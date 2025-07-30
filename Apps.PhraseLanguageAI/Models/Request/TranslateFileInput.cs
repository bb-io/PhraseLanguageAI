using Apps.Appname.Handlers;
using Apps.Appname.Handlers.Static;
using Apps.PhraseLanguageAI.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PhraseLanguageAI.Models.Request
{
    public class TranslateFileInput : ITranslateFileInput
    {
        [Display("Source language")]
        [StaticDataSource(typeof(LanguageCodesHandler))]
        public string SourceLang { get; set; }

        [Display("Target language")]
        [StaticDataSource(typeof(LanguageCodesHandler))]
        public string TargetLanguage { get; set; }

        [Display("File")]
        public FileReference File { get; set; }

        [Display("Language profile ID")]
        [DataSource(typeof(LanguageAiProfilesDataHandler))]
        public string? Uid { get; set; }

        [Display("Output file handling", Description = "Determine the format of the output file. The default Blackbird behavior is to convert to XLIFF for future steps."), StaticDataSource(typeof(ProcessFileFormatHandler))]
        public string? OutputFileHandling { get; set; }

        [Display("File translation strategy", Description = "Select whether to use Phrase Language AI's own file processing capabilities or use Blackbird interoperability mode"), StaticDataSource(typeof(FileTranslationStrategyHandler))]
        public string? FileTranslationStrategy { get; set; }
    }
}
