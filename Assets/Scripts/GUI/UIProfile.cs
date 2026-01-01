using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TabProfile
{
    Avatar,
    Frame,
    Badge,
    None
}

public enum ButtonStatus
{
    Used,
    NotUsed,
    Save,
    Buy,
    None
}


public class UIProfile : UIPopup
{
    [Header("Button")]
    [SerializeField] private Button btn_changeName;
    [SerializeField] private Button btn_tabAvatar;
    [SerializeField] private Button btn_tabFrame;
    [SerializeField] private Button btn_tabBadge;

    [Header("Img Tab")]

    [SerializeField] private Image img_tabAvatar;
    [SerializeField] private Image img_tabFrame;
    [SerializeField] private Image img_tabBadge;

    [SerializeField] private Sprite img_tabAvatarActive;

    [SerializeField] private Sprite img_tabAvatarInactive;

    [SerializeField] private Sprite img_tabFrameActive;

    [SerializeField] private Sprite img_tabFrameInactive;

    [SerializeField] private Sprite img_tabBadgeActive;

    [SerializeField] private Sprite img_tabBadgeInactive;

    [Header("Group Tab")]

    [SerializeField] private GameObject group_tabAvatar;

    [SerializeField] private GameObject group_tabFrame;

    [SerializeField] private GameObject group_tabBadge;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI text_name;

    [Header("Data")]
    [SerializeField] private DataAvatarCollection dataAvatar;

    [SerializeField] private DataFrameCollection dataFrame;

    [SerializeField] private ProfileItem profileItemPrefab;

    [Header("Parent")]
    [SerializeField] private Transform parentAvatar;
    [SerializeField] private Transform parentFrame;

    public Transform selectAvatar;
    public Transform selectFrame;

    [Header("Item")]
    [SerializeField] private Transform itemAvatar;
    [SerializeField] private Transform itemFrame;

    private readonly Dictionary<int, GameObject> cloneAvatar = new();
    private readonly Dictionary<int, GameObject> cloneFrame = new();

    private TabProfile currentTab = TabProfile.None;
    
    private int currentBadgeIndex = 0;

    private int currentFrameIndex = 0;

    private int currentAvatarIndex = 0;

    private readonly List<ProfileItem> listAvatar = new();
    private readonly List<ProfileItem> listFrame = new();

    [SerializeField] private GameObject info_save;
    [SerializeField] private GameObject info_buy_coin;
    [SerializeField] private GameObject info_ads;
    [SerializeField] private GameObject info_used;
    [SerializeField] private TextMeshProUGUI text_info_coin;
    [SerializeField] private TextMeshProUGUI text_unlock_info;
    [SerializeField] private Button btn_unlock;
    [SerializeField] private Image img_unlock;
    [SerializeField] private Sprite btn_unlock_on;
    [SerializeField] private Sprite btn_unlock_off;
    private ButtonStatus buttonStatus = ButtonStatus.None;
    private int selectedItemIndex = -1;
    private TabProfile selectedItemTab = TabProfile.None;


    protected override void Start()
    {
        base.Start();
        btn_changeName.onClick.AddListener(OnChangeName);
        btn_tabAvatar.onClick.AddListener(() => OnClickTab(TabProfile.Avatar));
        btn_tabFrame.onClick.AddListener(() => OnClickTab(TabProfile.Frame));
        btn_tabBadge.onClick.AddListener(() => OnClickTab(TabProfile.Badge));
        if (btn_unlock != null)
        {
            btn_unlock.onClick.AddListener(OnClickUnlock);
        }
    }

    public void SetSelectedAvatar(int index)
    {
        currentAvatarIndex = index;
        selectAvatar.position = listAvatar[index].transform.position + Vector3.up * 0.02f;
        UpdateAvatarSelection();

        Debug.Log("Set selected avatar: " + index);

        CloneItemAvatar(index);
    }

    public void SetSelectedFrame(int index)
    {
        currentFrameIndex = index;
        selectFrame.position = listFrame[index].transform.position + Vector3.up * 0.02f;
        UpdateFrameSelection();

        Debug.Log("Set selected frame: " + index);

        CloneItemFrame(index);
    }

    public void CloneItemAvatar(int index){
        foreach (var clone in cloneAvatar.Values)
        {
            clone.SetActive(false);
        }

        if (cloneAvatar.ContainsKey(index))
        {
            cloneAvatar[index].SetActive(true);
            return;
        }
        GameObject newClone = Instantiate(dataAvatar.listAvatar[index].Avatar, itemAvatar);
        cloneAvatar[index] = newClone;
    }

