using System.Text.RegularExpressions;

namespace KeyFinder.Models;

public class KeyPattern
{
    public string Name { get; set; }
    public string ProviderId { get; set; }
    public string Description { get; set; }
    public Regex Regex { get; set; }
    public List<string> SearchTerms { get; set; }
    public string Confidence { get; set; }

    public KeyPattern(string name, string providerId, string description, string pattern,
        List<string> searchTerms, string confidence)
    {
        Name = name;
        ProviderId = providerId;
        Description = description;
        Regex = new Regex(pattern, RegexOptions.Compiled);
        SearchTerms = searchTerms;
        Confidence = confidence;
    }
}
