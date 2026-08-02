using BocceManager.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace BocceManager.Services;

// Handles the Gmail OAuth2 desktop (loopback) flow. Client ID/Secret and the resulting
// refresh token all live in AppParameters (DPAPI-protected via SecretProtector) instead of
// Google's default on-disk FileDataStore, so they travel with the rest of the app's config/backups.
public static class GmailAuthService
{
    private const string AppName = "BocceManager";
    private const string ClientIdKey = "Gmail.ClientId";
    private const string ClientSecretKey = "Gmail.ClientSecretProtected";
    private const string TokenStoreKey = "Gmail.TokenStore";
    private static readonly string[] Scopes = [GmailService.Scope.GmailSend];

    public static bool IsConnected()
    {
        using var db = new BocceDbContext();
        return !string.IsNullOrEmpty(AppParameterService.GetAppParameter(db, TokenStoreKey));
    }

    public static void Disconnect()
    {
        using var db = new BocceDbContext();
        AppParameterService.SetAppParameter(db, TokenStoreKey, "");
        AppLogger.Info("Gmail disconnected");
    }

    // Runs (or silently refreshes) the OAuth flow. Only pops the system browser for consent
    // the first time, or after Disconnect() — GoogleWebAuthorizationBroker reuses a stored,
    // still-valid refresh token otherwise.
    public static async Task<UserCredential> GetCredentialAsync(CancellationToken ct = default)
    {
        using var db = new BocceDbContext();
        var clientId = AppParameterService.GetAppParameter(db, ClientIdKey);
        var protectedSecret = AppParameterService.GetAppParameter(db, ClientSecretKey);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(protectedSecret))
            throw new InvalidOperationException("Enter a Gmail Client ID and Client Secret in Parameters before connecting.");

        var clientSecret = SecretProtector.Unprotect(protectedSecret);

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes,
            "user",
            ct,
            new AppParameterDataStore());

        return credential;
    }

    public static async Task<string> ConnectAsync(CancellationToken ct = default)
    {
        var credential = await GetCredentialAsync(ct);
        var email = await GetConnectedAccountAsync(credential, ct);
        AppLogger.Info("Gmail connected as {Email}", email);
        return email;
    }

    public static async Task<string> GetConnectedAccountAsync(UserCredential credential, CancellationToken ct = default)
    {
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });
        var profile = await service.Users.GetProfile("me").ExecuteAsync(ct);
        return profile.EmailAddress;
    }

    // Stores Google's token JSON as a single DPAPI-protected AppParameter row instead of
    // per-user files on disk. The app only ever authorizes one Windows/Gmail account, so a
    // single well-known AppParameter key is used regardless of the "key"/"user" Google passes in.
    private sealed class AppParameterDataStore : IDataStore
    {
        public Task StoreAsync<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            using var db = new BocceDbContext();
            AppParameterService.SetAppParameter(db, TokenStoreKey, SecretProtector.Protect(json));
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            using var db = new BocceDbContext();
            AppParameterService.SetAppParameter(db, TokenStoreKey, "");
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string key)
        {
            using var db = new BocceDbContext();
            var stored = AppParameterService.GetAppParameter(db, TokenStoreKey);
            if (string.IsNullOrEmpty(stored))
                return Task.FromResult(default(T)!);

            var json = SecretProtector.Unprotect(stored);
            return Task.FromResult(JsonConvert.DeserializeObject<T>(json)!);
        }

        public Task ClearAsync()
        {
            using var db = new BocceDbContext();
            AppParameterService.SetAppParameter(db, TokenStoreKey, "");
            return Task.CompletedTask;
        }
    }
}
