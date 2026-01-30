using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] float rotation_speed_ = -50;
    [SerializeField] Vector3 rotation_axis_ = Vector3.up;

    private void Awake()
    {
        rotation_axis_ = rotation_axis_.normalized;
    }

    private void Update()
    {
		transform.Rotate(rotation_axis_ * Time.deltaTime * rotation_speed_);
    }
}
