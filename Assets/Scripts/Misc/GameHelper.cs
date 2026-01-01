using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;
using System;

public static class GameHelper
{
    private static StringBuilder StringBuilder = new StringBuilder();
    private static List<RaycastResult> RaycastResults = new List<RaycastResult>();
    private static PointerEventData PointerEventData = new PointerEventData(EventSystem.current);
    public static bool IsPointerOverGameObject(Vector2 screenPosition)
    {
        RaycastResults.Clear();
        PointerEventData.position = screenPosition;
        EventSystem.current.RaycastAll(PointerEventData, RaycastResults);
        return RaycastResults.Count > 0;
    }
    public static string CreateText(string format, params object[] args)
    {
        StringBuilder.Remove(0, StringBuilder.Length);
        return StringBuilder.AppendFormat(format, args).ToString();
    }
    public static string CreateText(params object[] args)
    {
        StringBuilder.Remove(0, StringBuilder.Length);
        foreach (string arg in args) StringBuilder.Append(arg);
        return StringBuilder.ToString();
    }
    private readonly static string[] suffixes = { "", "k", "M", "G" };
    public static string GetPrettyCurrency(int cash)
    {
        int k;
        if (cash == 0)
            k = 0;    // log10 of 0 is not valid
        else
            k = (int)(Math.Log10(cash) / 3); // get number of digits and divide by 3
        var dividor = Math.Pow(10, k * 3);  // actual number we print
        var text = ((int)((cash / dividor))) + suffixes[k];
        if (cash > 1000)
        {
            text = "" + (cash / dividor).ToString("F1") + suffixes[k];
        }
        return text;
    }
    public static string GetPrettyCurrencyDiamond(int diamond)
    {
        int k;
        if (diamond == 0)
            k = 0;    // log10 of 0 is not valid
        else
            k = (int)(Math.Log10(diamond) / 3); // get number of digits and divide by 3
        var dividor = Math.Pow(10, k * 3);  // actual number we print
        var text = ((int)((diamond / dividor))) + suffixes[k];
        if (diamond > 1000)
        {
            text = "" + (diamond / dividor).ToString("F1") + suffixes[k];
        }
        return text;
    }
}