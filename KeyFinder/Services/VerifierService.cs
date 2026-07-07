using System.Net.Http.Headers;
using System.Text;
using KeyFinder.Models;

namespace KeyFinder.Services;

public class VerifierService
{
    private readonly HttpClient _client;
    private readonly int _concurrent;

    public VerifierService(int concurrent = 5)
    {
        _concurrent = concurrent;
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<List<VerifiedKey>> Verify(List<KeyFinding> findings, int? limit, IProgress<string>? progress = null)
    {
        if (limit.HasValue)
            findings = findings.Take(limit.Value).ToList();

        var results = new List<VerifiedKey>();
        var semaphore = new SemaphoreSlim(_concurrent);
        var tasks = findings.Select(async finding =>
        {
            await semaphore.WaitAsync();
            try
            {
                var result = await VerifySingle(finding);
                lock (results) results.Add(result);
                progress?.Report($"{(result.IsActive ? "✓ ACTIVE" : "✗ INVALID")} {finding.KeyMasked} ({finding.Provider})");
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    private async Task<VerifiedKey> VerifySingle(KeyFinding finding)
    {
        var provider = finding.Provider.ToLowerInvariant();
        var key = finding.Key;

        var (isActive, method, error) = provider switch
        {
            "openai" => await VerifyOpenai(key),
            "anthropic" => await VerifyAnthropic(key),
            "google" => await VerifyGoogle(key),
            "groq" => await VerifyGroq(key),
            "huggingface" => await VerifyHuggingface(key),
            "github" => await VerifyGithub(key),
            "gitlab" => await VerifyGitlab(key),
            "stripe live" or "stripe restricted" => await VerifyStripe(key),
            "sendgrid" => await VerifySendgrid(key),
            "slack bot" or "slack user" => await VerifySlack(key),
            "discord" => await VerifyDiscord(key),
            "telegram" => await VerifyTelegram(key),
            "new relic" => await VerifyNewRelic(key),
            "mapbox" => await VerifyMapbox(key),
            "datadog" => await VerifyDatadog(key),
            _ => (false, "unsupported", $"No verification for {provider}")
        };

        finding.Verified = isActive;
        return new VerifiedKey
        {
            Finding = finding,
            IsActive = isActive,
            VerifiedAt = DateTime.UtcNow.ToString("o"),
            VerificationMethod = method,
            ErrorMessage = error
        };
    }

    private async Task<(bool, string, string?)> VerifyOpenai(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => (true, "GET /v1/models", null),
                System.Net.HttpStatusCode.Unauthorized => (false, "GET /v1/models", null),
                _ => (false, "GET /v1/models", $"status {(int)resp.StatusCode}")
            };
        }
        catch (Exception e) { return (false, "GET /v1/models", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyAnthropic(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", key);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(
                "{\"model\":\"claude-3-haiku-20240307\",\"max_tokens\":1,\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}",
                Encoding.UTF8, "application/json");
            var resp = await _client.SendAsync(req);
            return resp.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => (true, "POST /v1/messages", null),
                System.Net.HttpStatusCode.Unauthorized => (false, "POST /v1/messages", null),
                _ when (int)resp.StatusCode == 400 => (true, "POST /v1/messages", null),
                _ => (false, "POST /v1/messages", $"status {(int)resp.StatusCode}")
            };
        }
        catch (Exception e) { return (false, "POST /v1/messages", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyGoogle(string key)
    {
        try
        {
            var resp = await _client.GetAsync($"https://generativelanguage.googleapis.com/v1/models?key={key}");
            return resp.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => (true, "GET /v1/models", null),
                System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => (false, "GET /v1/models", null),
                _ => (false, "GET /v1/models", $"status {(int)resp.StatusCode}")
            };
        }
        catch (Exception e) { return (false, "GET /v1/models", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyGroq(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /v1/models", null)
                : (false, "GET /v1/models", $"status {(int)resp.StatusCode}");
        }
        catch (Exception e) { return (false, "GET /v1/models", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyHuggingface(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://huggingface.co/api/whoami-v2");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /api/whoami-v2", null)
                : (false, "GET /api/whoami-v2", null);
        }
        catch (Exception e) { return (false, "GET /api/whoami-v2", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyGithub(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("KeyFinder", "1.0"));
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /user", null)
                : (false, "GET /user", null);
        }
        catch (Exception e) { return (false, "GET /user", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyGitlab(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://gitlab.com/api/v4/user");
            req.Headers.Add("PRIVATE-TOKEN", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /api/v4/user", null)
                : (false, "GET /api/v4/user", null);
        }
        catch (Exception e) { return (false, "GET /api/v4/user", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyStripe(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.stripe.com/v1/balance");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /v1/balance", null)
                : (false, "GET /v1/balance", null);
        }
        catch (Exception e) { return (false, "GET /v1/balance", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifySendgrid(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.sendgrid.com/v3/scopes");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /v3/scopes", null)
                : (false, "GET /v3/scopes", null);
        }
        catch (Exception e) { return (false, "GET /v3/scopes", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifySlack(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://slack.com/api/auth.test");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var resp = await _client.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return (false, "GET /api/auth.test", null);
            var body = await resp.Content.ReadAsStringAsync();
            return (body.Contains("\"ok\":true"), "GET /api/auth.test", null);
        }
        catch (Exception e) { return (false, "GET /api/auth.test", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyDiscord(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bot", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /users/@me", null)
                : (false, "GET /users/@me", null);
        }
        catch (Exception e) { return (false, "GET /users/@me", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyTelegram(string key)
    {
        try
        {
            var resp = await _client.GetAsync($"https://api.telegram.org/bot{key}/getMe");
            if (!resp.IsSuccessStatusCode) return (false, "GET /getMe", null);
            var body = await resp.Content.ReadAsStringAsync();
            return (body.Contains("\"ok\":true"), "GET /getMe", null);
        }
        catch (Exception e) { return (false, "GET /getMe", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyNewRelic(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.newrelic.com/v2/users.json");
            req.Headers.Add("X-Api-Key", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /v2/users.json", null)
                : (false, "GET /v2/users.json", null);
        }
        catch (Exception e) { return (false, "GET /v2/users.json", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyMapbox(string key)
    {
        try
        {
            var resp = await _client.GetAsync($"https://api.mapbox.com/tokens/v2?access_token={key}");
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /tokens/v2", null)
                : (false, "GET /tokens/v2", null);
        }
        catch (Exception e) { return (false, "GET /tokens/v2", e.Message); }
    }

    private async Task<(bool, string, string?)> VerifyDatadog(string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.datadoghq.com/api/v1/validate");
            req.Headers.Add("DD-API-KEY", key);
            var resp = await _client.SendAsync(req);
            return resp.StatusCode == System.Net.HttpStatusCode.OK
                ? (true, "GET /api/v1/validate", null)
                : (false, "GET /api/v1/validate", null);
        }
        catch (Exception e) { return (false, "GET /api/v1/validate", e.Message); }
    }
}
