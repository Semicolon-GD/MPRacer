using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIConsoleManager : MonoBehaviour
{
    [SerializeField] Button _toggleConsoleButton;
    [SerializeField] GameObject _consolePanel;
    [SerializeField] TMP_Text _text;

    static ObservableCollection<string> _logEntries = new();

    void Start()
    {
        _toggleConsoleButton.onClick.AddListener(ToggleConsoleVisibility);
        _logEntries.CollectionChanged += HandleCollectionChanged;
    }

    void OnEnable()
    {
        AddLog("Log Console Opened");
        RefreshLogText();
    }

    void OnDestroy()
    {
        _logEntries.CollectionChanged -= HandleCollectionChanged;
    }

    void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshLogText();
    }

    void RefreshLogText()
    {
        string allText = String.Join(System.Environment.NewLine, _logEntries);
        _text.SetText(allText);
    }

    void ToggleConsoleVisibility()
    {
        _consolePanel.SetActive(!_consolePanel.activeSelf);
        _logEntries.CollectionChanged += HandleCollectionChanged;
    }


    public static void AddLog(string message)
    {
        _logEntries.Add(message);
    }
}