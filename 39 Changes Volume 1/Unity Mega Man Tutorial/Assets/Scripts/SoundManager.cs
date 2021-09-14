using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script from this source
 * https://www.daggerhart.com/unity-audio-and-sound-manager-singleton-script/
 * 
 * Note: I expanded upon the script by adding the Pause and Stop functions
 */

public class SoundManager : MonoBehaviour
{
    // Audio players components.
    public AudioSource EffectsSource;
    public AudioSource MusicSource;

    // Random pitch adjustment range.
    public float LowPitchRange = .95f;
    public float HighPitchRange = 1.05f;

    // Singleton instance.
    public static SoundManager Instance = null;

    // Initialize the singleton instance.
    private void Awake()
    {
        // If there is not already an instance of SoundManager, set it to this.
        if (Instance == null)
        {
            Instance = this;
        }
        //If an instance already exists, destroy whatever this object is to enforce the singleton.
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        //Set SoundManager to DontDestroyOnLoad so that it won't be destroyed when reloading our scene.
        DontDestroyOnLoad(gameObject);
    }

    // Play a single clip through the sound effects source.
    public void Play(AudioClip clip, bool loop = false)
    {
        EffectsSource.time = 0;
        EffectsSource.loop = loop;
        EffectsSource.clip = clip;
        EffectsSource.Play();
    }

    // Play a single clip through the music source.
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        MusicSource.time = 0;
        MusicSource.loop = loop;
        MusicSource.clip = clip;
        MusicSource.Play();
    }

    // Pause single clip through the sound effect source.
    public void Pause()
    {
        EffectsSource.Pause();
    }

    // Pause single clip through the music source.
    public void PauseMusic()
    {
        MusicSource.Pause();
    }

    // Stop single clip through the sound effect source.
    public void Stop()
    {
        EffectsSource.Stop();
    }

    // Stop single clip through the music source.
    public void StopMusic()
    {
        MusicSource.Stop();
    }

    // Play a random clip from an array, and randomize the pitch slightly.
    public void RandomSoundEffect(params AudioClip[] clips)
    {
        int randomIndex = Random.Range(0, clips.Length);
        float randomPitch = Random.Range(LowPitchRange, HighPitchRange);

        EffectsSource.pitch = randomPitch;
        EffectsSource.clip = clips[randomIndex];
        EffectsSource.Play();
    }
}