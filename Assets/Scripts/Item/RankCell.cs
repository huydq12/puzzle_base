using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class RankCell : MonoBehaviour
{
    private int m_index;
    [SerializeField] private TextMeshProUGUI t_stt;
    [SerializeField] private TextMeshProUGUI t_name;
    [SerializeField] private TextMeshProUGUI t_coin;
    [SerializeField] private TextMeshProUGUI t_item_count;
    [SerializeField] private TextMeshProUGUI t_score;

    [SerializeField] private Sprite tip;
    [SerializeField] private Sprite balloon;
    [SerializeField] private Image item;

    [SerializeField] private Image item_rank;
    [SerializeField] private Sprite item_me;
    [SerializeField] private RectTransform rectTransform;

    public void SetupData(int rank,string name,string item,int score,bool isMe,int n)
    {
        t_stt.text = rank.ToString();
        t_name.text = name;
        t_score.text = score.ToString();
        m_index = n;
        ParseItemString(item);
        if (isMe)
        {
            item_rank.sprite = item_me;
        }
    }

    public void ChangeIndex(int index)
    {

    }

    public void ParseItemString(string itemString)
    {
        // Initialize default values
        int coinAmount = 0;
        int booster_type1Amount = 0;
        int booster_type2Amount = 0;

        // Split the string by ';'
        string[] items = itemString.Split(';');

        foreach (string item in items)
        {
            // Split each part by space
            string[] parts = item.Split(' ');

            if (parts.Length == 2)
            {
                // Extract quantity and item name
                int quantity;
                if (int.TryParse(parts[0], out quantity))
                {
                    string itemName = parts[1];
                    // Update quantities based on item name
                    if (itemName == "coin")
                    {
                        coinAmount = quantity;
                    }
                    else if (itemName == "booster_type1")
                    {
                        booster_type1Amount = quantity;
                    }
                    else if (itemName == "booster_type2")
                    {
                        booster_type2Amount = quantity;
                    }

                    Debug.Log(itemName + " " + quantity);
                }
                else
                {
                    Debug.LogWarning("Quantity parsing failed.");
                }
            }
            else
            {
                Debug.LogWarning("Invalid item format.");
            }
        }

        // Update UI text components
        if (coinAmount > 0)
        {
            t_coin.text = coinAmount.ToString();
        }

        if (booster_type1Amount > 0)
        {
            t_item_count.text = booster_type1Amount.ToString();
            item.sprite = tip;
        }

        if (booster_type2Amount > 0)
        {
            t_item_count.text = booster_type2Amount.ToString();
            item.sprite = balloon;
        }

    }
}
