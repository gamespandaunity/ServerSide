using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtentions
{
    public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
    {
        v.x = x != null ? (float)x : v.x;
        v.y = y != null ? (float)y : v.y;
        v.z = z != null ? (float)z : v.z;
        return v;
    }


    public static Vector3 WithAddition(this Vector3 v, float? x = null, float? y = null, float? z = null)
    {
        v.x = x != null ? v.x + (float)x : v.x;
        v.y = y != null ? v.y + (float)y : v.y;
        v.z = z != null ? v.z + (float)z : v.z;
        return v;
   }

    
}
