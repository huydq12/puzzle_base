using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static string ConvertNumber(float number)
    {
        if(number < 10000)
        {
            return Mathf.Floor(number) + "";
        }
        else if(number >= 10000 && number < 10000000)
        {
            return Mathf.Floor(number / 1000) + "K";
        }
        else if(number >= 10000000 && number < 10000000000)
        {
            return Mathf.Floor(number / 1000000) + "M";
        }
        else if(number >= 10000000000 && number < 10000000000000)
        {
            return Mathf.Floor(number / 1000000000) + "B";
        }
        else
        {
            return Mathf.Floor(number / 1000000000000) + "T";
        }
    }
}
