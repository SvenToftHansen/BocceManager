using Polly;
using Polly.Retry;

namespace BocceManager.Services;

/// <summary>
/// Shared Polly resilience policies for external HTTP calls (Brevo, website API).
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Retry up to 3 times with exponential back-off (2s, 4s, 8s).
    /// Covers transient HTTP errors and timeouts.
    /// </summary>
    public static readonly ResiliencePipeline HttpRetry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                AppLogger.Warn(
                    "HTTP retry {Attempt} after {Delay}s — {Outcome}",
                    args.AttemptNumber + 1,
                    args.RetryDelay.TotalSeconds,
                    args.Outcome.Exception?.Message ?? "non-success result");
                return ValueTask.CompletedTask;
            }
        })
        .Build();
}
