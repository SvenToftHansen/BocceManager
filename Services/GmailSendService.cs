using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;

namespace BocceManager.Services;

public record EmailAttachment(string FileName, string MimeType, byte[] Bytes);

// Sends a single email through the Gmail API. One call per recipient (not a bulk/BCC send)
// so that {FirstName}/{TeamName}-style merge fields can differ per recipient.
public static class GmailSendService
{
    private const string AppName = "BocceManager";
    private const string AltBoundary = "----BocceManagerAltBoundary";
    private const string MixBoundary = "----BocceManagerMixBoundary";

    public static async Task SendAsync(UserCredential credential, string fromEmail, string toEmail,
        string subject, string htmlBody, IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken ct = default)
    {
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });

        var message = new GmailMessage { Raw = BuildRawMessage(fromEmail, toEmail, subject, htmlBody, attachments) };

        await PollyPolicies.HttpRetry.ExecuteAsync(async token =>
        {
            await service.Users.Messages.Send(message, "me").ExecuteAsync(token);
        }, ct);
    }

    private static string BuildRawMessage(string fromEmail, string toEmail, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments)
    {
        var plainText = System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]+>", " ");

        var alt = new StringBuilder();
        alt.Append("--").Append(AltBoundary).Append("\r\n");
        alt.Append("Content-Type: text/plain; charset=\"UTF-8\"\r\n");
        alt.Append("Content-Transfer-Encoding: base64\r\n\r\n");
        alt.Append(Base64Encode(plainText)).Append("\r\n\r\n");

        alt.Append("--").Append(AltBoundary).Append("\r\n");
        alt.Append("Content-Type: text/html; charset=\"UTF-8\"\r\n");
        alt.Append("Content-Transfer-Encoding: base64\r\n\r\n");
        alt.Append(Base64Encode(htmlBody)).Append("\r\n\r\n");

        alt.Append("--").Append(AltBoundary).Append("--");

        var mime = new StringBuilder();
        mime.Append("From: ").Append(fromEmail).Append("\r\n");
        mime.Append("To: ").Append(toEmail).Append("\r\n");
        mime.Append("Subject: ").Append(EncodeHeader(subject)).Append("\r\n");
        mime.Append("MIME-Version: 1.0\r\n");

        if (attachments == null || attachments.Count == 0)
        {
            mime.Append("Content-Type: multipart/alternative; boundary=\"").Append(AltBoundary).Append("\"\r\n\r\n");
            mime.Append(alt);
        }
        else
        {
            mime.Append("Content-Type: multipart/mixed; boundary=\"").Append(MixBoundary).Append("\"\r\n\r\n");

            mime.Append("--").Append(MixBoundary).Append("\r\n");
            mime.Append("Content-Type: multipart/alternative; boundary=\"").Append(AltBoundary).Append("\"\r\n\r\n");
            mime.Append(alt).Append("\r\n\r\n");

            foreach (var attachment in attachments)
            {
                mime.Append("--").Append(MixBoundary).Append("\r\n");
                mime.Append("Content-Type: ").Append(attachment.MimeType).Append("; name=\"").Append(attachment.FileName).Append("\"\r\n");
                mime.Append("Content-Disposition: attachment; filename=\"").Append(attachment.FileName).Append("\"\r\n");
                mime.Append("Content-Transfer-Encoding: base64\r\n\r\n");
                mime.Append(Convert.ToBase64String(attachment.Bytes)).Append("\r\n\r\n");
            }

            mime.Append("--").Append(MixBoundary).Append("--");
        }

        var bytes = Encoding.UTF8.GetBytes(mime.ToString());
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Base64Encode(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static string EncodeHeader(string text) =>
        "=?UTF-8?B?" + Base64Encode(text) + "?=";
}
