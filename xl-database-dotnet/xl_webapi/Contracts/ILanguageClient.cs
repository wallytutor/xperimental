namespace xl_webapi.Contracts;
using Microsoft.Extensions.Options;

public interface ILanguageClientOptions {}

public interface ILanguageClient
{
    Task<string> Generate(string prompt);
    Task<string> Pull();
}

public abstract class LanguageClientBase<TOptions> : ILanguageClient
    where TOptions : class, ILanguageClientOptions
{
    protected LanguageClientBase(HttpClient http, IOptions<TOptions> options)
    {
        Http = http;
        Options = options.Value;
    }

    protected HttpClient Http { get; }
    protected TOptions Options { get; }

    public abstract Task<string> Generate(string prompt);
    public abstract Task<string> Pull();
}
