using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Car : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> OwnerName;
    public NetworkVariable<FixedString32Bytes> CarName;
    public bool IsLocalPlayers { get; private set; }
    public CarCustomData OverrideCustomData { get; private set; }

    void Awake()
    {
        OwnerName = new NetworkVariable<FixedString32Bytes>();
        OwnerName.OnValueChanged += OwnerNameChanged;
        CarName = new NetworkVariable<FixedString32Bytes>();
        CarName.OnValueChanged += CarNameChanged;
    }

    void CarNameChanged(FixedString32Bytes previousvalue, FixedString32Bytes newvalue) => UpdateCustomData(newvalue);

    void UpdateCustomData(FixedString32Bytes newvalue)
    {
        OverrideCustomData = CarUnlockManager.Instance.GetCarCustomData(newvalue.Value);
        Debug.LogError("Update Custom Data " + newvalue);
    }

    void OwnerNameChanged(FixedString32Bytes previousvalue, FixedString32Bytes newvalue) => HandleNameOrOwnerChanged();

    protected override void OnOwnershipChanged(ulong previous, ulong current)  => HandleNameOrOwnerChanged();

    public override void OnNetworkSpawn() => HandleNameOrOwnerChanged();

    void HandleNameOrOwnerChanged()
    {
        IsLocalPlayers = OwnerName.Value == AuthenticationManager.Instance.GetPlayerName();
        gameObject.name = "Car (" + OwnerName.Value + ")";
        if (IsLocalPlayers)
            gameObject.name += " Local";
    }
}