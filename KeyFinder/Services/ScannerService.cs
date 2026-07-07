using KeyFinder.Models;
using Newtonsoft.Json;

namespace KeyFinder.Services;

public class ScannerService
{
    private readonly GitHubService _gitHub;
    private readonly AppConfig _config;
    private readonly List<KeyFinding> _findings = new();
    private readonly HashSet<string> _seenKeys = new();
    private bool _interrupted;

    public Action<string>? OnNetworkLog
    {
        get => _gitHub.OnNetworkLog;
        set => _gitHub.OnNetworkLog = value;
    }

    public ScannerService(AppConfig config)
    {
        _config = config;
        _gitHub = new GitHubService(config);
    }

    public void Stop() => _interrupted = true;

    public async Task<List<KeyFinding>> ScanAll(string providerFilter, IProgress<ScanProgress> progress)
    {
        _interrupted = false;
        _findings.Clear();
        _seenKeys.Clear();

        var providers = providerFilter == "all"
            ? PatternProvider.GetEnabledProviders(_config)
            : new() { providerFilter };

        var totalQueries = providers.Sum(p =>
            PatternProvider.GetPattern(p)?.SearchTerms.Count ?? 0);

        if (totalQueries == 0)
        {
            progress.Report(new ScanProgress { Message = "No providers enabled. Check your config.", IsComplete = true });
            return new();
        }

        progress.Report(new ScanProgress { Message = $"Scanning {providers.Count} provider(s), {totalQueries} queries" });

        var queryNum = 0;
        foreach (var provider in providers)
        {
            if (_interrupted) break;
            var pattern = PatternProvider.GetPattern(provider);
            if (pattern == null) continue;

            progress.Report(new ScanProgress { Message = $"Scanning {pattern.Name} ({pattern.Description})..." });

            foreach (var term in pattern.SearchTerms)
            {
                if (_interrupted) break;
                queryNum++;
                progress.Report(new ScanProgress { Message = $"Query {queryNum}/{totalQueries}: {term}", CurrentQuery = queryNum, TotalQueries = totalQueries });

                await SearchAndExtract(term, provider, progress);
            }

            progress.Report(new ScanProgress { Message = $"Running total: {_findings.Count} unique keys" });
        }

        progress.Report(new ScanProgress { Message = $"Found {_findings.Count} potential leaked keys", IsComplete = true });
        return _findings.ToList();
    }

    private async Task SearchAndExtract(string searchTerm, string provider, IProgress<ScanProgress> progress)
    {
        var recencyQualifier = _config.Scan.RecencyDays > 0
            ? $" pushed:>={DateTime.UtcNow.AddDays(-_config.Scan.RecencyDays):yyyy-MM-dd}"
            : "";
        var queries = new[] { $"{searchTerm}{recencyQualifier}", $"{searchTerm} filename:.env{recencyQualifier}" };
        var page = 1;
        var perPage = 30;
        var maxPages = Math.Clamp(_config.Scan.MaxResults / perPage, 1, 10);

        foreach (var query in queries)
        {
            if (_interrupted) break;
            page = 1;

            while (page <= maxPages && !_interrupted)
            {
                SearchResponse? response = null;
                try
                {
                    response = await _gitHub.SearchCode(query, page, perPage);
                }
                catch (RateLimitException)
                {
                    progress.Report(new ScanProgress { Message = "Rate limited! Waiting 60s..." });
                    await Task.Delay(60_000);
                    continue;
                }
                catch
                {
                    progress.Report(new ScanProgress { Message = $"Query failed: {query}" });
                    break;
                }

                if (response == null || response.Items.Count == 0)
                {
                    progress.Report(new ScanProgress { Message = $"No results for: {query}" });
                    break;
                }

                progress.Report(new ScanProgress { Message = $"Page {page}: {response.Items.Count} results for: {query}" });

                foreach (var item in response.Items)
                {
                    if (_interrupted) break;

                    OnNetworkLog?.Invoke($"[FILE] {item.Repository.FullName}/{item.Path}");

                    string? content = null;
                    try
                    {
                        content = await _gitHub.GetRawContent(item.HtmlUrl);
                    }
                    catch { }

                    content ??= string.Join("\n", item.TextMatches.Select(m => m.Fragment));

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        OnNetworkLog?.Invoke($"[SKIP] Empty content for {item.Repository.FullName}/{item.Path}");
                        continue;
                    }

                    var extracted = PatternProvider.ExtractKeys(content);
                    OnNetworkLog?.Invoke($"[SCAN] {item.Repository.FullName}/{item.Path}: {extracted.Count} keys found");

                    foreach (var (prov, key, masked) in extracted)
                    {
                        if (_seenKeys.Add(key))
                        {
                            _findings.Add(new KeyFinding
                            {
                                Provider = prov,
                                Key = key,
                                KeyMasked = masked,
                                FilePath = item.Path,
                                FileUrl = item.HtmlUrl,
                                RepoName = item.Repository.FullName,
                                RepoUrl = item.Repository.HtmlUrl,
                                Owner = item.Repository.Owner.Login,
                                OwnerUrl = item.Repository.Owner.HtmlUrl,
                                OwnerType = item.Repository.Owner.Type
                            });
                        }
                    }
                }
                page++;
            }
        }
    }

    public void SaveResults(List<KeyFinding> findings, string suffix = "")
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var dir = _config.Output.OutputPath;
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"findings_{timestamp}{(suffix != "" ? "_" + suffix : "")}.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(findings, Formatting.Indented));
    }
}

public class ScanProgress
{
    public string Message { get; set; }
    public int CurrentQuery { get; set; }
    public int TotalQueries { get; set; }
    public bool IsComplete { get; set; }
}
