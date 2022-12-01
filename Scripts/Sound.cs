using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip[] variations;


    [HideInInspector]
    public AudioSource source;

    [Range(0f, 5f)]
    public float volume;
    [Range(0f, 5f)]
    public float pitch;
    public bool Loop;

    public void Play() {
        source.clip = variations[Random.Range(0, variations.Length)];
        source.Play();
    }

    public void Stop() {
        source.Stop();
    }
}
