using Newtonsoft.Json;

namespace Apps.PhraseLanguageAI.Models.Response
{
    public class ListTranslationProfilesResponse
    {
        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("content")]
        public List<TranslationProfile> Content { get; set; } = new();

        [JsonProperty("numberOfElements")]
        public int NumberOfElements { get; set; }

        [JsonProperty("totalElements")]
        public int TotalElements { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }

    public class TranslationProfile
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("engines")]
        public List<Engine> Engines { get; set; } = new();

        [JsonProperty("glossaries")]
        public List<Glossary> Glossaries { get; set; } = new();

        [JsonProperty("dateCreated")]
        public string DateCreated { get; set; }
    }

    public class Engine
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("baseName")]
        public string BaseName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("glossarySupported")]
        public bool GlossarySupported { get; set; }
    }

    public class Glossary
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
