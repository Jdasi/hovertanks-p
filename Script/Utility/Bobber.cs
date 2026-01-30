using UnityEngine;

public class Bobber : MonoBehaviour
{
    [SerializeField] float strength_ = 5;
    [SerializeField] float speed_ = 2;

    private float original_y_;
    private float _randomOffset;

    void Awake()
    {
        original_y_ = transform.localPosition.y;
        _randomOffset = Random.Range(0, 1000);
    }

    void Update()
    {
        float y = original_y_ + Mathf.Sin(_randomOffset + (Time.time * speed_)) * strength_;
        transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
    }
}
