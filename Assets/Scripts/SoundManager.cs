using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioClip[] swordSwingsSounds;
    [SerializeField] AudioClip[] hitSounds;
    [SerializeField] AudioClip[] clashSounds;

    [SerializeField] AudioSource audioSource;

    public void PlayAttackSound()
    {
        var sound = swordSwingsSounds[Random.Range(0, swordSwingsSounds.Length)];
        audioSource.clip = sound;
        audioSource.Play();
    }

    public void PlayHitSound()
    {
        var sound = hitSounds[Random.Range(0, hitSounds.Length)];
        audioSource.clip = sound;
        audioSource.Play();
    }

    public void PlayClashSound()
    {
        var sound = clashSounds[Random.Range(0, clashSounds.Length)];
        audioSource.clip = sound;
        audioSource.Play();
    }
}
