using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CarPowerups : NetworkBehaviour
{
    List<PowerupInstance> _powerups = new();
    [SerializeField] GameObject _speedVisual;

    void Update()
    {
        if (IsServer)
            RemoveExpiredPowerups();
        if (IsOwner)
            HandleInputs();
    }

    void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            UseFreezeServerRpc();
    }

    public void AddPowerup(PowerupData powerupData)
    {
        _powerups.Add(new PowerupInstance(powerupData));
        AddPowerupVisualClientRpc(powerupData.PowerupType);
    }

    [ClientRpc]
    void AddPowerupVisualClientRpc(PowerupType powerupType)
    {
        _speedVisual.SetActive(true);
    }

    [ClientRpc]
    void RemovePowerupVisualClientRpc(PowerupType powerupType)
    {
        _speedVisual.SetActive(false);
    }


    void RemoveExpiredPowerups()
    {
        for (var i = 0; i < _powerups.Count; i++)
        {
            var powerup = _powerups[i];
            if (powerup.EndTime <= Time.time)
            {
                _powerups.Remove(powerup);
                RemovePowerupVisualClientRpc(powerup.PowerupData.PowerupType);
            }
        }
    }

    public float GetMaxSpeed(float maxSpeed)
    {
        foreach (var powerup in _powerups)
        {
            switch (powerup.PowerupData.PowerupType)
            {
                case PowerupType.SpeedBoost:
                    maxSpeed *= powerup.PowerupData.Multiplier;
                    break;
            }
        }

        return maxSpeed;
    }

    public float GetTurnAmount(float turnAmount)
    {
        foreach (var powerup in _powerups)
        {
            switch (powerup.PowerupData.PowerupType)
            {
                case PowerupType.TurningBoost:
                    turnAmount *= powerup.PowerupData.Multiplier;
                    break;
            }
        }

        return turnAmount;
    }

    public bool HasFreeze()
    {
        return _powerups.Any(t => t.PowerupData.PowerupType == PowerupType.FreezeEnemies);
    }

    [ServerRpc]
    public void UseFreezeServerRpc()
    {
        if (!HasFreeze())
            return;
        _powerups.RemoveAll(t => t.PowerupData.PowerupType == PowerupType.FreezeEnemies);
        foreach (var car in FindObjectsByType<CarPowerups>(FindObjectsSortMode.None))
        {
            //if (car != this)
                car.GetComponent<CarClientMovementController>().FreezeClientRpc();
        }
    }

    public void TryPickupPowerup(PowerupPickup powerupPickup)
    {
        TryPickupPowerupOnServerRpc(powerupPickup.NetworkObjectId, transform.position);
    }

    [ServerRpc]
    void TryPickupPowerupOnServerRpc(ulong powerupPickupNetworkObjectId, Vector3 clientCarPosition)
    {
        ClientCarPosition = clientCarPosition;

        var powerup = FindObjectsByType<PowerupPickup>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.NetworkObjectId == powerupPickupNetworkObjectId);

        if (powerup != null && powerup.WasPickedUp == false) // server side distance checks are done by the powerup
            powerup.TryPickupOnServer(this);
    }

    public Vector3 ClientCarPosition { get; private set; }
}