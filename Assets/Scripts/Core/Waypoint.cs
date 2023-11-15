using Unity.Netcode;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [SerializeField] Renderer _renderer;

    void OnValidate() => _renderer = GetComponent<Renderer>();

    void OnEnable() => ResetColor();

    public void ResetColor() => _renderer.material.color = Color.white;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entered Waypoint {NetworkManager.Singleton.IsServer}");

        if (!other.TryGetComponent<CarLapCounter>(out var car))
            car = other.GetComponentInParent<CarLapCounter>();

        if (car == null)
            return;

        if (NetworkManager.Singleton.IsServer)
            car.PassWaypoint(this);

        if (car.IsOwner)
            _renderer.material.color = Color.green;
    }
}