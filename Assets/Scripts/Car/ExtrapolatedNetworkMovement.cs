using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ExtrapolatedNetworkMovement : NetworkBehaviour
{
    Rigidbody _rb;
    Vector3 netPosition;
    Vector3 netVelocity;
    Vector3 netOrientation;
    double netServerTime;
    double snapThreshold = 1f;
    [SerializeField] double _tolerance = 0.01f;
    [SerializeField] bool shouldSendFromClient;
    [SerializeField] bool shouldSendFromServer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        NetworkManager.Singleton.NetworkTickSystem.Tick += HandleNetworkTick;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton)
            NetworkManager.Singleton.NetworkTickSystem.Tick -= HandleNetworkTick;
    }

    void HandleNetworkTick()
    {
        if (!enabled)
            return;

        if (!IsOwner)
        {
            ExtrapolateMovementFromPreviousData();
            return;
        }

        shouldSendFromServer = false; // Debug variables to show when messages will send and wont send
        shouldSendFromClient = ShouldSend(_rb.position, _rb.velocity, _rb.transform.forward,
            NetworkManager.Singleton.ServerTime.Time);

        if (shouldSendFromClient)
        {
            SendMovementToServerRpc(_rb.position,
                _rb.velocity,
                _rb.transform.forward,
                NetworkManager.Singleton.ServerTime.Time);
        }
    }

    void ExtrapolateMovementFromPreviousData()
    {
        double elapsed = NetworkManager.Singleton.ServerTime.Time - netServerTime;
        var estimatedPosition = netPosition + netVelocity * (float)elapsed;

        if (Vector3.Distance(_rb.position, estimatedPosition) > snapThreshold)
            _rb.position = Vector3.Lerp(_rb.position, estimatedPosition, Time.deltaTime * 10);

        _rb.transform.forward = Vector3.Lerp(_rb.transform.forward, netOrientation, Time.deltaTime * 10f);
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    void SendMovementToServerRpc(Vector3 position, Vector3 velocity, Vector3 orientation, double serverTime)
    {
        shouldSendFromServer = ShouldSend(position, velocity, orientation, serverTime);
        if (shouldSendFromServer)
        {
            UpdateLocalData(position, velocity, orientation, serverTime);
            SendMovementToClientRpc(position, velocity, orientation, serverTime);
        }
    }

    bool ShouldSend(Vector3 position, Vector3 velocity, Vector3 orientation, double serverTime)
    {
        return Vector3.Distance(netPosition, position) > _tolerance ||
               Vector3.Distance(netVelocity, velocity) > _tolerance ||
               Vector3.Distance(netOrientation, orientation) > _tolerance;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    void SendMovementToClientRpc(Vector3 position, Vector3 velocity, Vector3 orientation, double serverTime)
    {
        UpdateLocalData(position, velocity, orientation, serverTime);
    }

    void UpdateLocalData(Vector3 position, Vector3 velocity, Vector3 orientation, double serverTime)
    {
        netPosition = position;
        netVelocity = velocity;
        netOrientation = orientation;
        netServerTime = serverTime;
    }
}