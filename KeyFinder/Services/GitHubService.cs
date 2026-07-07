using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeyFinder.Models;

namespace KeyFinder.Services;

public class GitHubService
{
    private readonly List<HttpClient> _clients;
    private int _currentToken;
    private readonly SemaphoreSlim _semaphore;
    private readonly int _delayMs;

    public Action<string>? OnNetworkLog { get; set; }

    public GitHubService(AppConfig config)
    {
        _semaphore = new SemaphoreSlim(config.GitHub.Concurrency);
        _delayMs = config.GitHub.DelayMs;
        _clients = new List<HttpClient>();

        foreach (var token in config.GitHub.Tokens)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.text-match+json"));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("keyfinder", "1.0"));
            client.Timeout = TimeSpan.FromSeconds(30);
            _clients.Add(client);
        }
    }

    private HttpClient GetClient()
    {
        var idx = Interlocked.Increment(ref _currentToken) % _clients.Count;
        return _clients[idx];
    }

    private string MaskToken(string token) =>
        token.Length > 8 ? token[..4] + "..." + token[^4..] : "***";

    public async Task<SearchResponse?> SearchCode(string query, int page, int perPage)
    {
        await _semaphore.WaitAsync();
        try
        {
            await Task.Delay(_delayMs);
            var client = GetClient();
            var url = $"https://api.github.com/search/code?q={Uri.EscapeDataString(query)}&page={page}&per_page={perPage}";
            var tokenPreview = MaskToken(_clients.Count > 0
                ? _clients[0].DefaultRequestHeaders.Authorization?.Parameter ?? "none"
                : "none");

            OnNetworkLog?.Invoke($"[REQ] GET {url}");
            OnNetworkLog?.Invoke($"[REQ] Token: {tokenPreview}");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
                sw.Stop();
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                OnNetworkLog?.Invoke($"[ERR] HTTP request failed after {sw.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                OnNetworkLog?.Invoke($"[ERR] Request timed out after {sw.ElapsedMilliseconds}ms");
                throw;
            }

            var statusCode = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync();
            OnNetworkLog?.Invoke($"[RES] {statusCode} in {sw.ElapsedMilliseconds}ms | Content-Length: {body.Length}");

            foreach (var header in response.Headers)
                OnNetworkLog?.Invoke($"[HDR] {header.Key}: {string.Join(", ", header.Value)}");

            var rateLimit = response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining)
                ? remaining.FirstOrDefault() : "?";
            var rateReset = response.Headers.TryGetValues("X-RateLimit-Reset", out var reset)
                ? reset.FirstOrDefault() : "?";
            OnNetworkLog?.Invoke($"[RATE] Remaining: {rateLimit}, Resets at: {rateReset}");

            if (statusCode == 403)
            {
                OnNetworkLog?.Invoke($"[RATE-LIMIT] 403 Forbidden — waiting");
                throw new RateLimitException("Rate limited by GitHub API");
            }

            if (!response.IsSuccessStatusCode)
            {
                OnNetworkLog?.Invoke($"[WARN] Non-success status {statusCode}, returning null");
                return null;
            }

            return JsonSerializer.Deserialize<SearchResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetRawContent(string htmlUrl)
    {
        await _semaphore.WaitAsync();
        try
        {
            await Task.Delay(_delayMs);
            var rawUrl = htmlUrl
                .Replace("github.com", "raw.githubusercontent.com")
                .Replace("/blob/", "/");

            var client = GetClient();
            OnNetworkLog?.Invoke($"[REQ] GET {rawUrl}");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(rawUrl);
                sw.Stop();
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                OnNetworkLog?.Invoke($"[ERR] Raw content request failed after {sw.ElapsedMilliseconds}ms: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                OnNetworkLog?.Invoke($"[ERR] Raw content request timed out after {sw.ElapsedMilliseconds}ms");
                return null;
            }

            OnNetworkLog?.Invoke($"[RES] {(int)response.StatusCode} in {sw.ElapsedMilliseconds}ms");

            if (!response.IsSuccessStatusCode)
            {
                OnNetworkLog?.Invoke($"[WARN] Raw content returned {(int)response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            OnNetworkLog?.Invoke($"[OK] Raw content fetched: {content.Length} bytes");
            return content;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public class SearchResponse
{
    [JsonPropertyName("total_count")]
    public long TotalCount { get; set; }

    [JsonPropertyName("incomplete_results")]
    public bool IncompleteResults { get; set; }

    [JsonPropertyName("items")]
    public List<SearchItem> Items { get; set; } = new();
}

public class SearchItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }

    [JsonPropertyName("repository")]
    public Repository Repository { get; set; }

    [JsonPropertyName("text_matches")]
    public List<TextMatch> TextMatches { get; set; } = new();
}

public class Repository
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }

    [JsonPropertyName("owner")]
    public Owner Owner { get; set; }
}

public class Owner
{
    [JsonPropertyName("login")]
    public string Login { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class TextMatch
{
    [JsonPropertyName("fragment")]
    public string Fragment { get; set; }
}

public class RateLimitException : Exception
{
    public RateLimitException(string msg) : base(msg) { }
}
