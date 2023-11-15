using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CarLapCounter : NetworkBehaviour
{
    public NetworkVariable<byte> LapsComplete = new();

    List<Waypoint> _allWaypoints;
    HashSet<Waypoint> _waypointsRemaining = new();
    NetworkPlayer _player;
    public string PlayerName => _player.PlayerName.Value.ToString(); // { get; }

    public override void OnNetworkSpawn() => BindToPlayer(OwnerClientId);

    protected override void OnOwnershipChanged(ulong previous, ulong current) => BindToPlayer(current);

    void BindToPlayer(ulong ownerClientId) =>
        _player = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.OwnerClientId == ownerClientId);

    void Start()
    {
        if (IsServer)
        {
            _allWaypoints = FindObjectsByType<Waypoint>(FindObjectsSortMode.None).ToList();
            ResetWaypoints();
        }

        if (IsOwner)
            ShortcutManager.Add("Finish Lap", CheatFinishLap);
    }

    [ContextMenu(nameof(CheatFinishLap))]
    public void CheatFinishLap() => CheatFinishLapServerRpc();

    [ServerRpc]
    void CheatFinishLapServerRpc() => FinishLap();

    void FinishLap()
    {
        LapsComplete.Value++;
        GameManager.Instance.FinishLap(LapsComplete.Value, this);
        Debug.Log("Lap Done");
    }

    public void PassWaypoint(Waypoint waypoint)
    {
        if (_allWaypoints == null)
            return; // not initialized yet
        if (waypoint.CompareTag("Finish") && _waypointsRemaining.Count > 1)
            return;

        _waypointsRemaining.Remove(waypoint);
        if (_waypointsRemaining.Count == 0)
        {
            FinishLap();
            ResetWaypoints();
        }
    }

    void ResetWaypoints()
    {
        _waypointsRemaining = new HashSet<Waypoint>(_allWaypoints);
        foreach (var waypoint in _allWaypoints)
            waypoint.ResetColor();
    }
}