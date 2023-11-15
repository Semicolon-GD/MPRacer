using UnityEngine;

public class PreventTipping : MonoBehaviour
{
    public Vector3 uprightDirection = Vector3.up;
    public float antiTippingForce = 10f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Calculate the torque needed to align the Rigidbody's up direction with the upright direction
        Vector3 torque = Vector3.Cross(transform.up, uprightDirection) * antiTippingForce;

        // Apply the torque to the Rigidbody
        rb.AddTorque(torque);
    }
}