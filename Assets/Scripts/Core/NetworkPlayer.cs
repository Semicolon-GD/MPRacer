using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayer : NetworkBehaviour
{
    static int _nextSpawnPoint;
    [SerializeField] List<GameObject> _carPrefabs;

    public NetworkVariable<FixedString32Bytes> PlayerId;
    public NetworkVariable<FixedString32Bytes> PlayerName;

    Car _car;

    void Awake()
    {
        PlayerId = new(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
        PlayerName = new();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        Debug.LogError("Player OnNetworkDespawn for " + PlayerName.Value);
        if (IsServer)
            _car.GetComponent<NetworkObject>().ChangeOwnership(0);
        base.OnNetworkDespawn();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            PlayerId.Value = AuthenticationManager.Instance.GetPlayerId();

        if (IsServer)
        {
            var playerName = PlayerConnectionsManager.Instance.GetName(OwnerClientId);
            PlayerName.Value = playerName;

            gameObject.name = "NetworkPlayer (" + playerName + ")";
            PlayerConnectionsManager.Instance.RegisterPlayerAndRemoveDuplicates(this);
            Debug.LogError("3. Player Spawn Completed for " + playerName + " on " + (IsServer ? "Server" : "Client"));

        }

    }

    public void SpawnCarOnServer()
    {
        if (_car != null)
        {
            Debug.LogError($"Attempted to spawn car for client {NetworkObject.OwnerClientId} when one already exists");
            return;
        }

        Debug.LogError($"4.5 Spawning Car for {PlayerId.Value.Value} on client {NetworkObject.OwnerClientId}");

        string carItemId = PlayerConnectionsManager.Instance.GetCar(OwnerClientId);
        Debug.Log($"Spawning {carItemId}");

        var sp = GetNextSpawnPoint();
        var carPrefab = _carPrefabs.FirstOrDefault(t => 
            carItemId.StartsWith(t.name, StringComparison.InvariantCultureIgnoreCase));
        
        if (carPrefab == null)
        {
            var customData = CarUnlockManager.Instance.GetCarCustomData(carItemId);
            if (customData != default)
                carPrefab = _carPrefabs.FirstOrDefault(t=> 
                    customData.cardefinition.StartsWith(t.name, StringComparison.InvariantCultureIgnoreCase));

            if (carPrefab == null)
            {
                Debug.LogError("No Car prefab found, defaulting to first");
                carPrefab = _carPrefabs.FirstOrDefault();
            }
        }
        _car = Instantiate(carPrefab, sp.position, sp.rotation).GetComponent<Car>();
        
        SceneManager.MoveGameObjectToScene(_car.gameObject, ProjectSceneManager.Instance.CurrentTrackScene);
        _car.GetComponent<ExtrapolatedNetworkMovement>().InitializeForSync();
        _car.NetworkObject.SpawnWithOwnership(OwnerClientId, true);
        _car.OwnerName.Value = PlayerName.Value;
        _car.CarName.Value = carItemId;
    }

    public ClientRpcParams OwnerOnlyRpcParms()
    {
        return new ClientRpcParams()
        {
            Send = new() { TargetClientIds = new[] { OwnerClientId } }
        };
    }

    Transform GetNextSpawnPoint()
    {
        var spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (_nextSpawnPoint > spawnPoints.Length)
            _nextSpawnPoint = 0;

        _nextSpawnPoint++;
        return spawnPoints[_nextSpawnPoint].transform;
    }

    #region Helpers (could be moved)

    string TryGetCarNameFromLobby(string carName)
    {
        if (LobbyManager.Instance?.CurrentLobby != null)
        {
            var playerData =
                LobbyManager.Instance.CurrentLobby.Players.FirstOrDefault(t => t.Id == PlayerId.Value.Value);
            if (playerData == null)
            {
                Debug.LogError($"Unable to find playerid {PlayerId.Value.Value}");
            }
            else if (playerData.Data.TryGetValue("car", out var carNameObject))
                carName = carNameObject.Value; // "Blue";
        }


        return carName;
    }

    #endregion
}