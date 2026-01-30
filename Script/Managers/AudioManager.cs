using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Range(0, 1)] public float _musicVolume = 0.5f;
    [Range(0, 1)] public float _sfxVolume = 0.5f;

    [Space]
    [SerializeField] List<AudioClip> _musicClips;
    [SerializeField] List<AudioClip> _sfxClips;

    [Space]
    [SerializeField] float _silenceBuffer = 0.1f;

    [Space]
    [SerializeField] EffectsSource _effectsSourcePrefab;

    private static AudioManager _instance;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private AudioSource _sfxUnscaledSource;

    private AudioClip _lastClipPlayed;
    private float _silenceCooldown;

    public static void PlayClip(string clip_name)
    {
        PlayClip(_instance.GetAudioClip(clip_name));
    }

    public static void PlayClip(IList<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, clips.Count);
        PlayClip(clips[index]);
    }

    public static void PlayClip(AudioClip clip)
    {
        if (_instance._silenceCooldown > 0
            || _instance._lastClipPlayed == clip)
        {
            return;
        }

        _instance._lastClipPlayed = clip;
        _instance._silenceCooldown = _instance._silenceBuffer;

        if (clip != null)
        {
            _instance._sfxSource.PlayOneShot(clip);
        }
    }

    public static EffectsSource CreateEffectsSource(Transform parent = null)
    {
        return GameObject.Instantiate(_instance._effectsSourcePrefab, parent);
    }

    public static void PlayClipAtPoint(EffectAudioSettings settings, Vector3 position)
    {
        if (settings == null)
        {
            return;
        }

        if (settings.Clip == null)
        {
            return;
        }

        var source = CreateEffectsSource(_instance.transform);
        source.transform.position = position;
        source.name = "OneShotAudio";
        source.Init(settings);

        Destroy(source.gameObject, settings.Clip.length);
    }

    public static void PlayClipUnscaled(string clip)
    {
        PlayClipUnscaled(_instance.GetAudioClip(clip));
    }

    public static void PlayClipUnscaled(IList<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, clips.Count);
        PlayClipUnscaled(clips[index]);
    }

    public static void PlayClipUnscaled(AudioClip clip)
    {
        if (_instance._silenceCooldown > 0
            || _instance._lastClipPlayed == clip)
        {
            return;
        }

        _instance._lastClipPlayed = clip;
        _instance._silenceCooldown = _instance._silenceBuffer;

        if (clip != null)
        {
            _instance._sfxUnscaledSource.PlayOneShot(clip);
        }
    }

    public static void StopAllSFX()
    {
        _instance._sfxSource.Stop();
    }

    public AudioClip GetAudioClip(string clip_name)
    {
        return _sfxClips.Find(elem => elem != null
            && elem.name.Substring(0) == clip_name);
    }

    public void PlayRandomMusic()
    {
        if (_musicClips.Count == 0)
        {
            return;
        }

        _musicSource.Stop();

        int index = Random.Range(0, _musicClips.Count);

        _musicSource.clip = _musicClips[index];
        _musicSource.loop = true;

        _musicSource.Play();
    }

    void Awake()
    {
        if (_instance == null)
        {
            InitSingleton();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void InitSingleton()
    {
        _instance = this;

        GameObject audio_parent = new GameObject("Persistent Audio");
        audio_parent.transform.SetParent(this.transform);

        _musicSource = audio_parent.AddComponent<AudioSource>();
        _sfxSource = audio_parent.AddComponent<AudioSource>();
        _sfxUnscaledSource = audio_parent.AddComponent<AudioSource>();

        _musicSource.volume = _musicVolume;
        _sfxSource.volume = _sfxVolume;
        _sfxUnscaledSource.volume = _sfxVolume;

        PlayRandomMusic();
    }

    void Update()
    {
        _sfxSource.pitch = Time.timeScale;

        if (_silenceCooldown > 0)
        {
            _silenceCooldown -= Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        _lastClipPlayed = null;
    }

}
