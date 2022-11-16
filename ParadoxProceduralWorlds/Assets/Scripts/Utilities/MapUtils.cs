using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapUtils
{
    public static int Mod(int a, int n)
    {
        int c = a % n;
        if ((c < 0 && n > 0) || (c > 0 && n < 0))
        {
            c += n;
        }
        return c;
    }
}
