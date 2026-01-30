using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Effect Settings")]
public class EffectAudioSettings : ScriptableObject
{
    public AudioClip Clip = null;

    [Space]
    public float Volume = 1;
    public float VolumeVariance = 0;

    [Space]
    public float Pitch = 1;
    public float PitchVariance = 0;

    [Space]
    public bool PlayOnAwake = true;
}
