using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AudioController : MonoBehaviour
{
    [Header("SFX")]
    public Sound[] sounds;

    public static AudioController instance;

    [Header("Music")]
    [Range(0f, 5f)]
    public float volume;
    [Range(0f, 5f)]
    public float pitch;
    public AudioClip[] songs;

    private int songIndex = 0;
    private AudioSource songSource;
    public TMP_Text song;
    private bool musicMuted = false;


    public void ShuffleArray(AudioClip[] songs) //shuffles the palette array
    {
        for (int i = 0; i < songs.Length; i++)
        {
            Swap(songs, i, Random.Range(i, songs.Length));
        }
    }

    public void Swap(AudioClip[] arr, int a, int b) //swaps items in the ColorPalette array
    {
        AudioClip temp = arr[a];
        arr[a] = arr[b];
        arr[b] = temp;
    }

    private void Update()
    {
        if (!songSource.isPlaying) //when song finishes
        {
            songIndex++;
            if (songIndex > songs.Length - 1)
            {
                songIndex = 0;                
            }          
            songSource.clip = songs[songIndex];
            songSource.Play();

            song.text = songSource.clip.name;
        }
    }

    public void MuteMusic() {
        if (musicMuted) {
            musicMuted = false;
            songSource.volume = volume;
        }
        else {
            musicMuted = true;
            songSource.volume = 0f;
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ShuffleArray(songs);
        songSource = gameObject.AddComponent<AudioSource>();
        songSource.clip = songs[songIndex];
        songSource.volume = volume;
        songSource.pitch = pitch;
        songSource.Play();
        song.text = songSource.clip.name;

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.Loop;
        }
    }

    public void Play(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                sound.Play();
            }
        }
    }

    public void Stop(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                sound.Stop();
            }
        }
    }
}
