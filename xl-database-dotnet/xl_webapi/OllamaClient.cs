using System.Net.Http.Json;
using System.Text.Json;

public class OllamaClient
{
    private readonly HttpClient _http;

    public OllamaClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("http://localhost:11434");
    }

    public async Task<string> GenerateAsync(string model, string prompt)
    {
        var payload = new
        {
            model = model,
            prompt = prompt,
            stream = false
        };

        var response = await _http.PostAsJsonAsync("/api/generate", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString()!;
    }
}
