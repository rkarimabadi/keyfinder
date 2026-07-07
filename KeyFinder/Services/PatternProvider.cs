using System.Text.RegularExpressions;
using KeyFinder.Models;

namespace KeyFinder.Services;

public static class PatternProvider
{
    private static readonly Dictionary<string, KeyPattern> _patterns = new();

    static PatternProvider()
    {
        _patterns["anthropic"] = new("Anthropic", "anthropic", "Anthropic Claude API Key",
            @"sk-ant-api03-[A-Za-z0-9_-]{93}",
            new() { "sk-ant-api03-", "ANTHROPIC_API_KEY", "CLAUDE_API_KEY" }, "high");

        _patterns["openai"] = new("OpenAI", "openai", "OpenAI API Key",
            @"sk-[A-Za-z0-9]{20}T3BlbkFJ[A-Za-z0-9]{20}|sk-proj-[A-Za-z0-9_-]{48,180}|sk-[a-zA-Z0-9]{48}",
            new() { "T3BlbkFJ", "sk-proj-", "OPENAI_API_KEY", "OPENAI_KEY" }, "high");

        _patterns["google"] = new("Google AI", "google", "Google AI / Gemini API Key",
            @"AIza[0-9A-Za-z_-]{35}",
            new() { "AIzaSy", "GOOGLE_API_KEY", "GEMINI_API_KEY" }, "high");

        _patterns["grok"] = new("xAI Grok", "grok", "xAI Grok API Key",
            @"xai-[A-Za-z0-9]{48,}",
            new() { "xai-", "XAI_API_KEY", "GROK_API_KEY" }, "high");

        _patterns["deepseek"] = new("DeepSeek", "deepseek", "DeepSeek API Key",
            @"sk-[a-f0-9]{32}",
            new() { "DEEPSEEK_API_KEY", "DEEPSEEK_KEY" }, "medium");

        _patterns["huggingface"] = new("HuggingFace", "huggingface", "Hugging Face Token",
            @"hf_[A-Za-z0-9]{34,}",
            new() { "hf_", "HUGGINGFACE_TOKEN", "HUGGINGFACE_API_KEY", "HF_TOKEN" }, "high");

        _patterns["replicate"] = new("Replicate", "replicate", "Replicate API Token",
            @"r8_[A-Za-z0-9]{38,}",
            new() { "r8_", "REPLICATE_API_TOKEN" }, "high");

        _patterns["perplexity"] = new("Perplexity", "perplexity", "Perplexity API Key",
            @"pplx-[A-Za-z0-9]{48,}",
            new() { "pplx-", "PERPLEXITY_API_KEY" }, "high");

        _patterns["groq"] = new("Groq", "groq", "Groq API Key",
            @"gsk_[A-Za-z0-9]{52,}",
            new() { "gsk_", "GROQ_API_KEY" }, "high");

        _patterns["fireworks"] = new("Fireworks AI", "fireworks", "Fireworks AI API Key",
            @"fw_[A-Za-z0-9]{32,}",
            new() { "fw_", "FIREWORKS_API_KEY" }, "high");

        _patterns["aws"] = new("AWS Access Key", "aws", "AWS Access Key ID",
            @"AKIA[0-9A-Z]{16}",
            new() { "AKIA", "AWS_ACCESS_KEY", "AWS_ACCESS_KEY_ID" }, "high");

        _patterns["stripe_live"] = new("Stripe Live", "stripe_live", "Stripe Live Secret Key",
            @"sk_live_[0-9a-zA-Z]{24,99}",
            new() { "sk_live_", "STRIPE_SECRET_KEY" }, "high");

        _patterns["github_token"] = new("GitHub", "github_token", "GitHub Personal Access Token",
            @"ghp_[A-Za-z0-9]{36}|gho_[A-Za-z0-9]{36}|ghu_[A-Za-z0-9]{36}|ghs_[A-Za-z0-9]{36}|ghr_[A-Za-z0-9]{36}|github_pat_[A-Za-z0-9_]{22,}",
            new() { "ghp_", "GITHUB_TOKEN", "GH_TOKEN" }, "high");

        _patterns["gitlab"] = new("GitLab", "gitlab", "GitLab Personal Access Token",
            @"glpat-[A-Za-z0-9_-]{20,}",
            new() { "glpat-", "GITLAB_TOKEN" }, "high");

        _patterns["slack_bot"] = new("Slack Bot", "slack_bot", "Slack Bot Token",
            @"xoxb-[0-9]{10,13}-[0-9]{10,13}-[a-zA-Z0-9]{24}",
            new() { "xoxb-", "SLACK_BOT_TOKEN" }, "high");

        _patterns["discord"] = new("Discord", "discord", "Discord Bot Token",
            @"[MN][A-Za-z\d]{23,}\.[\w-]{6}\.[\w-]{27,}",
            new() { "DISCORD_TOKEN", "DISCORD_BOT_TOKEN" }, "high");

        _patterns["telegram"] = new("Telegram", "telegram", "Telegram Bot Token",
            @"[0-9]{8,10}:[A-Za-z0-9_-]{35}",
            new() { "TELEGRAM_BOT_TOKEN", "TELEGRAM_TOKEN" }, "high");

        _patterns["mongodb"] = new("MongoDB", "mongodb", "MongoDB Connection String",
            @"mongodb\+srv://[^:]+:[^@]+@[^\s]+",
            new() { "mongodb+srv://", "MONGODB_URI", "MONGO_URL" }, "high");

        _patterns["postgres"] = new("PostgreSQL", "postgres", "PostgreSQL Connection String",
            @"postgres://[^:]+:[^@]+@[^\s]+",
            new() { "postgres://", "POSTGRES_URL" }, "high");

        _patterns["sendgrid"] = new("SendGrid", "sendgrid", "SendGrid API Key",
            @"SG\.[A-Za-z0-9_-]{22}\.[A-Za-z0-9_-]{43}",
            new() { "SG.", "SENDGRID_API_KEY" }, "high");

        _patterns["stripe_restricted"] = new("Stripe Restricted", "stripe_restricted", "Stripe Restricted Key",
            @"rk_live_[0-9a-zA-Z]{24,99}",
            new() { "rk_live_" }, "high");

        _patterns["twilio"] = new("Twilio", "twilio", "Twilio API Key",
            @"SK[a-f0-9]{32}",
            new() { "TWILIO_API_KEY", "TWILIO_AUTH_TOKEN" }, "high");

        _patterns["mailgun"] = new("Mailgun", "mailgun", "Mailgun API Key",
            @"key-[a-f0-9]{32}",
            new() { "key-", "MAILGUN_API_KEY" }, "high");

        _patterns["newrelic"] = new("New Relic", "newrelic", "New Relic API Key",
            @"NRAK-[A-Z0-9]{27}",
            new() { "NRAK-", "NEW_RELIC_LICENSE_KEY" }, "high");

        _patterns["mapbox"] = new("Mapbox", "mapbox", "Mapbox Access Token",
            @"pk\.[a-zA-Z0-9]{60,}|sk\.[a-zA-Z0-9]{60,}",
            new() { "pk.eyJ", "sk.eyJ", "MAPBOX_TOKEN" }, "high");

        _patterns["sentry"] = new("Sentry", "sentry", "Sentry DSN",
            @"https://[a-f0-9]{32}@[a-z0-9]+\.ingest\.sentry\.io/[0-9]+",
            new() { "ingest.sentry.io", "SENTRY_DSN" }, "high");

        _patterns["private_key"] = new("Private Key", "private_key", "Private Key File",
            @"-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----",
            new() { "BEGIN RSA PRIVATE KEY", "BEGIN PRIVATE KEY" }, "high");

        _patterns["npm"] = new("NPM", "npm", "NPM Access Token",
            @"npm_[A-Za-z0-9]{36}",
            new() { "npm_", "NPM_TOKEN" }, "high");

        _patterns["pypi"] = new("PyPI", "pypi", "PyPI API Token",
            @"pypi-[A-Za-z0-9_-]{50,}",
            new() { "pypi-", "PYPI_TOKEN" }, "high");

        _patterns["doppler"] = new("Doppler", "doppler", "Doppler Token",
            @"dp\.pt\.[A-Za-z0-9]{40,}",
            new() { "dp.pt.", "DOPPLER_TOKEN" }, "high");

        _patterns["planetscale"] = new("PlanetScale", "planetscale", "PlanetScale Token",
            @"pscale_tkn_[A-Za-z0-9_]{32,}",
            new() { "pscale_tkn_", "PLANETSCALE_TOKEN" }, "high");
    }

