using Newtonsoft.Json;

namespace KeyFinder.Models;

public class AppConfig
{
    [JsonProperty("github")]
    public GitHubConfig GitHub { get; set; } = new();

    [JsonProperty("scan")]
    public ScanConfig Scan { get; set; } = new();

    [JsonProperty("output")]
    public OutputConfig Output { get; set; } = new();

    [JsonProperty("providers")]
    public ProvidersConfig Providers { get; set; } = new();
}

public class GitHubConfig
{
    [JsonProperty("tokens")]
    public List<string> Tokens { get; set; } = new();

    [JsonProperty("concurrency")]
    public int Concurrency { get; set; } = 5;

    [JsonProperty("delay_ms")]
    public int DelayMs { get; set; } = 500;
}

public class ScanConfig
{
    [JsonProperty("max_results")]
    public int MaxResults { get; set; } = 100;

    [JsonProperty("recency_days")]
    public int RecencyDays { get; set; } = 0;
}

public class OutputConfig
{
    [JsonProperty("format")]
    public string Format { get; set; } = "table";

    [JsonProperty("save_to_file")]
    public bool SaveToFile { get; set; } = true;

    [JsonProperty("output_path")]
    public string OutputPath { get; set; } = "results";
}

public class ProvidersConfig
{
    public bool Anthropic { get; set; } = true;
    public bool Openai { get; set; } = true;
    public bool Google { get; set; } = true;
    public bool Grok { get; set; } = true;
    public bool Deepseek { get; set; } = true;
    public bool Huggingface { get; set; } = true;
    public bool Replicate { get; set; } = true;
    public bool Cohere { get; set; } = true;
    public bool Mistral { get; set; } = true;
    public bool Together { get; set; } = true;
    public bool Perplexity { get; set; } = true;
    public bool Groq { get; set; } = true;
    public bool Fireworks { get; set; } = true;
    public bool Aws { get; set; } = true;
    public bool AwsSecret { get; set; } = true;
    public bool Azure { get; set; } = true;
    public bool StripeLive { get; set; } = true;
    public bool StripeRestricted { get; set; } = true;
    public bool Paypal { get; set; } = true;
    public bool Square { get; set; } = true;
    public bool Twilio { get; set; } = true;
    public bool Sendgrid { get; set; } = true;
    public bool Mailgun { get; set; } = true;
    public bool Mailchimp { get; set; } = true;
    public bool GithubToken { get; set; } = true;
    public bool Gitlab { get; set; } = true;
    public bool Npm { get; set; } = true;
    public bool Pypi { get; set; } = true;
    public bool SlackBot { get; set; } = true;
    public bool SlackUser { get; set; } = true;
    public bool SlackWebhook { get; set; } = true;
    public bool Discord { get; set; } = true;
    public bool DiscordWebhook { get; set; } = true;
    public bool Telegram { get; set; } = true;
    public bool Mongodb { get; set; } = true;
    public bool Postgres { get; set; } = true;
    public bool Mysql { get; set; } = true;
    public bool Redis { get; set; } = true;
    public bool Firebase { get; set; } = true;
    public bool Supabase { get; set; } = true;
    public bool Vercel { get; set; } = true;
    public bool Netlify { get; set; } = true;
    public bool Heroku { get; set; } = true;
    public bool Algolia { get; set; } = true;
    public bool Mapbox { get; set; } = true;
    public bool Sentry { get; set; } = true;
    public bool Datadog { get; set; } = true;
    public bool Newrelic { get; set; } = true;
    public bool Planetscale { get; set; } = true;
    public bool Doppler { get; set; } = true;
    public bool PrivateKey { get; set; } = true;
}
