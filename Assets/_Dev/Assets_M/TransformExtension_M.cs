using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtension_M
{
    ///<summary>    
    ///[Mohsin] Destroy All of the Transform's Children
    /// </summary>
    /// <param name="transform">The Parent</param>
    public static void Clear(this Transform transform)
    {
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(null);
            Object.Destroy(child.gameObject, 0.01f);
        }
    }

    ///<summary>    
    /// Destroy All of the Transform's Children of Given Name
    /// </summary>
    /// <param name="transform">The Parent</param>
    public static void ClearByName(this Transform transform, string name)
    {
        int index = transform.childCount;
        for (int i = index-1; i > 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == name)
            {
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
    ///<summary>    
    /// Destroy All of the Transform's Children with contain given string
    /// </summary>
    /// <param name="transform">The Parent</param>
    public static void ClearByNameContain(this Transform transform, string name)
    {
        int index = transform.childCount;
        for (int i = index - 1; i >= 0; i--)  // Looping in reverse
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains(name))
            {
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
    /// <param name="gameObject">The Parent</param>
    public static void ChangeName(this GameObject gameObject, string name)
    {
        gameObject.name = name;
    }
}