using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class FileResponse
    {
        public FileReference File { get; set; }

        [Display("ID of the operation")]
        public string Uid { get; set; }
    }
}
