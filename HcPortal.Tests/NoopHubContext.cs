// Phase 412 — Stub IHubContext<AssessmentHub> no-op untuk write-path tests yang men-drive endpoint
// add/remove/restore ASLI. Endpoint 410/411 kini broadcast SignalR POST-commit (participantAdded/
// participantRemoved/examRemoved). Harness lama passing `hubContext: null!` → NRE saat broadcast.
// Stub ini menelan semua SendAsync (Clients.Group/.User/.All) tanpa efek samping — kontrak JSON
// endpoint TIDAK berubah, test hanya bisa menembus jalur broadcast. NO replica logika produksi.
using System.Threading;
using System.Threading.Tasks;
using HcPortal.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HcPortal.Tests;

/// <summary>No-op IHubContext untuk test (broadcast 412 tak punya efek, hanya tidak melempar).</summary>
internal sealed class NoopHubContext : IHubContext<AssessmentHub>
{
    public IHubClients Clients { get; } = new NoopHubClients();
    public IGroupManager Groups { get; } = new NoopGroupManager();

    private sealed class NoopHubClients : IHubClients
    {
        private static readonly IClientProxy Proxy = new NoopClientProxy();
        public IClientProxy All => Proxy;
        public IClientProxy AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Client(string connectionId) => Proxy;
        public IClientProxy Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds) => Proxy;
        public IClientProxy Group(string groupName) => Proxy;
        public IClientProxy GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Groups(System.Collections.Generic.IReadOnlyList<string> groupNames) => Proxy;
        public IClientProxy User(string userId) => Proxy;
        public IClientProxy Users(System.Collections.Generic.IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class NoopClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
