using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ProfileItem : MonoBehaviour
{
	[SerializeField]
	[Header("-----Avatar-----")]
	private Vector2 avatarScale;

	[SerializeField]
	private Transform avatarHolder;

	[SerializeField]
	[Header("-----Frame-----")]
	private Transform frameHolder;

	[SerializeField]
	[Space]
	private GameObject lockObject;

	[SerializeField]
	private GameObject checkObject;


	private GameObject currentAvatar;

	private GameObject currentFrame;

	private Action onClickItemAction;

	private void Awake()
	{
		Button button = GetComponent<Button>();
		if (button != null)
		{
			button.onClick.AddListener(OnClickItem);
		}
		RefreshUI();
	}

	private void RefreshUI()
	{
		SetItemLocked(false);
		SetItemSelected(false);
	}

	private void OnClickItem()
	{
		onClickItemAction?.Invoke();
	}

	public void SetItemLocked(bool isLock)
	{
		lockObject.SetActive(isLock);
	}

	public void SetItemSelected(bool isSelected)
	{
		checkObject.SetActive(isSelected);
	}

	public void SetData(TabProfile tab, GameObject prefab, bool isLock, bool isSelected, bool isSpawn, Action onClickItem)
	{
		// Lưu callback
		onClickItemAction = onClickItem;

		// Cập nhật trạng thái UI
		SetItemLocked(isLock);
		SetItemSelected(isSelected);

		// Set active cho holder dựa vào tab (luôn thực hiện, không phụ thuộc isSpawn)
		switch (tab)
		{
			case TabProfile.Avatar:
				avatarHolder.gameObject.SetActive(true);
				frameHolder.gameObject.SetActive(false);
				break;
			case TabProfile.Frame:
				avatarHolder.gameObject.SetActive(false);
				frameHolder.gameObject.SetActive(true);
				break;
			case TabProfile.Badge:
				avatarHolder.gameObject.SetActive(false);
				frameHolder.gameObject.SetActive(false);
				break;
		}

		// Chỉ spawn prefab nếu isSpawn = true
		if (!isSpawn)
		{
			return;
		}

		// Spawn prefab
		switch (tab)
        {
			case TabProfile.Avatar:
				if (currentAvatar != null)
				{
					Destroy(currentAvatar);
				}
				currentAvatar = Instantiate(prefab, avatarHolder);
				currentAvatar.transform.localScale = avatarScale;
				currentAvatar.transform.localPosition = Vector3.zero;
                break;

			case TabProfile.Frame:
				if (currentFrame != null)
				{
					Destroy(currentFrame);
				}
				currentFrame = Instantiate(prefab, frameHolder);
				currentFrame.transform.localPosition = Vector3.zero;
				currentFrame.transform.localScale = Vector3.one;
                break;

            case TabProfile.Badge:
                break;
        }
    }

	
}
