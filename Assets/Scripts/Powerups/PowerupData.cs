using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = nameof(PowerupData))]
public class PowerupData : ScriptableObject
{
    public float Multiplier = 1f;
    public PowerupType PowerupType;
    public float Duration = 3f;
    public GameObject ParticleGameObject;
}