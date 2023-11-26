using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListPanel : MonoBehaviour
{
    [SerializeField] Transform _lobbiesRoot;
    [SerializeField] LobbyEntry _lobbyEntryPrefab;
    [SerializeField] Toggle _autoRefreshToggle;

    public void Initialize()
    {
        _autoRefreshToggle.onValueChanged.AddListener(LobbyManager.Instance.ToggleAutoRefreshLobbies);
        LobbyManager.Instance.ToggleAutoRefreshLobbies(_autoRefreshToggle.isOn);
        LobbyManager.Instance.OnLobbiesUpdated += UpdateLobbiesUI;
        LobbyManager.Instance.OnCountdownUpdated += HideForCountdown;
    }

    void HideForCountdown(int obj)
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance)
            LobbyManager.Instance.OnLobbiesUpdated -= UpdateLobbiesUI;
    }

    void UpdateLobbiesUI(List<Lobby> lobbies)
    {
        for (int i = _lobbiesRoot.childCount - 1; i >= 0; i--)
            Destroy(_lobbiesRoot.GetChild(i).gameObject);

        Debug.Log("RefreshLobby await done");

        foreach (var lobby in lobbies)
        {
            var lobbyPanel = Instantiate(_lobbyEntryPrefab, _lobbiesRoot);
            lobbyPanel.Bind(lobby);
        }
    }
}