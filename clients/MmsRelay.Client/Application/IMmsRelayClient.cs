using System.Threading;
using System.Threading.Tasks;
using MmsRelay.Client.Application.Models;

namespace MmsRelay.Client.Application;

/// <summary>
/// Interface for sending MMS messages through the MmsRelay service
/// </summary>
public interface IMmsRelayClient
{
    /// <summary>
    /// Sends an MMS message through the configured MmsRelay service
    /// </summary>
    /// <param name="request">The MMS request containing recipient and content</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The result from the MmsRelay service</returns>
    Task<SendMmsResult> SendMmsAsync(SendMmsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the MmsRelay service is healthy and reachable
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if the service is healthy, false otherwise</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}