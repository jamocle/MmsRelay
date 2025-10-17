using System.Threading;
using System.Threading.Tasks;
using MmsRelay.Application.Models;

namespace MmsRelay.Application;

public interface IMmsSender
{
    Task<SendMmsResult> SendAsync(SendMmsRequest request, CancellationToken ct);
}
