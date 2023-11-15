using UnityEngine;

public class CarParticles : MonoBehaviour
{
    [SerializeField] ParticleSystem _frozen;
    
    public void PlayFrozen()
    {
        _frozen.Play();
    }
}