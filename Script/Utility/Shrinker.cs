using UnityEngine;

public class Shrinker : MonoBehaviour
{
    [SerializeField] float _timeBeforeShrink;

    private float _shrinkTimer;

    public void Init(float timeBeforeShrink)
    {
        _timeBeforeShrink = timeBeforeShrink;
    }

    private void Update()
    {
        _shrinkTimer += Time.deltaTime;

        if (_shrinkTimer < _timeBeforeShrink)
        {
            return;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 2.5f * Time.deltaTime);

        if (transform.localScale.sqrMagnitude <= 0.25f)
        {
            Destroy(this.gameObject);
        }
    }
}
