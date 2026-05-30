using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;

public class ContentFitterRefresh : MonoBehaviour
{
	[SerializeField] private bool _refreshOnEnable = true;

	private void OnEnable()
	{
		if (_refreshOnEnable)
		{
			RefreshContentFitters();
		}
	}

	public void RefreshContentFitters()
	{
		try
		{
			var rectTransform = (RectTransform)transform;
			StartCoroutine(RefreshLayoutCoroutine(rectTransform));
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	private IEnumerator RefreshLayoutCoroutine(RectTransform root)
	{
		var tmpComponents = root.GetComponentsInChildren<TextMeshProUGUI>(true);
		foreach (var tmp in tmpComponents)
		{
			if (tmp.gameObject.activeSelf)
			{
				tmp.ForceMeshUpdate(true, true);
			}
		}

		yield return null;

		RefreshContentFitterBottomUp(root);

		yield return null;
		LayoutRebuilder.ForceRebuildLayoutImmediate(root);
	}

	private void RefreshContentFitterBottomUp(RectTransform root)
	{
		if (root == null || !root.gameObject.activeSelf)
		{
			return;
		}

		foreach (Transform child in root)
		{
			if (child is RectTransform rectChild)
			{
				RefreshContentFitterBottomUp(rectChild);
			}
		}

		var layoutGroup = root.GetComponent<LayoutGroup>();
		var contentSizeFitter = root.GetComponent<ContentSizeFitter>();

		if (layoutGroup != null)
		{
			LayoutRebuilder.MarkLayoutForRebuild(root);
		}

		if (contentSizeFitter != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(root);
		}
	}
}