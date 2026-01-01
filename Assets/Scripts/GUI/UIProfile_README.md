# UIProfile - Hướng dẫn sử dụng

## Tổng quan
UIProfile là một popup quản lý profile của người chơi, bao gồm:
- **Avatar**: Danh sách các avatar có thể chọn
- **Frame**: Danh sách các khung ảnh có thể chọn
- **Badge**: Danh sách các huy hiệu (chưa triển khai)

## Cấu trúc Code

### 1. Khởi tạo Items (CreateAvatarItems & CreateFrameItems)
```csharp
private void CreateAvatarItems()
{
    for (int i = 0; i < dataAvatar.listAvatar.Count; i++)
    {
        int index = i; // Capture index for closure
        ProfileItem item = Instantiate(profileItemPrefab, parentAvatar);
        
        bool isLocked = i > 0; // Logic khóa/mở khóa
        bool isSelected = i == currentAvatarIndex;
        
        item.SetData(
            TabProfile.Avatar, 
            dataAvatar.listAvatar[i], 
            isLocked, 
            isSelected,
            true,
            () => OnClickAvatarItem(index)
        );
        
        listAvatar.Add(item);
    }
}
```

**Giải thích:**
- `int index = i;` - Capture biến i để tránh closure issue trong lambda
- `isLocked` - Xác định item có bị khóa không (hiện tại: chỉ item đầu tiên được mở)
- `isSelected` - Xác định item có đang được chọn không
- `SetData()` - Thiết lập dữ liệu cho item với callback khi click

### 2. Xử lý Click Item (OnClickAvatarItem & OnClickFrameItem)
```csharp
private void OnClickAvatarItem(int index)
{
    bool isLocked = index > 0;
    
    if (isLocked)
    {
        Debug.Log("Avatar bị khóa! Cần mở khóa trước.");
        return;
    }

    currentAvatarIndex = index;
    UpdateAvatarSelection();
    
    // Lưu vào UserData nếu cần
    // GameManager.Instance.userData.currentAvatarIndex = index;
    // GameManager.Instance.SaveData();
}
```

**Giải thích:**
- Kiểm tra item có bị khóa không
- Nếu khóa: hiển thị thông báo (có thể mở popup mua)
- Nếu không khóa: cập nhật selection và lưu data

### 3. Cập nhật Selection (UpdateAvatarSelection & UpdateFrameSelection)
```csharp
private void UpdateAvatarSelection()
{
    for (int i = 0; i < listAvatar.Count; i++)
    {
        bool isSelected = i == currentAvatarIndex;
        listAvatar[i].SetItemSelected(isSelected);
    }
}
```

**Giải thích:**
- Duyệt qua tất cả items
- Chỉ item có index = currentIndex mới được đánh dấu selected

### 4. Unlock Items (UnlockAvatar & UnlockFrame)
```csharp
public void UnlockAvatar(int index)
{
    if (index >= 0 && index < listAvatar.Count)
    {
        listAvatar[index].SetItemLocked(false);
    }
}
```

**Cách sử dụng:**
```csharp
// Khi người chơi mua/mở khóa avatar
UIProfile profile = GameUI.Instance.Get<UIProfile>();
profile.UnlockAvatar(2); // Mở khóa avatar thứ 3
```

## ProfileItem Component

### SetData Method
```csharp
public void SetData(
    TabProfile tab,        // Loại tab (Avatar/Frame/Badge)
    GameObject prefab,     // Prefab cần spawn
    bool isLock,          // Item có bị khóa không
    bool isSelected,      // Item có được chọn không
    bool isSpawn,         // Có spawn prefab không
    Action onClickItem    // Callback khi click
)
```

### Public Methods
- `SetItemLocked(bool isLock)` - Hiển thị/ẩn icon khóa
- `SetItemSelected(bool isSelected)` - Hiển thị/ẩn icon check

## Tùy chỉnh Logic Khóa/Mở

### Cách 1: Dựa vào index
```csharp
bool isLocked = i > 0; // Chỉ item đầu tiên được mở
```

### Cách 2: Dựa vào UserData
```csharp
bool isLocked = !GameManager.Instance.userData.unlockedAvatars.Contains(i);
```

### Cách 3: Dựa vào level/điều kiện
```csharp
bool isLocked = GameManager.Instance.userData.level < requiredLevel[i];
```

## Lưu/Load Data

### Thêm vào UserData.cs
```csharp
public class UserData
{
    public int currentAvatarIndex = 0;
    public int currentFrameIndex = 0;
    public List<int> unlockedAvatars = new List<int> { 0 }; // Mặc định mở avatar đầu
    public List<int> unlockedFrames = new List<int> { 0 };
}
```

### Lưu khi chọn item
```csharp
private void OnClickAvatarItem(int index)
{
    // ... code kiểm tra ...
    
    currentAvatarIndex = index;
    GameManager.Instance.userData.currentAvatarIndex = index;
    GameManager.Instance.SaveData();
}
```

### Load khi Show popup
```csharp
public override void Show()
{
    base.Show();
    
    // Load từ UserData
    currentAvatarIndex = GameManager.Instance.userData.currentAvatarIndex;
    currentFrameIndex = GameManager.Instance.userData.currentFrameIndex;
    
    // ... code khác ...
}
```

## Tối ưu hóa

### 1. Tránh tạo lại items mỗi lần Show
```csharp
if (listAvatar.Count == 0)
{
    CreateAvatarItems();
}
```

### 2. Cleanup khi Destroy
```csharp
private void OnDestroy()
{
    btn_changeName?.onClick.RemoveAllListeners();
    btn_tabAvatar?.onClick.RemoveAllListeners();
    // ...
}
```

### 3. Sử dụng Object Pooling (nâng cao)
Nếu có nhiều items, có thể sử dụng Object Pooling để tái sử dụng items thay vì Instantiate mới.

## Mở rộng

### Thêm Badge Tab
1. Tạo DataBadge ScriptableObject
2. Thêm CreateBadgeItems() method
3. Thêm OnClickBadgeItem() method
4. Cập nhật OnClickTab() để hiển thị badge parent

### Thêm Animation
```csharp
private void OnClickAvatarItem(int index)
{
    // ... code kiểm tra ...
    
    // Thêm animation khi chọn
    listAvatar[index].GetComponent<Animator>().SetTrigger("Selected");
}
```

### Thêm Sound Effect
```csharp
private void OnClickAvatarItem(int index)
{
    if (isLocked)
    {
        AudioManager.Instance.PlaySFX("locked");
        return;
    }
    
    AudioManager.Instance.PlaySFX("select");
    // ... code khác ...
}
```

## Lưu ý quan trọng

1. **Closure Issue**: Luôn capture biến `i` thành `index` trong loop
2. **Memory Leak**: Nhớ RemoveAllListeners trong OnDestroy
3. **Null Check**: Kiểm tra null trước khi truy cập components
4. **Performance**: Không tạo lại items mỗi lần Show nếu không cần thiết

