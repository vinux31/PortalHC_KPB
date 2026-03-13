using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HcPortal.Hubs
{
    [Authorize]
    public class AssessmentHub : Hub
    {
        public async Task JoinBatch(string batchKey)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchKey}");
        }

        public async Task LeaveBatch(string batchKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch-{batchKey}");
        }
    }
}
