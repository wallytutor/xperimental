namespace xl_webapi.Models;
using xl_webapi.Contracts;

using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

public class OllamaOptions : ILanguageClientOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 11434;
    public string Model { get; set; } = "llama3";
    public string KeepAlive { get; set; } = "5m";
    public bool Think { get; set; } = false;
    public bool Raw { get; set; } = true;
    public string BaseAddress => $"http://{Host}:{Port}";
}

public class OllamaClient : LanguageClientBase<OllamaOptions>
{
    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options)
        : base(http, options)
    {
        Http.BaseAddress = new Uri(Options.BaseAddress);
    }

    public override async Task<string> Generate(string prompt)
    {
        var payload = new {
            model      = Options.Model,
            think      = Options.Think,
            raw        = Options.Raw,
            keep_alive = Options.KeepAlive,
            stream     = false,
            prompt
        };

        var response = await Http.PostAsJsonAsync("/api/generate", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString()!;
    }

    public override async Task<string> Pull()
    {
        // XXX: do not add arguments to this method! Only the configured
        // model should be pulled, otherwise users could start pulling
        // arbitrary models which could cause issues (size/safety/etc).
        var payload = new
        {
            model = Options.Model,
            stream = false
        };

        var response = await Http.PostAsJsonAsync("/api/pull", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("status", out var status))
        {
            return status.GetString() ?? "ok";
        }

        return json.ToString();
    }
}
