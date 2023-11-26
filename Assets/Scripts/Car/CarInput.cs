using System;
using Unity.Netcode;
using UnityEngine;

public class CarInput : NetworkBehaviour
{
    public NetworkVariable<float> Throttle = new(0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    
    public NetworkVariable<sbyte> Turn = new(0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public float ThrottlePct
    {
        get => Throttle.Value / 127f;
        set => Throttle.Value = Convert.ToSByte(Mathf.Clamp01(value) * 127f);
    }

    public float TurnPct
    {
        get => Turn.Value / 127f;
        private set => Turn.Value = Convert.ToSByte(Mathf.Clamp01(value) * 127f);
    }

    void Update()
    {
        if (IsOwner)
            ProcessInput();
    }

    void ProcessInput()
    {
        #if UNITY_EDITOR
        if (TryGetComponent<FollowPath>(out var path) && path.enabled)
        {
            ThrottlePct = 1f;
            TurnPct = path.Turn;
            return;
        }
        #endif
        
        Throttle.Value = Convert.ToSByte((float) (127 * Input.GetAxis("Vertical")));
        Turn.Value = Convert.ToSByte((float) (127 * Input.GetAxis("Horizontal")));
    }
}