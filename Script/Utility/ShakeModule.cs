using System.Collections.Generic;
using UnityEngine;

public class ShakeModule : MonoBehaviour
{
    private class Request
    {
        public float Progress => (Time.time - _startTime) / _duration;

        public float Strength { get; }
        private float _duration;
        private float _startTime;

        public Request(float strength, float duration)
        {
            Strength = strength;
            _duration = duration;
            _startTime = Time.time;
        }
    }

    public bool IsShaking => !Paused && _requests?.Count > 0;
    public bool Paused;

    [SerializeField] AnimationCurve decay_rate_;

    private List<Request> _requests;
    private Vector3 _originalLocalPos;

    public void Init(AnimationCurve decayRate)
    {
        decay_rate_ = decayRate;
    }

    public void ShakeWeak()
    {
        Shake(0.075f, 0.075f);
    }

    public void ShakeMedium()
    {
        Shake(0.09f, 0.09f);
    }

    public void ShakeStrong()
    {
        Shake(0.15f, 0.15f);
    }

    public void Shake(float strength, float duration)
    {
        _requests.Add(new Request(strength, duration));
    }

    private void Awake()
    {
        _requests = new List<Request>();
        _originalLocalPos = this.transform.localPosition;
    }

    private void FixedUpdate()
    {
        if (!IsShaking)
        {
            return;
        }

        float strongestShake = -float.MaxValue;

        // find strongest shake
        for (int i = 0; i < _requests.Count; ++i)
        {
            var request = _requests[i];
            float shake = request.Strength * decay_rate_.Evaluate(request.Progress);

            if (shake > strongestShake)
            {
                strongestShake = shake;
            }
        }

        // perform shake
        this.transform.localPosition = _originalLocalPos + (Random.insideUnitSphere * Time.timeScale * strongestShake);

        // remove all finished requests
        _requests.RemoveAll(elem => elem.Progress >= 1);

        if (!IsShaking)
        {
            this.transform.localPosition = _originalLocalPos;
        }
    }
}