    public void CloneItemFrame(int index){
        foreach (var clone in cloneFrame.Values)
        {
            clone.SetActive(false);
        }

        if (cloneFrame.ContainsKey(index))
        {
            cloneFrame[index].SetActive(true);
            return;
        }
        GameObject newClone = Instantiate(dataFrame.listFrame[index].Frame, itemFrame);
        cloneFrame[index] = newClone;
    }

    public override void Show()
    {
        base.Show();
        text_name.text = GameManager.Instance.userData.playerName;

        // Load current index from UserData
        currentAvatarIndex = GameManager.Instance.userData.currentAvatarIndex;
        currentFrameIndex = GameManager.Instance.userData.currentFrameIndex;


        // Open Avatar tab by default
        OnClickTab(TabProfile.Avatar);

        CloneItemAvatar(currentAvatarIndex);
        CloneItemFrame(currentFrameIndex);
        
        Invoke("SelectAvatar", 0.1f);
    }

    public void UpdatePLayerName()
    {
        text_name.text = GameManager.Instance.userData.playerName;
    }

    private void CreateAvatarItems()
    {
        // Create Avatar items list
        for (int i = 0; i < dataAvatar.listAvatar.Count; i++)
        {
            int index = i; // Capture index for closure
            ProfileItem item = Instantiate(profileItemPrefab, parentAvatar);

            // Check from UserData if this avatar is unlocked
            bool isLocked = !GameManager.Instance.userData.unlockedAvatars.Contains(i);
            bool isSelected = i == currentAvatarIndex;

            item.SetData(
                TabProfile.Avatar,
                dataAvatar.listAvatar[i].Avatar,
                isLocked,
                isSelected,
                true,
                () => OnClickAvatarItem(index)
            );
            listAvatar.Add(item);

            Invoke("SelectAvatar", 0.1f);

        }

    }

    public void SelectAvatar()
    {
        SetSelectedAvatar(currentAvatarIndex);
    }

    public void SelectFrame()
    {
        SetSelectedFrame(currentFrameIndex);
    }

    private void CreateFrameItems()
    {
        // Create Frame items list
        for (int i = 0; i < dataFrame.listFrame.Count; i++)
        {
            int index = i; // Capture index for closure
            ProfileItem item = Instantiate(profileItemPrefab, parentFrame);

            // Check from UserData if this frame is unlocked
            bool isLocked = !GameManager.Instance.userData.unlockedFrames.Contains(i);
            bool isSelected = i == currentFrameIndex;

            item.SetData(
                TabProfile.Frame,
                dataFrame.listFrame[i].Frame,
                isLocked,
                isSelected,
                true,
                () => OnClickFrameItem(index)
            );
            listFrame.Add(item);

            Invoke("SelectFrame", 0.1f);
        }
    }

    private void OnClickAvatarItem(int index)
    {
        OnClickProfileItem(index, TabProfile.Avatar);
    }

    private void OnClickFrameItem(int index)
    {
        OnClickProfileItem(index, TabProfile.Frame);
    }

    private void OnClickProfileItem(int index, TabProfile tab)
    {
        Debug.Log($"Selected {tab}: {index}");
        
        bool isLocked = tab == TabProfile.Avatar 
            ? !GameManager.Instance.userData.unlockedAvatars.Contains(index)
            : !GameManager.Instance.userData.unlockedFrames.Contains(index);
        
        if (tab == TabProfile.Avatar)
        {
            ShowUnlockInfo(dataAvatar.listAvatar[index]);
        }
        else
        {
            ShowUnlockInfo(dataFrame.listFrame[index]);
        }


        if (isLocked)
        {
            if (tab == TabProfile.Avatar)
            {
                CloneItemAvatar(index);
            }
            else
            {
                CloneItemFrame(index);
            }
            
            selectedItemIndex = index;
            selectedItemTab = tab;
            return;
        }

        if (tab == TabProfile.Avatar)
        {
            currentAvatarIndex = index;
            GameManager.Instance.userData.currentAvatarIndex = index;
            GameManager.Instance.userData.Save();
            SetSelectedAvatar(index);
        }
        else
        {
            currentFrameIndex = index;
            GameManager.Instance.userData.currentFrameIndex = index;
            GameManager.Instance.userData.Save();
            SetSelectedFrame(index);
        }
        
        HideUnlockInfo();
    }

    private void UpdateAvatarSelection()
    {
        for (int i = 0; i < listAvatar.Count; i++)
        {
            bool isSelected = i == currentAvatarIndex;
            listAvatar[i].SetItemSelected(isSelected);
        }
    }

    private void UpdateFrameSelection()
    {
        for (int i = 0; i < listFrame.Count; i++)
        {
            bool isSelected = i == currentFrameIndex;
            listFrame[i].SetItemSelected(isSelected);
        }
    }


    private void OnChangeName()
    {
        GameUI.Instance.Get<UIChangeName>().Show();
    }

