using System;

[AttributeUsage(AttributeTargets.Method)]
public class ShortcutAttribute : Attribute
{
    public string Key { get; private set; }

    public ShortcutAttribute(string key)
    {
        Key = key;
    }
}