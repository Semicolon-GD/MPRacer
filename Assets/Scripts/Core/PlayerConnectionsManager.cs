using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class PlayerConnectionsManager : NetworkBehaviour
{
    [SerializeField] NetworkPlayer _playerPrefab;
    public static PlayerConnectionsManager Instance { get; private set; }
    void Awake() => Instance = this;

    public Dictionary<ulong, string> PlayerConnectionsToNames = new();
    public Dictionary<ulong, string> PlayerConnectionToCars = new();
    public List<string> PlayerNames;
    public string LocalCarSelection { get; set; }


    public string GetName(ulong connectionId) => PlayerConnectionsToNames.GetValueOrDefault(connectionId, "Unknown");

    public string GetCar(ulong connectionId) => PlayerConnectionToCars.GetValueOrDefault(connectionId, "Unknown");

    void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = HandleConnectionApprovalOnServer;
    }

    public async Awaitable StartHostOnServer()
    {
        await AddPayloadData();
        NetworkManager.Singleton.StartHost();
    }

    async Task AddPayloadData()
    {
        #if UNITY_EDITOR // Editor only Logging
            var profileName = FindFirstObjectByType<MPPMManager>().profileName;
            Debug.Log($"Getting PlayerName for Profile {profileName}");
        #endif

        string playerName = await AuthenticationManager.Instance.GetPlayerNameAsync();
        await Awaitable.WaitForSecondsAsync(1f);
        string id = await AuthenticationManager.Instance.GetPlayerIdAsync();
        string car = LocalCarSelection ?? "red";

        RaceGameConnectionData data = new()
        {
            Name = playerName,
            Id = id,
            Car = car
        };
        var json = JsonUtility.ToJson(data);
        Debug.Log(json);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(json);
        Debug.LogError("1. Added Payload Data approval for " + data.Name + " on Client");
    }

    Dictionary<string, NetworkPlayer> _playerConnections = new();

    void HandleConnectionApprovalOnServer(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        var payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        Debug.Log(payload);
        var data = JsonUtility.FromJson<RaceGameConnectionData>(payload);
        Debug.Log($"Connection Approval {payload}");

        PlayerConnectionsToNames[request.ClientNetworkId] = data.Name;
        PlayerConnectionToCars[request.ClientNetworkId] = data.Car;
        PlayerNames.Add(data.Name);

        Debug.Log("Set ClientId " + request.ClientNetworkId + " to " + data.Name + " " + data.Car);
        response.Approved = true;

        _playerConnections.TryGetValue(data.Name, out var existingPlayer);
        if (existingPlayer != null)
        {
            existingPlayer.GetComponent<NetworkObject>().Despawn();
            _playerConnections.Remove(data.Name);
        }

        response.CreatePlayerObject = true;
        Debug.LogError("2. Completed Connection approval for " + data.Name);
    }

    public async void StartClient()
    {
        await AddPayloadData();
        NetworkManager.Singleton.StartClient();
    }

    public void RegisterPlayerAndRemoveDuplicates(NetworkPlayer networkPlayer)
    {
        if (_playerConnections.TryGetValue(networkPlayer.PlayerName.Value.Value, out var existingPlayer))
        {
            existingPlayer.GetComponent<NetworkObject>().Despawn();
            _playerConnections.Remove(networkPlayer.PlayerName.Value.Value);
            Debug.LogError("3.5 Removed Duplicate Player for " + networkPlayer.PlayerName.Value.Value + " on Server");
        }
        _playerConnections.Add(networkPlayer.PlayerName.Value.Value, networkPlayer);
    }
}