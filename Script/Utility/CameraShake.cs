using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] AnimationCurve decay_rate_;

    private static CameraShake instance_;
    private ShakeModule shake_module_;

    public static void Shake(float stength, float duration)
    {
        instance_.shake_module_.Shake(stength, duration);
    }

    public static void Pause()
    {
        instance_.shake_module_.Paused = true;
    }

    public static void Resume()
    {
        instance_.shake_module_.Paused = false;
    }

    void Awake()
    {
        if (instance_ == null)
        {
            instance_ = this;
            shake_module_ = this.gameObject.AddComponent<ShakeModule>();
            shake_module_.Init(decay_rate_);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

}
