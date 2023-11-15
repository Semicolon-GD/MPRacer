using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] List<GameObject> _carPrefabs;

    GameObject _car;

    NetworkVariable<FixedString32Bytes> _playerId = new(readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<FixedString32Bytes> PlayerName = new();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (IsOwner)
            _playerId.Value = AuthenticationManager.Instance.GetPlayerIdCached();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            var playerName = PlayerConnectionsManager.Instance.GetName(this.OwnerClientId.ToString());
            PlayerName.Value = playerName;
        }
    }

    public void SpawnCar()
    {
        Debug.Log($"Called SpawnCar on {NetworkObject.OwnerClientId}");
        if (_car != null)
        {
            Debug.LogError("Attempted to spawn car when one already exists");
            return;
        }

        Debug.Log($"Spawning Car for {_playerId.Value.Value}");
        string carName = "Yellow";
        if (LobbyManager.Instance?.CurrentLobby != null)
        {
            var playerData =
                LobbyManager.Instance.CurrentLobby.Players.FirstOrDefault(t => t.Id == _playerId.Value.Value);
            if (playerData == null)
            {
                Debug.LogError($"Unable to find playerid {_playerId.Value.Value}");
            }
            else if (playerData.Data.TryGetValue("car", out var carNameObject))
                carName = carNameObject.Value; // "Blue";
        }

        Debug.Log($"Spawning {carName}");
        var sp = GetSpawnPoint();
        var carPrefab = _carPrefabs.FirstOrDefault(t => t.name == carName);
        _car = Instantiate(carPrefab, sp.position, sp.rotation);
        SceneManager.MoveGameObjectToScene(_car.gameObject, ProjectSceneManager.Instance.CurrentTrackScene);
        _car.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId, true);

        ResetCarPosition();
    }

    Transform GetSpawnPoint()
    {
        var spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Any())
        {
            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return spawnPoint.transform;
        }

        return default;
    }

    void ResetCarPosition()
    {
        UIConsoleManager.AddLog("Track loaded, spawning car");

        Debug.Log("ResetCarPosition");
        var spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Any())
        {
            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;
        }
    }
}