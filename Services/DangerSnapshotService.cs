using ECommons.DalamudServices;
using TankCooldownHelper.Core;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Services;

public sealed class DangerSnapshotService
{
    private readonly Configuration _config;
    private readonly DangerCalculator _dangerCalculator;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;
    private readonly Predictor _predictor;

    public DangerSnapshotService(
        Configuration config,
        DangerCalculator dangerCalculator,
        DamageTracker damageTracker,
        HealingTracker healingTracker,
        Predictor predictor)
    {
        _config = config;
        _dangerCalculator = dangerCalculator;
        _damageTracker = damageTracker;
        _healingTracker = healingTracker;
        _predictor = predictor;
    }

    public bool TryGetLocalSnapshot(out TankDangerSnapshot snapshot)
    {
        snapshot = default;

        if (!_config.EnableParseLord5Integration)
            return false;

        if (!Svc.ClientState.IsLoggedIn)
            return false;

        if (Svc.Objects.LocalPlayer is not { } player)
            return false;

        var targetId = (uint)player.GameObjectId;
        var maxHp = player.MaxHp;
        var dps = _damageTracker.GetIncomingDps(targetId);
        var hps = _healingTracker.GetIncomingHps(targetId);
        var ratio = _dangerCalculator.CalculateDangerRatio(dps, hps);
        var level = _dangerCalculator.CalculateDangerLevel(dps, hps, maxHp);
        var netDps = Math.Max(0f, dps - hps);
        var ttd = _predictor.PredictTimeOfDeath(player.CurrentHp, dps, hps);
        var inDanger = level >= DangerLevel.Warning;

        snapshot = new TankDangerSnapshot(
            true,
            dps,
            hps,
            ratio,
            level,
            inDanger,
            netDps,
            ttd);

        return true;
    }
}
