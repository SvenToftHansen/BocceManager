namespace BocceManager.Services;

public static class SearchQueryService
{
    private static readonly char[] Delimiters = ['|', '\\', '/', ':', ';'];

    public static List<string> SplitTerms(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return query
            .Split(Delimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public static bool MatchesAnyTerm(string source, string? query)
    {
        var terms = SplitTerms(query);
        return terms.Count == 0 || terms.Any(term => source.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
