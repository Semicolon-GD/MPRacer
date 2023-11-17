using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Car : NetworkBehaviour
{
    public bool IsLocalPlayers { get; private set; }
    public NetworkVariable<FixedString32Bytes> OwnerName;


    void Awake()
    {
        OwnerName = new NetworkVariable<FixedString32Bytes>();
        OwnerName.OnValueChanged += OwnerNameChanged;
    }

    void OwnerNameChanged(FixedString32Bytes previousvalue, FixedString32Bytes newvalue) => HandleNameOrOwnerChanged();

    protected override void OnOwnershipChanged(ulong previous, ulong current)  => HandleNameOrOwnerChanged();

    public override void OnNetworkSpawn() => HandleNameOrOwnerChanged();

    void HandleNameOrOwnerChanged()
    {
        IsLocalPlayers = OwnerName.Value == AuthenticationManager.LocalPlayerName;
        gameObject.name = "Car (" + OwnerName.Value + ")";
        if (IsLocalPlayers)
            gameObject.name += " Local";
    }
}