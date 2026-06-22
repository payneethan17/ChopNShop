using System.Text.Json;

namespace ChopNShop.Services
{
    public class PexelsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public PexelsService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Pexels:ApiKey"] ?? "";
        }

        public async Task<List<PhotoResult>> SearchPhotosAsync(string query)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page=12");
            request.Headers.Add("Authorization", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<PexelsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data?.Photos?.Select(p => new PhotoResult
            {
                ThumbnailUrl = p.Src.Medium,
                FullUrl = p.Src.Original
            }).ToList() ?? new List<PhotoResult>();
        }
    }

    public class PhotoResult
    {
        public string ThumbnailUrl { get; set; } = "";
        public string FullUrl { get; set; } = "";
    }

    public class PexelsResponse
    {
        public List<PexelsPhoto>? Photos { get; set; }
    }

    public class PexelsPhoto
    {
        public PexelsSrc Src { get; set; } = new();
    }

    public class PexelsSrc
    {
        public string Medium { get; set; } = "";
        public string Original { get; set; } = "";
    }
}