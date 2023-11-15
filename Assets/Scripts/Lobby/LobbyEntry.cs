using System;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyEntry : MonoBehaviour
{
    [SerializeField] TMP_Text _name;
    [SerializeField] TMP_Text _playerCount;
    [SerializeField] Button _joinButton;
    [SerializeField] TMP_Text _trackName;

    Lobby _lobby;

    void Start()
    {
        _joinButton.onClick.AddListener(TryJoin);
    }

    async void TryJoin()
    {
        try
        {
            await LobbyManager.Instance.JoinLobby(_lobby);
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogError(exception);
        }
    }

    public void Bind(Lobby lobby)
    {
        _lobby = lobby;
        _name.SetText(lobby.Name);
        _playerCount.SetText(lobby.Players.Count + " / " + lobby.MaxPlayers);
        string track = "Not Picked";
        if (_lobby?.Data != null && _lobby.Data.TryGetValue("track", out var trackData))
            track = trackData.Value;
        _trackName.SetText(track);

        if (_lobby.Id == LobbyManager.Instance.CurrentLobby?.Id)
            _joinButton.interactable = false;
    }
}