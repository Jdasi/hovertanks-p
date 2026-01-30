using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformFollower : MonoBehaviour
{
    public Vector3 offset_;

    public Transform Target
    {
        get
        {
            return target_;
        }

        set
        {
            target_ = value;
            this.gameObject.SetActive(true);
        }
    }

    [SerializeField] Transform target_;

    public void Update()
    {
        if (Target == null)
        {
            this.gameObject.SetActive(false);
            return;
        }

        transform.position = new Vector3(Target.transform.position.x,
            Target.transform.position.y, Target.transform.position.z) + offset_;
    }
}
