using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    void Awake() => Instance = this;
    public Lobby CurrentLobby { get; private set; }

    float _lastLobbyRefreshTime;
    List<Lobby> _lobbies;
    Coroutine _refreshLobbiesRoutine;


    public bool IsLocalPlayerLobbyHost =>
        CurrentLobby != null && CurrentLobby.HostId == AuthenticationService.Instance?.PlayerId;

    public event Action<List<Lobby>> OnLobbiesUpdated;
    public event Action<Lobby> OnJoinedLobby;
    public event Action OnCurrentLobbyUpdated;
    public event Action OnLobbyHostChanged;
    public event Action OnLeftLobby;


    public async Task<Lobby> HostLobby()
    {
        var trackData = new DataObject(DataObject.VisibilityOptions.Public, "Track 1");
        var options = new CreateLobbyOptions();
        options.Data = new();
        options.Data["track"] = trackData;
        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
            AuthenticationService.Instance.Profile + "'s Game",
            5,
            options);

        OnJoinedLobby?.Invoke(CurrentLobby);

        await UpdatePlayerIdInLobby();

        // Heartbeat the lobby every 15 seconds.
        StartCoroutine(HeartbeatLobbyCoroutine(CurrentLobby.Id, 15));

        LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += HandleCurrentLobbyChanged;
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(CurrentLobby.Id, callbacks);
        return CurrentLobby;
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    public void ToggleAutoRefreshLobbies(bool autoRefreshEnabled)
    {
        if (autoRefreshEnabled)
            _refreshLobbiesRoutine = StartCoroutine(nameof(AutoRefreshLobbies));
        else if (_refreshLobbiesRoutine != null)
            StopCoroutine(_refreshLobbiesRoutine);
    }

    IEnumerator AutoRefreshLobbies()
    {
        while (true)
        {
            yield return null;
            if (AuthenticationService.Instance?.IsSignedIn == true)
            {
                if (Time.time < _lastLobbyRefreshTime + 10f) // hard check to prevent over spamming
                    continue;

                yield return RefreshLobbies();
                yield return new WaitForSeconds(15f);
            }
        }
    }

    async Task UpdatePlayerIdInLobby()
    {
        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions();

            options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {
                    "name", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: await AuthenticationManager.Instance.GetPlayerNameAsync())
                },
                {
                    "id", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: await AuthenticationManager.Instance.GetPlayerIdAsync())
                }
            };

            string playerId = AuthenticationService.Instance.PlayerId;
            CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobby.Id, playerId, options);
            OnCurrentLobbyUpdated?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    //public async void RefreshLobbies() => await RefreshLobbies();

    public async Awaitable RefreshLobbies()
    {
        var lobbies = await LobbyService.Instance.QueryLobbiesAsync();
        _lobbies = lobbies.Results;
        OnLobbiesUpdated?.Invoke(lobbies.Results);
    }

    async void HandleCurrentLobbyChanged(ILobbyChanges changes)
    {
        changes.ApplyToLobby(CurrentLobby);
        if (changes.HostId.Changed)
            OnLobbyHostChanged?.Invoke();

        if (changes.Data.Changed &&
            changes.Data.Value.TryGetValue("relaycode", out var relayCode))
        {
            Debug.Log($"'relaycode' set to '{relayCode.Value.Value}'");
            await SetRelayClientData(relayCode.Value.Value);
        }

        OnCurrentLobbyUpdated?.Invoke();
    }

    public async Task JoinLobby(Lobby lobby)
    {
        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
        Debug.Log($"Joined {CurrentLobby.Name}");
        await UpdatePlayerIdInLobby();
        LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += HandleCurrentLobbyChanged;
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(CurrentLobby.Id, callbacks);

        if (CurrentLobby.Data.TryGetValue("relaycode", out var relayCode))
            await SetRelayClientData(relayCode.Value);

        OnJoinedLobby?.Invoke(CurrentLobby);
    }

    public async void SetTrackInLobby(string trackName)
    {
        var options = new UpdateLobbyOptions()
        {
            Data = new Dictionary<string, DataObject>
            {
                ["track"] = new DataObject(DataObject.VisibilityOptions.Public, trackName)
            }
        };
        CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);
    }

    public async void LeaveCurrentLobby()
    {
        if (CurrentLobby == null)
            return;

        await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        CurrentLobby = null;
        OnLeftLobby?.Invoke();
    }

    public async void RenameLobby(string lobbyName)
    {
        if (!IsLocalPlayerLobbyHost)
            return;

        UpdateLobbyOptions options = new UpdateLobbyOptions();
        options.Name = lobbyName;
        CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);
        OnCurrentLobbyUpdated?.Invoke();
    }

    async Task SetRelayClientData(string relayCode)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>(); ;

        Debug.LogWarning($"Relay code {relayCode}");

        var joinAllocation = await Relay.Instance.JoinAllocationAsync(relayCode);

        var endpoint = GetEndpointForAllocation(
            joinAllocation.ServerEndpoints,
            joinAllocation.RelayServer.IpV4,
            joinAllocation.RelayServer.Port,
            out bool isSecure);

        transport.SetClientRelayData(
            AddressFromEndpoint(endpoint),
            endpoint.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            isSecure);

        StartCoroutine(CountdownThenStartClient());
    }

    IEnumerator CountdownThenStartClient()
    {
        yield return RunCountdownWithEvents();
        StartClient();
    }

    IEnumerator StartServerThenCountdown()
    {
        PlayerConnectionsManager.Instance.StartHostOnServer();
        yield return RunCountdownWithEvents();
    }

    IEnumerator RunCountdownWithEvents()
    {
        int countDownTimer = 3;
        OnCountdownStarted?.Invoke();
        while (countDownTimer > 0)
        {
            yield return new WaitForSeconds(1);
            countDownTimer--;
            OnCountdownUpdated?.Invoke(countDownTimer);
        }
    }

    static async void StartClient()
    {
        RaceGameConnectionData data = new()
        {
            Name = await AuthenticationManager.Instance.GetPlayerNameAsync(),
            Id = await AuthenticationManager.Instance.GetPlayerIdAsync(),
            Car = "Red"
        };
        var json = JsonUtility.ToJson(data);
        Debug.Log(json);
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            System.Text.Encoding.UTF8.GetBytes(json);
        NetworkManager.Singleton.StartClient();
    }



    public async Task RequestStartGame()
    {
        var allocation = await Relay.Instance.CreateAllocationAsync(CurrentLobby.MaxPlayers);
        var relaycode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
        var options = new UpdateLobbyOptions();
        options.Data = new Dictionary<string, DataObject>();
        options.Data["relaycode"] = new DataObject(DataObject.VisibilityOptions.Public, relaycode);
        CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);
        Debug.LogWarning($"Updated Relay code to {CurrentLobby.Data["relaycode"].Value}");

        var endpoint = GetEndpointForAllocation(
            allocation.ServerEndpoints,
            allocation.RelayServer.IpV4,
            allocation.RelayServer.Port,
            out bool isSecure);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(AddressFromEndpoint(endpoint), endpoint.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure);

        StartCoroutine(StartServerThenCountdown());
    }


    string AddressFromEndpoint(NetworkEndpoint endpoint)
    {
        return endpoint.Address.Split(':')[0];
    }

    /// <summary>
    /// Determine the server endpoint for connecting to the Relay server, for either an Allocation or a JoinAllocation.
    /// If DTLS encryption is available, and there's a secure server endpoint available, use that as a secure connection. Otherwise, just connect to the Relay IP unsecured.
    /// </summary>
    NetworkEndpoint GetEndpointForAllocation(
        List<RelayServerEndpoint> endpoints,
        string ip,
        int port,
        out bool isSecure)
    {
#if ENABLE_MANAGED_UNITYTLS
        foreach (RelayServerEndpoint endpoint in endpoints)
        {
            if (endpoint.Secure && endpoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                isSecure = true;
                return NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);
            }
        }
#endif
        isSecure = false;
        return NetworkEndpoint.Parse(ip, (ushort)port);
    }

    public event Action OnCountdownStarted;
    public event Action<int> OnCountdownUpdated;

    public async void JoinLobbyById(string lobbyId)
    {
        await RefreshLobbies();
        var lobby = _lobbies.FirstOrDefault(t => t.Id == lobbyId);
        if (lobby == null)
        {
            Debug.LogError($"Unable to find lobby {lobbyId}");
            return;
        }

        await JoinLobby(lobby);
    }

    public async void HostLobbyAsync()
    {
        await HostLobby();
    }

    public async Task SetLocalPlayerCar(string carDefinitionName)
    {
        var options = new UpdatePlayerOptions() { Data = new() };
        options.Data["car"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, carDefinitionName);
        CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(LobbyManager.Instance.CurrentLobby.Id,
            AuthenticationService.Instance.PlayerId,
            options);
        OnCurrentLobbyUpdated?.Invoke();
    }
}