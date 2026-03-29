using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task<string> GenerateAsync(string prompt)
    {
        var payload = new {
            model      = _options.Model,
            think      = _options.Think,
            raw        = _options.Raw,
            keep_alive = _options.KeepAlive,
            stream     = false,
            prompt
        };
        var response = await _http.PostAsJsonAsync("/api/generate", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString()!;
    }

    public async Task<string> OllamaPull()
    {
        // XXX: do not add arguments to this method! Only the configured
        // model should be pulled, otherwise users could start pulling
        // arbitrary models which could cause issues (size/safety/etc).
        var payload = new
        {
            model = _options.Model,
            stream = false
        };

        var response = await _http.PostAsJsonAsync("/api/pull", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("status", out var status))
        {
            return status.GetString() ?? "ok";
        }

        return json.ToString();
    }
}
