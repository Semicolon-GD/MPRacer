using System;
using System.Collections.Generic;
using System.Linq;

public static class ShortcutManager
{
    public static Dictionary<string, Action> Shortcuts = new();
    public static event Action OnShortcutsChanged;

    public static void Add(string name, Action action)
    {
        Shortcuts[name] = action;
        // foreach(var shortcut in Shortcuts.ToList())
        //     if (shortcut.Value == null)
        //         Shortcuts.Remove(shortcut.Key);
        OnShortcutsChanged?.Invoke();
    }
}