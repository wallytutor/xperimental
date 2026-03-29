using Microsoft.Extensions.Options;
using System.Text.Json;
// using System.Net.Http.Json;

public class OllamaOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 11434;
    public string Model { get; set; } = "llama3";
    public string KeepAlive { get; set; } = "5m";
    public bool Think { get; set; } = false;
    public bool Raw { get; set; } = true;
    public string BaseAddress => $"http://{Host}:{Port}";
}

public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;

        _http.BaseAddress = new Uri(_options.BaseAddress);
    }

    private object GenerateRequest(string prompt)
    {
        return new {
            model      = _options.Model,
            think      = _options.Think,
            raw        = _options.Raw,
            keep_alive = _options.KeepAlive,
            stream     = false,
            prompt
        };
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        var payload = GenerateRequest(prompt);
        var response = await _http.PostAsJsonAsync("/api/generate", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString()!;
    }
}
