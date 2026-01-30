using UnityEngine;

public class AnimatedSprite : MonoBehaviour
{
    public bool DestroyIfNotPlaying;
    public bool IsPlaying { get; private set; }

    [SerializeField] Sprite[] frames_;
    [SerializeField] bool looping_;
    [SerializeField] float fps_ = 20;
    [SerializeField] bool play_on_awake_ = true;

    [Header("References")]
    [SerializeField] SpriteRenderer renderer_;

    private float play_time_;

    public void SetOrderInLayer(int order)
    {
        renderer_.sortingOrder = order;
    }

    public void Play()
    {
        if (IsPlaying)
        {
            return;
        }

        IsPlaying = true;
        play_time_ = 0;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    void Awake()
    {
        if (play_on_awake_)
        {
            Play();
        }
    }

    void Update()
    {
        if (!IsPlaying)
        {
            if (DestroyIfNotPlaying)
            {
                Destroy(this.gameObject);
            }

            return;
        }

        play_time_ += Time.deltaTime;
        int index = (int)(play_time_ * fps_) % frames_.Length;

        renderer_.sprite = frames_[index];

        if (!looping_ && index == frames_.Length - 1)
        {
            IsPlaying = false;
        }
    }
}
