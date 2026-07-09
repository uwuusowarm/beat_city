using UnityEngine;

public enum SfxType
{
    Punch,
    Kick,
    Jump,
    EnemyDeath,
    PlayerDeath,
    Special
}

public enum MusicType
{
    MainMenu,
    Stage1,
    Stage2,
}

[System.Serializable]
public class MusicEntry
{
    public MusicType musicType;
    public AudioClip[] musicClips;
}

[System.Serializable]
public class SfxEntry
{
    public SfxType sfxType;
    public AudioClip[] sfxClips;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private MusicEntry[] musicEntries;
    [SerializeField] private SfxEntry[] sfxEntries;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        UpdateVolume();
    }
    
    public void PlaySfx(SfxType type, int index)
    {
        foreach (SfxEntry entry in sfxEntries)
        {
            if (entry.sfxType == type)
            {
                if (entry.sfxClips == null || entry.sfxClips.Length == 0)
                {
                    Debug.LogWarning("No clips assigned for: " + type);
                    return;
                }

                if (index < 0 || index >= entry.sfxClips.Length)
                {
                    Debug.LogWarning("Index out of range for: " + type);
                    return;
                }

                AudioClip clip = entry.sfxClips[index];

                if (clip == null)
                {
                    Debug.LogWarning("Clip is null for: " + type);
                    return;
                }

                sfxSource.PlayOneShot(clip);
                return;
            }
        }

        Debug.LogWarning("No SfxEntry found for: " + type);
    }

    public void PlayMusic(MusicType type)
    {
        foreach (MusicEntry entry in musicEntries)
        {
            if (entry.musicType == type)
            {
                if (entry.musicClips == null || entry.musicClips.Length == 0)
                {
                    Debug.LogWarning("No music assigned for: " + type);
                    return;
                }

                AudioClip clip = entry.musicClips[0];

                if (clip == null)
                {
                    Debug.LogWarning("Music clip is null for: " + type);
                    return;
                }

                musicSource.clip = clip;
                musicSource.loop = true;
                musicSource.Play();
                return;
            }
        }

        Debug.LogWarning("No MusicEntry found for: " + type);
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void UnPauseMusic()
    {
        musicSource.UnPause();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    private void UpdateVolume()
    {
        musicSource.volume = masterVolume * musicVolume;
        sfxSource.volume = masterVolume * sfxVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        UpdateVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        UpdateVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = value;
        UpdateVolume();
    }
}
