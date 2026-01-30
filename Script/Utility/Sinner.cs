using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sinner : MonoBehaviour
{
    [SerializeField] float scale_speed_ = 1;
    [SerializeField] float sin_factor_ = 1;

    private Vector3 start_scale_;

    void Start()
    {
        start_scale_ = transform.localScale;
    }

    void Update()
    {
        float sin = Mathf.Sin(scale_speed_ * Time.time) * sin_factor_;
        transform.localScale = start_scale_ + (start_scale_ * sin);
    }

}
