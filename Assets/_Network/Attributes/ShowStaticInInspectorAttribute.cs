using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class ShowStaticInInspectorAttribute : PropertyAttribute
{
    public bool ReadOnly { get; set; }
    
    public ShowStaticInInspectorAttribute(bool readOnly = false)
    {
        ReadOnly = readOnly;
    }
}

// Marker interface - Classes that implement this will have static fields shown
public interface IHasStaticInspector { }