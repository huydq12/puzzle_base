using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils.Pattern;
using TMPro;

public class ItemDailyBonus : MonoBehaviour
{
    [SerializeField] private GameObject selectGameObject;
    [SerializeField] private GameObject completeObject;
    [SerializeField] Image itemImage;
    [SerializeField] TextMeshProUGUI dayIndex;
    [SerializeField] TextMeshProUGUI totalItem;
    int m_Index;
    int m_Total_num;
    
    public void SetUp(int index, Sprite sprite, int Cach, string title)
    {
        SetDefault();
        itemImage.sprite = sprite;
        m_Index = index;
        m_Total_num = Cach;
        dayIndex.text = "Day " + (m_Index + 1).ToString();
        if (m_Total_num != 0)
        {
            totalItem.text = "+" + m_Total_num;
        }
        else
        {
            totalItem.text = title;
        }
        SetActive(index);
    }

    void SetDefault()
    {
        //lockGameObject.SetActive(false);
        completeObject.SetActive(false);
    }

    void SetActive(object index)
    {
        var currentIndexBonus = GameManager.Instance.userData.dailyBonus.currentIndex;

        if(m_Index == currentIndexBonus )
        {
            selectGameObject.SetActive(true);
        }

        if (currentIndexBonus == (m_Index+1))
        {
            completeObject.SetActive(true);
        }
        else if (currentIndexBonus > m_Index)
        {
            completeObject.SetActive(true);
            //lockGameObject.SetActive(false);
        }
        //else
        //{
        //    //lockGameObject.SetActive(true);
        //}    
    }    
}