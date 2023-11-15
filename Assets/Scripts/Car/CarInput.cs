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

    public float ThrottlePct => Throttle.Value / 128f;
    public float TurnPct => Turn.Value / 128f;

    void Update()
    {
        if (IsOwner)
            ProcessInput();
    }

    void ProcessInput()
    {
        Throttle.Value = Convert.ToSByte((float) (127 * Input.GetAxis("Vertical")));
        Turn.Value = Convert.ToSByte((float) (127 * Input.GetAxis("Horizontal")));
    }
  
}