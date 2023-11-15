using Unity.Netcode;
using UnityEngine;

public class CarSpawn : NetworkBehaviour
{
    // static int nextPlayerIndex;
    // int _playerIndex;
    //
    // public override void OnNetworkSpawn()
    // {
    //     _playerIndex = nextPlayerIndex;
    //     nextPlayerIndex++;
    //     base.OnNetworkSpawn();
    //
    //     var starts = GameObject.FindGameObjectsWithTag("Respawn");
    //     var picked = starts[_playerIndex];
    //     transform.position = picked.transform.position;
    //     transform.rotation = picked.transform.rotation;
    // }
}