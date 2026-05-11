namespace Apps.PhraseLanguageAI.Constants;

public static class ConnectionTypes
{
    public const string ApiToken = "ApiToken";
    public const string ApiKey = "Developer API key";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [ApiToken, ApiKey];
}