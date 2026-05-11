using Newtonsoft.Json;

namespace Apps.PhraseLanguageAI.Models.Auth;

public class AccessTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}