using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILoadingInGame : Singleton<UILoadingInGame>
{
    [SerializeField] public GameObject holder;

    private GameObject HolderOrSelf()
    {
        return holder != null ? holder : gameObject;
    }

    public void Show()
    {
        HolderOrSelf().SetActive(true);
    }

    public void Hide()
    {
        HolderOrSelf().SetActive(false);
    }
}