    private void OnClickTab(TabProfile tab)
    {
        currentTab = tab;

        // Reset all tabs to inactive state
        img_tabAvatar.sprite = img_tabAvatarInactive;
        img_tabFrame.sprite = img_tabFrameInactive;
        img_tabBadge.sprite = img_tabBadgeInactive;

        // Hide all parents
        parentAvatar.gameObject.SetActive(false);
        parentFrame.gameObject.SetActive(false);

        group_tabAvatar.SetActive(false);
        group_tabFrame.SetActive(false);
        group_tabBadge.SetActive(false);

        CloneItemAvatar(currentAvatarIndex);
        CloneItemFrame(currentFrameIndex);

        btn_unlock.gameObject.SetActive(false);
        text_unlock_info.text = "";

        // Show selected tab
        switch (tab)
        {
            case TabProfile.Avatar:
                img_tabAvatar.sprite = img_tabAvatarActive;
                parentAvatar.gameObject.SetActive(true);
                group_tabAvatar.SetActive(true);

                // Only create items if not already created
                if (listAvatar.Count == 0)
                {
                    CreateAvatarItems();
                }else{
                    SetSelectedAvatar(currentAvatarIndex);
                }
                
                break;
            case TabProfile.Frame:
                img_tabFrame.sprite = img_tabFrameActive;
                parentFrame.gameObject.SetActive(true);
                group_tabFrame.SetActive(true);

                // Only create items if not already created
        
                if (listFrame.Count == 0)
                {
                    CreateFrameItems();
                }else{
                    SetSelectedFrame(currentFrameIndex);
                }
                break;
            case TabProfile.Badge:
                img_tabBadge.sprite = img_tabBadgeActive;
                group_tabBadge.SetActive(true);
                break;
        }
    }

    public override void Hide()
    {
        base.Hide();
        // Cleanup if needed (don't destroy items as they can be reused)
    }

    /// <summary>
    /// Method to unlock a specific avatar (called when player purchases/unlocks)
    /// </summary>
    public void UnlockAvatar(int index)
    {
        if (index >= 0 && index < dataAvatar.listAvatar.Count)
        {
            // Add to unlocked list in UserData
            if (!GameManager.Instance.userData.unlockedAvatars.Contains(index))
            {
                GameManager.Instance.userData.unlockedAvatars.Add(index);
                GameManager.Instance.userData.Save();
            }

            // Update UI if item was already created
            if (index < listAvatar.Count)
            {
                listAvatar[index].SetItemLocked(false);
            }

            Debug.Log($"Unlocked Avatar {index}");
            text_unlock_info.text = "Unlocked!";
        }
    }

    /// <summary>
    /// Method to unlock a specific frame (called when player purchases/unlocks)
    /// </summary>
    public void UnlockFrame(int index)
    {
        if (index >= 0 && index < dataFrame.listFrame.Count)
        {
            // Add to unlocked list in UserData
            if (!GameManager.Instance.userData.unlockedFrames.Contains(index))
            {
                GameManager.Instance.userData.unlockedFrames.Add(index);
                GameManager.Instance.userData.Save();
            }

            // Update UI if item was already created
            if (index < listFrame.Count)
            {
                listFrame[index].SetItemLocked(false);
            }

            Debug.Log($"Unlocked Frame {index}");
            text_unlock_info.text = "Unlocked!";
        }
    }

    private void ShowUnlockInfo(DataAvatar avatar)
    {
        ShowUnlockInfoGeneric(avatar.unlockType, avatar.unlockLevel, avatar.price);
    }

    private void ShowUnlockInfo(DataFrame frame)
    {
        ShowUnlockInfoGeneric(frame.unlockType, frame.unlockLevel, frame.price);
    }

    private void ShowUnlockInfoGeneric(UnlockType unlockType, int unlockLevel, int price)
    {
        if (text_unlock_info == null) return;

        string unlockText = "";
        bool canUnlock = false;
        int playerLevel = GetPlayerLevel();
        int playerCash = GameManager.Instance.userData.playerCash;
        OffInfo();
        btn_unlock.gameObject.SetActive(true);
        switch (unlockType)
        {
            case UnlockType.Default:
                btn_unlock.gameObject.SetActive(false);
                unlockText = "Unlocked";
                canUnlock = true;
                break;

            case UnlockType.Level:
                info_save.gameObject.SetActive(true);
                img_unlock.sprite = btn_unlock_off;
                unlockText = playerLevel >= unlockLevel 
                    ? $"Reach level {unlockLevel} to unlock" 
                    : $"Need level {unlockLevel} (Current: {playerLevel})";
                canUnlock = playerLevel >= unlockLevel;
                break;

            case UnlockType.Purchase:
                img_unlock.sprite = btn_unlock_on;
                OnInfoBuyCoin(price);
                unlockText = $"Buy for {price} coins";
                canUnlock = playerCash >= price;
                break;

            case UnlockType.LevelOrPurchase:
                info_save.gameObject.SetActive(true);
                img_unlock.sprite = btn_unlock_off;     
                bool levelUnlocked = playerLevel >= unlockLevel;
                bool canBuy = playerCash >= price;
                
                if (levelUnlocked)
                {
                    unlockText = $"Reach level {unlockLevel} to unlock";
                    canUnlock = true;
                }
                else
                {
                    unlockText = $"Level {unlockLevel} or buy for {price} coins";
                    canUnlock = canBuy;
                }
                break;
        }

        text_unlock_info.text = unlockText;
        // if (btn_unlock != null)
        // {
        //     btn_unlock.gameObject.SetActive(true);
        //     btn_unlock.interactable = canUnlock;
        // }
    }

