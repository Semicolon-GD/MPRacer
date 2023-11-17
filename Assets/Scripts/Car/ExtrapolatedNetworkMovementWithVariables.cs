using Unity.Netcode;
using UnityEngine;

public class ExtrapolatedNetworkMovementWithVariables : NetworkBehaviour
{
    Rigidbody _rb;
    NetworkVariable<Vector3> netPosition;
    NetworkVariable<Vector3> netVelocity;
    NetworkVariable<Vector3> netOrientation;

    double netServerTime;
    double snapThreshold = 5f;
    bool _extrapolate;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        netPosition = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
        netVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
        netOrientation = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            ShortcutManager.Add("Toggle Extrapolation", () => ToggleExtrapolation());
    }

    void ToggleExtrapolation()
    {
        _extrapolate = !_extrapolate;
    }

    void OnEnable() => NetworkManager.Singleton.NetworkTickSystem.Tick += HandleNetworkTick;
    void OnDisable() => NetworkManager.Singleton.NetworkTickSystem.Tick -= HandleNetworkTick;

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkTickSystem != null)
            NetworkManager.Singleton.NetworkTickSystem.Tick -= HandleNetworkTick;
        base.OnDestroy();
    }

    void HandleNetworkTick()
    {
        if (!enabled)
            return;

        if (IsOwner)
        {
            netPosition.Value = _rb.position;
            netVelocity.Value = _rb.velocity;
            netOrientation.Value = _rb.transform.forward;
        }

        if (!IsOwner)
            ExtrapolateMovementFromPreviousData();
    }

    void ExtrapolateMovementFromPreviousData()
    {
        var estimatedPosition = netPosition.Value;
        if (_extrapolate)
        {
            double timeClientIsBehind =
                NetworkManager.Singleton.ServerTime.Time - NetworkManager.Singleton.LocalTime.Time;
            var estimatedMovement = netVelocity.Value * (float) timeClientIsBehind;
            estimatedPosition += estimatedMovement;
            Debug.LogError($"Extrapolation moved {estimatedMovement} to make up for {timeClientIsBehind}s delay");
        }

        if (Vector3.Distance(_rb.position, estimatedPosition) < snapThreshold)
            _rb.position = Vector3.Lerp(_rb.position, estimatedPosition, Time.deltaTime * 10);
        else
            _rb.position = estimatedPosition;

        _rb.transform.forward = Vector3.Lerp(_rb.transform.forward, netOrientation.Value, Time.deltaTime * 10f);
    }
}