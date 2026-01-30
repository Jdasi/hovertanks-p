using UnityEngine;

public class EffectsSource : MonoBehaviour
{
    private enum State
    {
        Idle,
        Playing,
        Paused
    }

    public bool isPlaying => _state == State.Playing;
    public AudioClip clip { get => _source.clip; set => _source.clip = value; }
    public float volume { get => _source.volume; set => _source.volume = value; }
    public float pitch { get => _source.pitch; set => _source.pitch = value; }
    public bool loop { get => _source.loop; set => _source.loop = value; }

    [SerializeField] AudioSource _source;
    [SerializeField] EffectAudioSettings _defaultSettings;

    private State _state;

    public void Awake()
    {
        Init(_defaultSettings);
    }

    private void OnDestroy()
    {
    }

    public void Init(EffectAudioSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        _source.clip = settings.Clip;
        _source.volume = Modulate(settings.Volume, settings.VolumeVariance);
        _source.pitch = Modulate(settings.Pitch, settings.PitchVariance);

        if (settings.PlayOnAwake)
        {
            Play();
        }
    }

    public void Play(EffectAudioSettings settings)
    {
        Init(settings);
        Play();
    }

    public void Play()
    {
        if (_state == State.Paused)
        {
            return;
        }

        _source.Play();
        _state = State.Playing;
    }

    public void PlayAtPoint(EffectAudioSettings settings, Vector3 position)
    {
        transform.position = position;
        Play(settings);
    }

    public void PlayAtPoint(Vector3 position)
    {
        transform.position = position;
        Play();
    }

    public void Stop()
    {
        _source.Stop();
        _state = State.Idle;
    }

    private void OnGamePaused()
    {
        if (_state == State.Playing)
        {
            _source.Pause();
            _state = State.Paused;
        }
    }

    private void OnGameUnPaused()
    {
        if (_state == State.Paused)
        {
            _source.UnPause();
            _state = State.Playing;
        }
    }

    private float Modulate(float original, float variance)
    {
        if (variance == 0)
        {
            return original;
        }

        return original + Random.Range(-variance, variance);
    }

    private void Update()
    {
    }
}
