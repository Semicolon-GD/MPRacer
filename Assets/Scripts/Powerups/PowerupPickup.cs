using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PowerupPickup : NetworkBehaviour
{
    [SerializeField] PowerupData[] _powerupDatas;
    [SerializeField] double _maxPickupDistance = 5f;

    public event Action OnPickedUp;

    PowerupData _powerupData;
    public bool WasPickedUp { get; private set; }

    void Start()
    {
        _powerupData = _powerupDatas[Random.Range(0, _powerupDatas.Length)];
        if (_powerupData.ParticleGameObject != null)
        Instantiate(_powerupData.ParticleGameObject, transform);
    }

    void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponent<CarPowerups>();
        if (car == null)
            car = other.GetComponentInParent<CarPowerups>();
        if (car == null)
            return;

        if (car.IsOwner == false)
            return; // only pickup on owner

        car.TryPickupPowerup(this);
    }

    public void TryPickupOnServer(CarPowerups car)
    {
        if (car == null)
        {
            Debug.LogError("Car not found on serve");
            return;
        }

        var distanceToPowerup = Vector3.Distance(car.transform.position, transform.position);
        if (distanceToPowerup > _maxPickupDistance)
        {
            Debug.LogError($"Car tried to pickup powerup from too far away {distanceToPowerup} > {_maxPickupDistance} (max)");
            return;
        }

        WasPickedUp = true;

        OnPickedUp?.Invoke();
        car.AddPowerup(_powerupData);
        NetworkObject.Despawn();
    }
}