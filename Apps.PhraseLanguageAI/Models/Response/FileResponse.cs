using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class FileResponse : ITranslateFileOutput
    {
        public FileReference File { get; set; }

        [Display("ID of the operation")]
        public string Uid { get; set; }
    }
}
