using System.Text.Json;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace TankCooldownHelper.Services.Ipc;

/// <summary>
/// Exposes local danger telemetry to other plugins via Dalamud IPC (JSON payload).
/// </summary>
internal sealed class DangerIpcProvider : IDisposable
{
    public const string GetLocalDangerSnapshotIpc = "TankCooldownHelper.GetLocalDangerSnapshot";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly DangerSnapshotService _snapshotService;
    private readonly ICallGateProvider<string> _provider;

    public DangerIpcProvider(DangerSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
        _provider = Svc.PluginInterface.GetIpcProvider<string>(GetLocalDangerSnapshotIpc);
        _provider.RegisterFunc(InvokeGetLocalDangerSnapshot);
    }

    private string InvokeGetLocalDangerSnapshot()
    {
        if (!_snapshotService.TryGetLocalSnapshot(out var snapshot))
        {
            return JsonSerializer.Serialize(DangerIpcPayload.Unavailable, JsonOptions);
        }

        var payload = new DangerIpcPayload(
            true,
            snapshot.IncomingDps,
            snapshot.IncomingHps,
            snapshot.DangerRatio,
            (int)snapshot.DangerLevel,
            snapshot.InDanger,
            snapshot.SecondsUntilDeath);

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public void Dispose() => _provider.UnregisterFunc();

    internal sealed record DangerIpcPayload(
        bool Ok,
        float IncomingDps,
        float IncomingHps,
        float DangerRatio,
        int DangerLevel,
        bool InDanger,
        float? SecondsUntilDeath)
    {
        public static DangerIpcPayload Unavailable { get; } = new(false, 0, 0, 0, 0, false, null);
    }
}
