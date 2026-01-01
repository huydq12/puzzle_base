using System;
using UnityEngine;

public class HeatManager : Singleton<HeatManager>
{
    public const int MAX_HEAT_DAY = 5;
    private const float HEAT_REGEN_HOURS = 24f / MAX_HEAT_DAY;

    public static HeatManager TryGetInstance()
    {
        return FindFirstObjectByType<HeatManager>();
    }

    public static bool TryGetInstance(out HeatManager instance)
    {
        instance = TryGetInstance();
        return instance != null;
    }
    
    private UserData userData;
    
    public event Action OnHeatChanged;
    public event Action OnUnlimitedHeatChanged;
    
    public void Initialize(UserData data)
    {
        userData = data;
        CheckDailyReset();
        CheckUnlimitedHeatExpiry();
        RegenerateHeat();
        Game.Update.AddTask(OnUpdate);
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Game.Update != null)
        {
            Game.Update.RemoveTask(OnUpdate);
        }
    }
    
    public bool HasUnlimitedHeat()
    {
        if (!userData.hasUnlimitedHeat) return false;
        
        if (string.IsNullOrEmpty(userData.unlimitedHeatExpireTime))
        {
            userData.hasUnlimitedHeat = false;
            return false;
        }
        
        DateTime expireTime = DateTime.Parse(userData.unlimitedHeatExpireTime);
        if (DateTime.Now >= expireTime)
        {
            userData.hasUnlimitedHeat = false;
            userData.unlimitedHeatExpireTime = string.Empty;
            userData.Save();
            OnUnlimitedHeatChanged?.Invoke();
            return false;
        }
        
        return true;
    }
    
    public TimeSpan GetUnlimitedHeatTimeRemaining()
    {
        if (!HasUnlimitedHeat()) return TimeSpan.Zero;
        
        DateTime expireTime = DateTime.Parse(userData.unlimitedHeatExpireTime);
        TimeSpan remaining = expireTime - DateTime.Now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
    
    public void AddUnlimitedHeat(float hours)
    {
        DateTime newExpireTime;
        
        if (HasUnlimitedHeat())
        {
            DateTime currentExpire = DateTime.Parse(userData.unlimitedHeatExpireTime);
            newExpireTime = currentExpire.AddHours(hours);
        }
        else
        {
            newExpireTime = DateTime.Now.AddHours(hours);
        }
        
        userData.hasUnlimitedHeat = true;
        userData.unlimitedHeatExpireTime = newExpireTime.ToString();
        userData.Save();
        OnUnlimitedHeatChanged?.Invoke();
    }
    
    public bool CanPlay()
    {
        if (HasUnlimitedHeat()) return true;
        return userData.playerHeat > 0;
    }
    
    public void ConsumeHeat()
    {
        if (HasUnlimitedHeat()) return;
        
        if (userData.playerHeat > 0)
        {
            userData.playerHeat--;
            userData.lastTimePlayGame = DateTime.Now.ToString();
            userData.Save();
            OnHeatChanged?.Invoke();
        }
    }
    
    public int GetCurrentHeat()
    {
        if (HasUnlimitedHeat()) return MAX_HEAT_DAY;
        return userData.playerHeat;
    }
    
    public TimeSpan GetTimeUntilNextHeat()
    {
        if (HasUnlimitedHeat()) return TimeSpan.Zero;
        if (userData.playerHeat >= MAX_HEAT_DAY) return TimeSpan.Zero;
        if (string.IsNullOrEmpty(userData.lastTimePlayGame)) return TimeSpan.Zero;
        
        DateTime lastPlayTime = DateTime.Parse(userData.lastTimePlayGame);
        DateTime nextHeatTime = lastPlayTime.AddHours(HEAT_REGEN_HOURS);
        TimeSpan timeRemaining = nextHeatTime - DateTime.Now;
        
        return timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero;
    }
    
    private void CheckDailyReset()
    {
        if (string.IsNullOrEmpty(userData.lastDailyResetTime))
        {
            userData.lastDailyResetTime = DateTime.Now.ToString();
            userData.playerHeat = MAX_HEAT_DAY;
            userData.Save();
            return;
        }
        
        DateTime lastReset = DateTime.Parse(userData.lastDailyResetTime);
        DateTime now = DateTime.Now;
        DateTime todayMidnight = now.Date;
        DateTime lastResetMidnight = lastReset.Date;
        
        if (todayMidnight > lastResetMidnight)
        {
            userData.playerHeat = MAX_HEAT_DAY;
            userData.lastDailyResetTime = now.ToString();
            userData.lastTimePlayGame = string.Empty;
            userData.Save();
            OnHeatChanged?.Invoke();
        }
    }
    
    private void CheckUnlimitedHeatExpiry()
    {
        if (userData.hasUnlimitedHeat && !string.IsNullOrEmpty(userData.unlimitedHeatExpireTime))
        {
            DateTime expireTime = DateTime.Parse(userData.unlimitedHeatExpireTime);
            if (DateTime.Now >= expireTime)
            {
                userData.hasUnlimitedHeat = false;
                userData.unlimitedHeatExpireTime = string.Empty;
                userData.Save();
                OnUnlimitedHeatChanged?.Invoke();
            }
        }
    }
    
    private void RegenerateHeat()
    {
        if (HasUnlimitedHeat()) return;
        if (userData.playerHeat >= MAX_HEAT_DAY) return;
        if (string.IsNullOrEmpty(userData.lastTimePlayGame)) return;
        
        DateTime lastPlayTime = DateTime.Parse(userData.lastTimePlayGame);
        TimeSpan timePassed = DateTime.Now - lastPlayTime;
        
        int heatsToAdd = Mathf.FloorToInt((float)timePassed.TotalHours / HEAT_REGEN_HOURS);
        
        if (heatsToAdd > 0)
        {
            userData.playerHeat = Mathf.Min(userData.playerHeat + heatsToAdd, MAX_HEAT_DAY);
            
            if (userData.playerHeat >= MAX_HEAT_DAY)
            {
                userData.lastTimePlayGame = string.Empty;
            }
            else
            {
                DateTime newLastPlayTime = lastPlayTime.AddHours(heatsToAdd * HEAT_REGEN_HOURS);
                userData.lastTimePlayGame = newLastPlayTime.ToString();
            }
            
            userData.Save();
            OnHeatChanged?.Invoke();
        }
    }
    
    private void OnUpdate()
    {
        CheckDailyReset();
        CheckUnlimitedHeatExpiry();
        RegenerateHeat();
    }
}
