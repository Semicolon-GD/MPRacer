using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CurrentLobbyPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _headerText;
    [SerializeField] TMP_Text _playersText;
    [SerializeField] TMP_Text _countdownTimerText;
    [SerializeField] GameObject _countdownTimerPanel;
    [SerializeField] Button _leaveButton;
    [SerializeField] Button _renameLobbyButton;
    [SerializeField] Button _confirmRenameLobbyButton;
    [SerializeField] Button _cancelRenameLobbyButton;
    [SerializeField] TMP_InputField _renameLobbyInput;
    [SerializeField] GameObject _renameLobbyPanel;
    [SerializeField] Button _startGameButton;
    [SerializeField] Button _readyToggleButton;

    List<TrackSelectionToggle> _tracks;
    bool _isReadyClient;

    public static CurrentLobbyPanel Instance { get; private set; }

    void Awake() => Instance = this;

    public void Initialize()
    {
        LobbyManager.Instance.OnJoinedLobby += JoinedLobby;
        LobbyManager.Instance.OnLeftLobby += LeftLobby;
        LobbyManager.Instance.OnLobbyHostChanged += LobbyHostChanged;
        LobbyManager.Instance.OnCurrentLobbyUpdated += CurrentLobbyUpdated;
        LobbyManager.Instance.OnCountdownUpdated += CountdownUpdated;
        _tracks = GetComponentsInChildren<TrackSelectionToggle>().ToList();
        _leaveButton.onClick.AddListener(LobbyManager.Instance.LeaveCurrentLobby);
        _renameLobbyButton.onClick.AddListener(OpenRenameLobbyPanel);
        _confirmRenameLobbyButton.onClick.AddListener(ConfirmRenameLobby);
        _cancelRenameLobbyButton.onClick.AddListener(CancelRenameLobby);
        _startGameButton.onClick.AddListener(TryStartGame);
        _readyToggleButton.onClick.AddListener(ToggleReadyStateOnNonHost);

        _renameLobbyPanel.SetActive(false);
        _countdownTimerText.gameObject.SetActive(false);
        _countdownTimerPanel.SetActive(false);
    }

    async void ToggleReadyStateOnNonHost()
    {
        _isReadyClient = !_isReadyClient;
        _readyToggleButton.GetComponentInChildren<TMP_Text>().color = _isReadyClient ? Color.green : Color.red;
        await LobbyManager.Instance.SetClientReadyState(_isReadyClient);
    }

    void OnEnable()
    {
        _startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLocalPlayerLobbyHost);
        _startGameButton.interactable = LobbyManager.Instance.IsLocalPlayerLobbyHost;
        _readyToggleButton.gameObject.SetActive(!LobbyManager.Instance.IsLocalPlayerLobbyHost);
        _readyToggleButton.interactable = !LobbyManager.Instance.IsLocalPlayerLobbyHost;
    }

    async void TryStartGame()
    {
        _startGameButton.interactable = false;
        await LobbyManager.Instance.RequestStartGame();
    }

    public void CountdownUpdated(int timeRemaining)
    {
        gameObject.SetActive(false);
        _countdownTimerText.gameObject.SetActive(timeRemaining > 0);
        _countdownTimerPanel.SetActive(timeRemaining > 0);

        _countdownTimerText.SetText(timeRemaining.ToString());
    }

    void CancelRenameLobby()
    {
        _renameLobbyPanel.SetActive(false);
    }

    void ConfirmRenameLobby()
    {
        string lobbyName = _renameLobbyInput.text;
        if (!string.IsNullOrWhiteSpace(lobbyName))
            LobbyManager.Instance.RenameLobby(lobbyName);
        _renameLobbyPanel.SetActive(false);
    }

    void OpenRenameLobbyPanel()
    {
        _renameLobbyPanel.SetActive(true);
        _renameLobbyInput.text = LobbyManager.Instance.CurrentLobby.Name;
    }

    void LobbyHostChanged()
    {
        HandleLobbyStateChanged();
    }

    void LeftLobby() => gameObject.SetActive(false);

    void CurrentLobbyUpdated()
    {
        _headerText.SetText(LobbyManager.Instance.CurrentLobby.Name);
        UpdateSelectedTrack();
        UpdatePlayersText();
    }

    void UpdateSelectedTrack()
    {
        if (LobbyManager.Instance.CurrentLobby.Data.TryGetValue("track", out var trackName))
        {
            var trackToggle = _tracks.FirstOrDefault(t => t.TrackName == trackName.Value);
            if (trackToggle != null)
                trackToggle.GetComponent<Toggle>().isOn = true;
        }
    }

    void JoinedLobby(Lobby obj)
    {
        gameObject.SetActive(true);
        _headerText.SetText(obj.Name);
        HandleLobbyStateChanged();

        UpdatePlayersText();
        UpdateSelectedTrack();
    }

    void HandleLobbyStateChanged()
    {
        _renameLobbyButton.interactable = LobbyManager.Instance.IsLocalPlayerLobbyHost;
        var notReadyPlayers = new List<Player>();
        foreach (var player in LobbyManager.Instance.CurrentLobby.Players)
        {
            if (player.Data == null)
                notReadyPlayers.Add(player);
            else if (player.Data.TryGetValue("isReady", out var isReady) == false)
                notReadyPlayers.Add(player);
            else if (isReady?.Value == "false")
                notReadyPlayers.Add(player);
        }

        foreach (var player in notReadyPlayers)
        {
            Debug.LogWarning($"Player not ready {player.Id} - {player.Data?["name"]}");
        }

        _startGameButton.interactable = LobbyManager.Instance.IsLocalPlayerLobbyHost;
        foreach (var track in _tracks)
        {
            track.SetInteractable(LobbyManager.Instance.IsLocalPlayerLobbyHost);
        }
    }

    void UpdatePlayersText()
    {
        StringBuilder b = new();
        foreach (var player in LobbyManager.Instance.CurrentLobby.Players)
        {
            if (player == default)
            {
                Debug.Log("Null Player");
                continue;
            }

            Debug.Log(player.ConnectionInfo);
            if (player.Data == null)
            {
                Debug.Log($"Null Player Data for {player}");
                continue;
            }

            if (player.Data.TryGetValue("name", out var playerName))
            {
                b.Append(playerName.Value);
                if (player.Data.TryGetValue("car", out var carName))
                    b.Append(" - " + carName.Value);
                if (player.Data.TryGetValue("isready", out var isReady))
                {
                    string readyText = isReady.Value == "true" ? " - READY" : " - NOT READY";
                    b.Append(readyText);
                }

                b.AppendLine();
            }
        }

        _playersText.SetText(b);
    }
}