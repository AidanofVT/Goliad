using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignTextLibrary : MonoBehaviour
{

    public const uint TID_BLANK = 0;
    public const uint TID_HELLOWORLD = 1;
    public const uint TID_TIP01 = 2;
    public const uint TID_TIP02 = 3;


    public static string GetText(uint tid)
    {

        switch (tid)
        {
            case TID_BLANK: return "";
            case TID_HELLOWORLD: return "Hello World!";
            case TID_TIP01: return "Use the 'E' key to enter Edit Mode.";
            case TID_TIP02: return "In edit mode, right mouse button can be used to remove edits";

            default: return "ERROR";
        }



    }



}
