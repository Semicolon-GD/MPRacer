using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Wheels : MonoBehaviour
{
    [SerializeField] List<Wheel> _frontWheels;
    [SerializeField] List<Wheel> _backWheels;
    [FormerlySerializedAs("_player")] [SerializeField] CarClientMovementController _clientMovementController;
    [SerializeField] float _wheelTurn = 5f;
    [SerializeField] LayerMask _groundLayer;
    [SerializeField] float _wheelDistance = 1f;
    public bool HasTraction { get; private set; }
    public bool CanSteer { get; private set; }

    void OnValidate()
    {
        _clientMovementController = GetComponent<CarClientMovementController>();
        _groundLayer = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        float turn = 0f;// _player.Turn.Value * _wheelTurn;
        float spinPct = 0f;// _player.Throttle.Value;

        foreach (var wheel in _frontWheels)
        {
            wheel.transform.localRotation = Quaternion.Euler(wheel.transform.localRotation.x + spinPct, turn, 0f);
        }

        foreach (var wheel in _backWheels)
        {
            wheel.transform.localRotation = Quaternion.Euler(wheel.transform.localRotation.x + spinPct, 0, 0f);
        }
    }

    void FixedUpdate()
    {
        CanSteer = false;
        foreach (var wheel in _frontWheels)
            if (Physics.Raycast(wheel.transform.position, -transform.up, _wheelDistance, _groundLayer))
                CanSteer = true;
        HasTraction = false;
        foreach (var wheel in _backWheels)
            if (Physics.Raycast(wheel.transform.position, -transform.up, _wheelDistance, _groundLayer))
                HasTraction = true;
    }
}