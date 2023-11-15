using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;

public class FollowLocalCar : MonoBehaviour
{
    private CinemachineVirtualCamera _vcam;

    // Start is called before the first frame update
    void Start()
    {
         _vcam = GetComponent<CinemachineVirtualCamera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_vcam.Follow == null)
        {
            var car = FindObjectsByType<CarClientMovementController>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.IsOwner);
            if (car != default)
            {
                _vcam.Follow = car.transform;
                _vcam.LookAt = car.transform;
            }
        }
    }
}