    public static KeyPattern? GetPattern(string providerId) =>
        _patterns.GetValueOrDefault(providerId);

    public static List<(string Name, string ProviderId, KeyPattern Pattern)> GetAll() =>
        _patterns.Select(kv => (kv.Value.Name, kv.Key, kv.Value)).ToList();

    public static List<KeyPattern> GetByProviderIds(List<string> ids) =>
        ids.Select(id => _patterns.GetValueOrDefault(id)).Where(p => p != null).ToList()!;

    public static bool IsPlaceholder(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("xxxx") || lower.Contains("yyyy") || lower.Contains("zzzz")
            || lower.Contains("your_") || lower.Contains("your-") || lower.Contains("example")
            || lower.Contains("placeholder") || lower.Contains("insert") || lower.Contains("_here")
            || lower.Contains("<your") || lower.Contains("test_") || lower.Contains("dummy")
            || lower.Contains("sample") || lower.Contains("fake")
            || key == "sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
            || key.All(c => c == 'x' || c == 'X' || c == '-' || c == '_');
    }

    public static string MaskKey(string key)
    {
        var len = key.Length;
        if (len <= 16) return $"{key[..Math.Min(len, 8)]}...";
        return $"{key[..10]}...{key[^4..]}";
    }

    public static List<(string ProviderName, string FullKey, string Masked)> ExtractKeys(
        string content, string? providerId = null)
    {
        var results = new List<(string, string, string)>();
        var patterns = providerId != null
            ? new[] { (_patterns.GetValueOrDefault(providerId), providerId) }
                .Where(x => x.Item1 != null).Select(x => (x.Item1!, x.Item2!)).ToList()
            : _patterns.Select(kv => (kv.Value, kv.Key)).ToList();

        foreach (var (pattern, _) in patterns)
        {
            foreach (Match match in pattern.Regex.Matches(content))
            {
                var key = match.Value;
                if (IsPlaceholder(key)) continue;
                if (key.Length < 16 && pattern.Confidence == "low") continue;
                results.Add((pattern.Name, key, MaskKey(key)));
            }
        }
        return results.DistinctBy(r => r.Item2).ToList();
    }

