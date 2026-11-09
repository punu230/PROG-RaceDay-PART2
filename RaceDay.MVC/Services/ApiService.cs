using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace RaceDay.MVC.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = config["ApiSettings:BaseUrl"]!;
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return default;
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
        {
            AddAuthHeader();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
        {
            AddAuthHeader();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", content);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            AddAuthHeader();
            return await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}");
        }
    
            public async Task<HttpResponseMessage> PostFileAsync(string endpoint, IFormFile file)
        {
            AddAuthHeader();
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, "file", file.FileName);
            return await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content);
        }
    }
}

