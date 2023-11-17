using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ExtrapolatedNetworkMovement : NetworkBehaviour
{
    public Vector3 netPosition;
    public Vector3 netVelocity;
    public Vector3 netOrientation;
    public double netServerTime;

    Vector3 estimatedPosition;
    bool _dirty;

    [SerializeField] double _snapThreshold = 5f;
    [SerializeField] double _tolerance = 0.01f;

    public bool shouldSendFromClient;
    public bool shouldSendFromServer;

    Rigidbody _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();

    void Update()
    {
        if (!IsOwner && netServerTime != 0)
        {
            if (Vector3.Distance(_rb.position, estimatedPosition) > _snapThreshold)
                _rb.position = estimatedPosition;
            else
                _rb.position = Vector3.Lerp(_rb.position, estimatedPosition, 0.5f);

            _rb.transform.forward = Vector3.Lerp(_rb.transform.forward, netOrientation, 0.5f);
        }
    }

    void OnEnable() => NetworkManager.Singleton.NetworkTickSystem.Tick += HandleNetworkTick;
    void OnDisable() => NetworkManager.Singleton.NetworkTickSystem.Tick -= HandleNetworkTick;

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkTickSystem != null)
            NetworkManager.Singleton.NetworkTickSystem.Tick -= HandleNetworkTick;
        base.OnDestroy();
    }

    public void InitializeForSync()
    {
        // Write the data to be synchronized before Spawn causes OnSynchronize to be called
        UpdateLocalData(transform.position, _rb.velocity, transform.forward, 0);
    }

    protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
    {
        // This is written on the server and read on clients when they spawn
        serializer.SerializeValue(ref netPosition);
        serializer.SerializeValue(ref netVelocity);
        serializer.SerializeValue(ref netOrientation);
        serializer.SerializeValue(ref netServerTime);
        base.OnSynchronize(ref serializer);
    }

    void HandleNetworkTick()
    {
        if (!enabled)
            return;

        if (IsServer && _dirty)
        {
            SendMovementToClientRpc(netPosition, netVelocity, netOrientation, netServerTime);
            _dirty = false;
            return;
        }

        if (!IsOwner)
        {
            ExtrapolateMovementFromPreviousData();
            return;
        }

        if (IsClient)
        {
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
    }

    void ExtrapolateMovementFromPreviousData()
    {
        if (netServerTime == 0)
        {
            _rb.position = netPosition;
            _rb.transform.forward = netOrientation;
            return;
        }

        double elapsed = NetworkManager.Singleton.ServerTime.Time - netServerTime;
        estimatedPosition = netPosition + netVelocity * (float) elapsed;
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
               Vector3.Distance(netOrientation, orientation) > _tolerance ||
               serverTime - netServerTime > 0.25;
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