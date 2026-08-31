using BocceManager.Data.Entities;

namespace BocceManager.Services;

// Substitutes {FirstName}/{LastName}/{Email}/{TeamName}/{Division}/{Day}/{Time} placeholders in
// a subject or body template, personalized per recipient before each individual Gmail send.
public static class MergeFieldService
{
    public static string Apply(string template, Player player, string? teamName,
        string? divisionName = null, string? dayName = null, string? timeName = null) =>
        template
            .Replace("{FirstName}", player.FirstName)
            .Replace("{LastName}", player.LastName)
            .Replace("{Email}", player.Email ?? "")
            .Replace("{TeamName}", teamName ?? "")
            .Replace("{Division}", divisionName ?? "")
            .Replace("{Day}", dayName ?? "")
            .Replace("{Time}", timeName ?? "");
}
