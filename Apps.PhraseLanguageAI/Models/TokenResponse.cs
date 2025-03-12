using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Apps.PhraseLanguageAI.Models
{
    public class TokenResponse
    {
        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("tokenType")]
        public string? TokenType { get; set; }

        [JsonProperty("expires")]
        public string? Expires { get; set; }
    }
}
