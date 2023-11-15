using UnityEngine;

public struct PowerupInstance
{
    public PowerupInstance(PowerupData powerupData)
    {
        this.PowerupData = powerupData;
        EndTime = Time.time + powerupData.Duration;
    }

    public float EndTime { get; private set; }

    public PowerupData PowerupData { get; private set; }
}