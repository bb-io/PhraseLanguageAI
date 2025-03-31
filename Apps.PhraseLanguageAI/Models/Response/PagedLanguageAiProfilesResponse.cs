namespace Apps.PhraseLanguageAI.Models.Response
{
    public class PagedLanguageAiProfilesResponse
    {
        public List<LanguageAiProfile> Content { get; set; }
        public int NumberOfElements { get; set; }
        public int TotalPages { get; set; }
    }
    public class LanguageAiProfile
    {
        public string Uid { get; set; }
        public string Name { get; set; }
    }
}
