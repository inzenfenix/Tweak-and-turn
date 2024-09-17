using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectAnimationEvent : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    private void ParticleEffect()
    {
        particles.Play();
    }
}