    public static List<string> GetEnabledProviders(AppConfig config)
    {
        var enabled = new List<string>();
        var p = config.Providers;

        if (p.Anthropic) enabled.Add("anthropic");
        if (p.Openai) enabled.Add("openai");
        if (p.Google) enabled.Add("google");
        if (p.Grok) enabled.Add("grok");
        if (p.Deepseek) enabled.Add("deepseek");
        if (p.Huggingface) enabled.Add("huggingface");
        if (p.Replicate) enabled.Add("replicate");
        if (p.Cohere) enabled.Add("cohere");
        if (p.Mistral) enabled.Add("mistral");
        if (p.Together) enabled.Add("together");
        if (p.Perplexity) enabled.Add("perplexity");
        if (p.Groq) enabled.Add("groq");
        if (p.Fireworks) enabled.Add("fireworks");
        if (p.Aws) enabled.Add("aws");
        if (p.AwsSecret) enabled.Add("aws_secret");
        if (p.Azure) enabled.Add("azure");
        if (p.StripeLive) enabled.Add("stripe_live");
        if (p.StripeRestricted) enabled.Add("stripe_restricted");
        if (p.Paypal) enabled.Add("paypal");
        if (p.Square) enabled.Add("square");
        if (p.Twilio) enabled.Add("twilio");
        if (p.Sendgrid) enabled.Add("sendgrid");
        if (p.Mailgun) enabled.Add("mailgun");
        if (p.Mailchimp) enabled.Add("mailchimp");
        if (p.GithubToken) enabled.Add("github_token");
        if (p.Gitlab) enabled.Add("gitlab");
        if (p.Npm) enabled.Add("npm");
        if (p.Pypi) enabled.Add("pypi");
        if (p.SlackBot) enabled.Add("slack_bot");
        if (p.SlackUser) enabled.Add("slack_user");
        if (p.SlackWebhook) enabled.Add("slack_webhook");
        if (p.Discord) enabled.Add("discord");
        if (p.DiscordWebhook) enabled.Add("discord_webhook");
        if (p.Telegram) enabled.Add("telegram");
        if (p.Mongodb) enabled.Add("mongodb");
        if (p.Postgres) enabled.Add("postgres");
        if (p.Mysql) enabled.Add("mysql");
        if (p.Redis) enabled.Add("redis");
        if (p.Firebase) enabled.Add("firebase");
        if (p.Supabase) enabled.Add("supabase");
        if (p.Vercel) enabled.Add("vercel");
        if (p.Netlify) enabled.Add("netlify");
        if (p.Heroku) enabled.Add("heroku");
        if (p.Algolia) enabled.Add("algolia");
        if (p.Mapbox) enabled.Add("mapbox");
        if (p.Sentry) enabled.Add("sentry");
        if (p.Datadog) enabled.Add("datadog");
        if (p.Newrelic) enabled.Add("newrelic");
        if (p.Planetscale) enabled.Add("planetscale");
        if (p.Doppler) enabled.Add("doppler");
        if (p.PrivateKey) enabled.Add("private_key");

        return enabled;
    }
}
