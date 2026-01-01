using UnityEngine;

public class InventoryManager : Singleton<InventoryManager>
{
    private UserData GetUserData()
    {
        var gm = GameManagerInGame.Instance;
        if (gm != null && gm.userData != null)
        {
            return gm.userData;
        }
        return Game.Data.Load<UserData>();
    }

    public int GetCoin()
    {
        return GetUserData().playerCash;
    }

    public int GetDiamond()
    {
        return GetUserData().playerDiamond;
    }

    public int GetHealth()
    {
        return GetUserData().playerHealth;
    }

    public int GetBoosterType1()
    {
        return GetUserData().boosterType1;
    }

    public int GetBoosterType2()
    {
        return GetUserData().boosterType2;
    }

    public int GetBoosterType3()
    {
        return GetUserData().boosterType3;
    }

    public int GetBoosterType4()
    {
        return GetUserData().boosterType4;
    }

    public void AddCoin(int amount)
    {
        var ud = GetUserData();
        ud.playerCash += amount;
        ud.Save();
    }

    public void AddDiamond(int amount)
    {
        var ud = GetUserData();
        ud.playerDiamond += amount;
        ud.Save();
    }

    public void AddHealth(int amount)
    {
        var ud = GetUserData();
        ud.playerHealth += amount;
        ud.Save();
    }

    public void AddBoosterType1(int amount)
    {
        var ud = GetUserData();
        ud.boosterType1 += amount;
        ud.Save();
    }

    public void AddBoosterType2(int amount)
    {
        var ud = GetUserData();
        ud.boosterType2 += amount;
        ud.Save();
    }

    public void AddBoosterType3(int amount)
    {
        var ud = GetUserData();
        ud.boosterType3 += amount;
        ud.Save();
    }

    public void AddBoosterType4(int amount)
    {
        var ud = GetUserData();
        ud.boosterType4 += amount;
        ud.Save();
    }

    public bool SpendCoin(int amount)
    {
        var ud = GetUserData();
        if (ud.playerCash >= amount)
        {
            ud.playerCash -= amount;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool SpendDiamond(int amount)
    {
        var ud = GetUserData();
        if (ud.playerDiamond >= amount)
        {
            ud.playerDiamond -= amount;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool SpendHealth(int amount)
    {
        var ud = GetUserData();
        if (ud.playerHealth >= amount)
        {
            ud.playerHealth -= amount;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool UseBoosterType1()
    {
        var ud = GetUserData();
        if (ud.boosterType1 > 0)
        {
            ud.boosterType1--;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool UseBoosterType2()
    {
        var ud = GetUserData();
        if (ud.boosterType2 > 0)
        {
            ud.boosterType2--;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool UseBoosterType3()
    {
        var ud = GetUserData();
        if (ud.boosterType3 > 0)
        {
            ud.boosterType3--;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool UseBoosterType4()
    {
        var ud = GetUserData();
        if (ud.boosterType4 > 0)
        {
            ud.boosterType4--;
            ud.Save();
            return true;
        }
        return false;
    }

    public bool HasEnoughCoin(int amount)
    {
        return GetUserData().playerCash >= amount;
    }

    public bool HasEnoughDiamond(int amount)
    {
        return GetUserData().playerDiamond >= amount;
    }

    public bool HasEnoughHealth(int amount)
    {
        return GetUserData().playerHealth >= amount;
    }

    public bool HasBoosterType1()
    {
        return GetUserData().boosterType1 > 0;
    }

    public bool HasBoosterType2()
    {
        return GetUserData().boosterType2 > 0;
    }

    public bool HasBoosterType3()
    {
        return GetUserData().boosterType3 > 0;
    }

    public bool HasBoosterType4()
    {
        return GetUserData().boosterType4 > 0;
    }
}
