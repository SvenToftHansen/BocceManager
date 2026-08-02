using BocceManager.Data.Entities;

namespace BocceManager.Services;

// Substitutes {FirstName}/{LastName}/{Email}/{TeamName} placeholders in a subject or body
// template, personalized per recipient before each individual Gmail send.
public static class MergeFieldService
{
    public static string Apply(string template, Player player, string? teamName) =>
        template
            .Replace("{FirstName}", player.FirstName)
            .Replace("{LastName}", player.LastName)
            .Replace("{Email}", player.Email ?? "")
            .Replace("{TeamName}", teamName ?? "");
}
