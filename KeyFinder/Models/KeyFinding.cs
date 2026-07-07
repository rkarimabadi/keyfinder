using Newtonsoft.Json;

namespace KeyFinder.Models;

public class KeyFinding
{
    [JsonProperty("provider")]
    public string Provider { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("key_masked")]
    public string KeyMasked { get; set; }

    [JsonProperty("file_path")]
    public string FilePath { get; set; }

    [JsonProperty("file_url")]
    public string FileUrl { get; set; }

    [JsonProperty("repo_name")]
    public string RepoName { get; set; }

    [JsonProperty("repo_url")]
    public string RepoUrl { get; set; }

    [JsonProperty("owner")]
    public string Owner { get; set; }

    [JsonProperty("owner_url")]
    public string OwnerUrl { get; set; }

    [JsonProperty("owner_type")]
    public string OwnerType { get; set; }

    [JsonProperty("found_at")]
    public string FoundAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonProperty("verified")]
    public bool? Verified { get; set; }
}

public class VerifiedKey
{
    [JsonProperty("finding")]
    public KeyFinding Finding { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("verified_at")]
    public string VerifiedAt { get; set; }

    [JsonProperty("verification_method")]
    public string VerificationMethod { get; set; }

    [JsonProperty("error_message")]
    public string? ErrorMessage { get; set; }
}
