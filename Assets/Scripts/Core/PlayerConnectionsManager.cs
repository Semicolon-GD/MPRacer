using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class PlayerConnectionsManager : NetworkBehaviour
{
    public static PlayerConnectionsManager Instance { get; private set; }
    void Awake() => Instance = this;

    public static Dictionary<string, string> PlayerConnectionsToNames = new();

    public string GetName(string connectionId)
    {
        if (PlayerConnectionsToNames.TryGetValue(connectionId, out var name))
            return name;

        return "Unknown";
    }

    void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = HandleConnectionApproval;
    }

    public async Awaitable StartHostOnServer()
    {
        await AddPayloadData();
        NetworkManager.Singleton.StartHost();
    }

    static async Task AddPayloadData()
    {
        RaceGameConnectionData data = new()
        {
            Name = await AuthenticationManager.Instance.GetPlayerNameAsync(),
            Id = await AuthenticationManager.Instance.GetPlayerIdAsync(),
            Car = "Red"
        };
        var json = JsonUtility.ToJson(data);
        Debug.Log(json);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(json);
    }

    void HandleConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        var payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        Debug.Log(payload);
        var data = JsonUtility.FromJson<RaceGameConnectionData>(payload);
        Debug.Log($"Connection Approval {payload}");
        PlayerConnectionsToNames[request.ClientNetworkId.ToString()] = data.Name;
        response.Approved = true;
        response.CreatePlayerObject = true;
    }

    public async void StartClient()
    {
        await AddPayloadData();
        NetworkManager.Singleton.StartClient();
    }
}