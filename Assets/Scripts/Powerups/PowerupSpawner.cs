using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PowerupSpawner : NetworkBehaviour
{
    PowerupPickup _currentPowerup;
    [SerializeField] PowerupPickup _powerupPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            SpawnPowerup();
    }

    void SpawnPowerup()
    {
        _currentPowerup = Instantiate(_powerupPrefab, transform.position, transform.rotation);
        SceneManager.MoveGameObjectToScene(_currentPowerup.gameObject, ProjectSceneManager.Instance.CurrentTrackScene);
        _currentPowerup.NetworkObject.Spawn();
        _currentPowerup.OnPickedUp += StartRespawnTimer;
    }

    void StartRespawnTimer()
    {
        _currentPowerup.OnPickedUp -= StartRespawnTimer;
        Invoke(nameof(SpawnPowerup), 30f);
    }
}