using System.Net.Mail;
using BocceManager.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public record InboxMessage(
    string Id, string ThreadId, string FromDisplay, string FromEmail,
    string Subject, DateTime Date, string Snippet, bool IsUnread,
    int? PlayerId, string? PlayerName);

// Read-side counterpart to GmailSendService — lists/reads/labels messages in the connected
// account's inbox (for the Compose Email panel's "Replies" tab). Uses the same GmailModify
// scope GmailAuthService requests, so no separate connect flow is needed.
public static class GmailReadService
{
    private const string AppName = "BocceManager";

    public static async Task<List<InboxMessage>> ListInboxAsync(UserCredential credential, int maxResults = 50, CancellationToken ct = default)
    {
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });

        var listReq = service.Users.Messages.List("me");
        listReq.LabelIds = new Google.Apis.Util.Repeatable<string>(["INBOX"]);
        listReq.MaxResults = maxResults;
        var listResp = await PollyPolicies.HttpRetry.ExecuteAsync(async (CancellationToken t) => await listReq.ExecuteAsync(t), ct);

        var summaries = listResp.Messages;
        if (summaries == null || summaries.Count == 0) return [];

        Dictionary<string, (int Id, string Name)> playerByEmail;
        using (var db = new BocceDbContext())
        {
            var players = await db.Players
                .Where(p => p.Email != null && p.Email != "")
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.Email })
                .ToListAsync(ct);
            playerByEmail = players
                .GroupBy(p => p.Email!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => (g.First().Id, $"{g.First().FirstName} {g.First().LastName}".Trim()));
        }

        var results = new List<InboxMessage>();
        foreach (var summary in summaries)
        {
            var getReq = service.Users.Messages.Get("me", summary.Id);
            getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
            getReq.MetadataHeaders = new Google.Apis.Util.Repeatable<string>(["From", "Subject"]);
            var msg = await PollyPolicies.HttpRetry.ExecuteAsync(async (CancellationToken t) => await getReq.ExecuteAsync(t), ct);

            var headers = msg.Payload?.Headers ?? [];
            var fromRaw = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "";
            var subject = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(no subject)";
            var (fromDisplay, fromEmail) = ParseFrom(fromRaw);
            var date = msg.InternalDate.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(msg.InternalDate.Value).LocalDateTime
                : DateTime.MinValue;
            bool unread = msg.LabelIds?.Contains("UNREAD") ?? false;

            int? playerId = null;
            string? playerName = null;
            if (playerByEmail.TryGetValue(fromEmail.Trim().ToLowerInvariant(), out var match))
            {
                playerId = match.Id;
                playerName = match.Name;
            }

            results.Add(new InboxMessage(
                msg.Id, msg.ThreadId, fromDisplay, fromEmail, subject, date,
                msg.Snippet ?? "", unread, playerId, playerName));
        }

        return results.OrderByDescending(r => r.Date).ToList();
    }

    public static async Task<string> GetMessageBodyHtmlAsync(UserCredential credential, string messageId, CancellationToken ct = default)
    {
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });

        var getReq = service.Users.Messages.Get("me", messageId);
        getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        var msg = await PollyPolicies.HttpRetry.ExecuteAsync(async (CancellationToken t) => await getReq.ExecuteAsync(t), ct);

        if (msg.Payload == null) return "<p><em>(empty message)</em></p>";

        var html = FindPart(msg.Payload, "text/html");
        if (html != null) return DecodeBase64Url(html);

        var plain = FindPart(msg.Payload, "text/plain");
        if (plain != null)
        {
            var text = DecodeBase64Url(plain);
            return $"<pre style=\"white-space:pre-wrap;font-family:Segoe UI,Arial,sans-serif;\">{System.Net.WebUtility.HtmlEncode(text)}</pre>";
        }

        return "<p><em>(no readable body)</em></p>";
    }

    public static async Task MarkAsReadAsync(UserCredential credential, string messageId, CancellationToken ct = default)
    {
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });
        var body = new ModifyMessageRequest { RemoveLabelIds = ["UNREAD"] };
        await PollyPolicies.HttpRetry.ExecuteAsync(async (CancellationToken t) => await service.Users.Messages.Modify(body, "me", messageId).ExecuteAsync(t), ct);
    }

    public static async Task ArchiveAsync(UserCredential credential, string messageId, CancellationToken ct = default)
    {
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });
        var body = new ModifyMessageRequest { RemoveLabelIds = ["INBOX"] };
        await PollyPolicies.HttpRetry.ExecuteAsync(async (CancellationToken t) => await service.Users.Messages.Modify(body, "me", messageId).ExecuteAsync(t), ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (string Display, string Email) ParseFrom(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return ("(unknown)", "");
        try
        {
            var addr = new MailAddress(raw);
            return (string.IsNullOrWhiteSpace(addr.DisplayName) ? addr.Address : addr.DisplayName, addr.Address);
        }
        catch
        {
            return (raw, raw);
        }
    }

    // Depth-first search for the first part matching mimeType — prefers a direct match at this
    // level before descending, so multipart/alternative's preferred (usually last/richest) part
    // isn't skipped over by an earlier nested attachment sharing the same mime type.
    private static string? FindPart(MessagePart part, string mimeType)
    {
        if (string.Equals(part.MimeType, mimeType, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(part.Body?.Data))
            return part.Body.Data;

        if (part.Parts == null) return null;
        foreach (var child in part.Parts)
        {
            var found = FindPart(child, mimeType);
            if (found != null) return found;
        }
        return null;
    }

    private static string DecodeBase64Url(string data)
    {
        var s = data.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
