namespace TankCooldownHelper.Data;

/// <summary>
/// Local-player danger telemetry for IPC consumers (e.g. ParseLord5 smart mitigation).
/// </summary>
public readonly record struct TankDangerSnapshot(
    bool Available,
    float IncomingDps,
    float IncomingHps,
    float DangerRatio,
    DangerLevel DangerLevel,
    bool InDanger,
    float NetDps,
    float? SecondsUntilDeath);