    private void HideUnlockInfo()
    {
        if (text_unlock_info != null)
        {
            text_unlock_info.text = "";
        }
        // if (btn_unlock != null)
        // {
        //     btn_unlock.gameObject.SetActive(false);
        // }
    }

    private void OnClickUnlock()
    {
        if (selectedItemIndex < 0) return;

        if (selectedItemTab == TabProfile.Avatar)
        {
            UnlockAvatarByCondition(selectedItemIndex);
        }
        else if (selectedItemTab == TabProfile.Frame)
        {
            UnlockFrameByCondition(selectedItemIndex);
        }
    }

    private void UnlockAvatarByCondition(int index)
    {
        if (index < 0 || index >= dataAvatar.listAvatar.Count) return;

        DataAvatar avatar = dataAvatar.listAvatar[index];
        if (TryUnlockItem(avatar.unlockType, avatar.unlockLevel, avatar.price))
        {
            UnlockAvatar(index);
            OnClickAvatarItem(index);
        }
    }

    private void UnlockFrameByCondition(int index)
    {
        if (index < 0 || index >= dataFrame.listFrame.Count) return;

        DataFrame frame = dataFrame.listFrame[index];
        if (TryUnlockItem(frame.unlockType, frame.unlockLevel, frame.price))
        {
            UnlockFrame(index);
            OnClickFrameItem(index);
        }
    }

    private bool TryUnlockItem(UnlockType unlockType, int unlockLevel, int price)
    {
        int playerLevel = GetPlayerLevel();
        int playerCash = GameManager.Instance.userData.playerCash;

        switch (unlockType)
        {
            case UnlockType.Default:
                return true;

            case UnlockType.Level:
                if (playerLevel >= unlockLevel)
                {
                    return true;
                }
                text_unlock_info.text = $"Need level {unlockLevel}";
                return false;

            case UnlockType.Purchase:
                if (playerCash >= price)
                {
                    GameManager.Instance.userData.playerCash -= price;
                    GameManager.Instance.userData.Save();
                    GameManager.Instance.UpdateValueData();
                    return true;
                }
                text_unlock_info.text = $"Not enough coins! Need {price} coins";
                return false;

            case UnlockType.LevelOrPurchase:
                if (playerLevel >= unlockLevel)
                {
                    return true;
                }
                if (playerCash >= price)
                {
                    GameManager.Instance.userData.playerCash -= price;
                    GameManager.Instance.userData.Save();
                    GameManager.Instance.UpdateValueData();
                    return true;
                }
                text_unlock_info.text = $"Need level {unlockLevel} or {price} coins";
                return false;

            default:
                return false;
        }
    }

    private void OffInfo(){
        info_save.gameObject.SetActive(false);
        info_buy_coin.gameObject.SetActive(false);
        info_ads.gameObject.SetActive(false);
        info_used.gameObject.SetActive(false);
    }

    private void OnInfoBuyCoin(int price){
        info_buy_coin.gameObject.SetActive(true);
        text_info_coin.text = $"{price}";
    }

    private int GetPlayerLevel()
    {
        if (GameManager.Instance.userData.listMap.Count > 0)
        {
            return GameManager.Instance.userData.listMap[GameManager.Instance.userData.currentMap].mapLevel;
        }
        return 1;
    }

    private void OnDestroy()
    {
        if (btn_changeName != null)
            btn_changeName.onClick.RemoveAllListeners();
        if (btn_tabAvatar != null)
            btn_tabAvatar.onClick.RemoveAllListeners();
        if (btn_tabFrame != null)
            btn_tabFrame.onClick.RemoveAllListeners();
        if (btn_tabBadge != null)
            btn_tabBadge.onClick.RemoveAllListeners();
        if (btn_unlock != null)
            btn_unlock.onClick.RemoveAllListeners();
    }
}
