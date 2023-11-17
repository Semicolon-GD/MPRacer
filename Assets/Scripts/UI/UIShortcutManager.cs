using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShortcutManager : MonoBehaviour
{
    [SerializeField] Button _buttonPrefab;
    [SerializeField] Transform _buttonPanel;

    List<Button> _buttons = new();

    void Start()
    {
        ShortcutManager.OnShortcutsChanged += RefreshButtons;
        RefreshButtons();
    }

    void AddButton(string name, Action action)
    {
        var button = Instantiate(_buttonPrefab, _buttonPanel);
        button.GetComponentInChildren<TMP_Text>().SetText(name);
        button.onClick.AddListener(() => action());
        _buttons.Add(button);
    }

    void RefreshButtons()
    {
        for (var i = _buttons.Count - 1; i >= 0; i--)
            Destroy(_buttons[i].gameObject);
        _buttons.Clear();
        
        foreach (var shortcut in ShortcutManager.Shortcuts)
            AddButton(shortcut.Key, shortcut.Value);
    }
}