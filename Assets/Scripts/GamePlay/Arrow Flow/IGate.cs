using System.Collections.Generic;
using UnityEngine;

public interface IGate
{
    Transform RootTransform { get; }
    bool IsClosed { get; }
    bool IsShooterFrozen { get; }
    int RemainingShooterCount { get; }
    IEnumerable<Shooter> GetCurrentShooters();
    bool ShuffleRemainingShooters();
    int ReduceNonCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false);
    int ReduceCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false);
    void ConsumeIceCounter(int amount);
    bool UnlockCurrentLock(Key key = null);
    bool UnlockNextLock(Key key = null);
    void CollectCurrentShooter();
}
