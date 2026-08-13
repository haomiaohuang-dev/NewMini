using System;
using UnityEngine;

public enum GameAudioChannel
{
    Music,
    SoundEffects
}

/// <summary>Persistent volume state shared by every scene.</summary>
public static class GameAudioSettings
{
    private const string MasterKey = "audio.master";
    private const string MusicKey = "audio.music";
    private const string SfxKey = "audio.sfx";

    private static float masterVolume = 1f;
    private static float musicVolume = 0.8f;
    private static float sfxVolume = 0.8f;

    public static event Action VolumesChanged;

    public static float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = masterVolume;
            VolumesChanged?.Invoke();
        }
    }

    public static float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            VolumesChanged?.Invoke();
        }
    }

    public static float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            VolumesChanged?.Invoke();
        }
    }

    public static float GetChannelVolume(GameAudioChannel channel) =>
        channel == GameAudioChannel.Music ? musicVolume : sfxVolume;

    public static void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.8f);
        AudioListener.volume = masterVolume;
        VolumesChanged?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
        PlayerPrefs.SetFloat(MusicKey, musicVolume);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        PlayerPrefs.Save();
    }
}

/// <summary>
/// Add this beside an AudioSource and choose Music or SoundEffects to make it
/// follow the corresponding menu slider while preserving its authored volume.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class GameAudioSource : MonoBehaviour
{
    [SerializeField] private GameAudioChannel channel = GameAudioChannel.SoundEffects;
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplyVolume();
    }

    private void OnEnable()
    {
        GameAudioSettings.VolumesChanged += ApplyVolume;
        ApplyVolume();
    }

    private void OnDisable()
    {
        GameAudioSettings.VolumesChanged -= ApplyVolume;
    }

    private void OnValidate()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = baseVolume * GameAudioSettings.GetChannelVolume(channel);
        }
    }
}
