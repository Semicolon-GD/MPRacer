using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CarPowerups), typeof(Wheel), typeof(CarInput))]
[RequireComponent(typeof(CarLapCounter), typeof(CarSpawn), typeof(Car))]
public class CarClientMovementController : NetworkBehaviour
{
    float _velocityMultiplier = 100f;

    [Header("Car Settings - Per Car")]
    [SerializeField] float _maxSpeed = 5f;
    [SerializeField] float _turnSpeed = 1f;

    [Header("Physics Settings")]
    [SerializeField] ForceMode _forceMode;


    public int MaxSpeed => Convert.ToInt32(_maxSpeed);
    public int TurnSpeed => Convert.ToInt32(_turnSpeed / 10f);

    Car _car;
    CarInput _input;
    Wheels _wheels;
    CarPowerups _powerups;
    TerrainDetector _terrainDetector;
    CarParticles _particles;
    Rigidbody _rigidbody;

    bool _frozen;

    void Awake()
    {
        _car = GetComponent<Car>();
        _input = GetComponent<CarInput>();
        _wheels = GetComponent<Wheels>();
        _powerups = GetComponent<CarPowerups>();
        _terrainDetector = GetComponent<TerrainDetector>();
        _particles = GetComponent<CarParticles>();
        _rigidbody = GetComponent<Rigidbody>();

    }


    void Update()
    {
        if (_car.IsLocalPlayers)
            UpdateMovement();
    }

    void UpdateMovement()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState.Value != GameState.Racing)
            return;

        if (_wheels.CanSteer)
            ApplyRotation();

        if (_wheels.HasTraction)
            ApplyMovement();
    }

    void ApplyRotation()
    {
        float turnAmount = _input.TurnPct * Time.deltaTime * _turnSpeed * _input.ThrottlePct;
        turnAmount = _powerups.GetTurnAmount(turnAmount);
        transform.Rotate(0, turnAmount, 0);
    }

    void ApplyMovement()
    {
        float moveAmount = _input.Throttle.Value * _velocityMultiplier;
        var flatRotation = new Vector3(transform.forward.x, 0f, transform.forward.z);

        float maxSpeed = _powerups.GetMaxSpeed(_maxSpeed);
        if (_terrainDetector.IsOnMud)
        {
            // Slowing for mud
            maxSpeed *= 0.5f;
        }

        var velocity = flatRotation * moveAmount;
        velocity.y = Physics.gravity.y;
        _rigidbody.AddForce(velocity, _forceMode);

        _rigidbody.maxLinearVelocity = maxSpeed;
        if (_frozen)
            _rigidbody.velocity = Vector3.zero;
    }

    [ClientRpc]
    public void FreezeClientRpc()
    {
        _frozen = true;
        Invoke(nameof(RemoveFreeze), 0.25f);
        _particles.PlayFrozen();
    }

    void RemoveFreeze() => _frozen = false;
}