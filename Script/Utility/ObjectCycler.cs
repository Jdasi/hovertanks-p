using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCycler : MonoBehaviour
{
    [SerializeField] List<GameObject> objects_;
    [SerializeField] float cycle_speed_ = 0.1f;

    private float timer_;
    private int index_;

    void Start()
    {
        if (objects_ == null || objects_.Count == 0)
        {
            Destroy(this);
        }
        else
        {
            foreach (GameObject obj in objects_)
            {
                obj.SetActive(false);
            }

            objects_[0].SetActive(true);
        }
    }

    void Update()
    {
        timer_ += Time.deltaTime;

        if (timer_ >= cycle_speed_)
        {
            timer_ = 0;
            CycleNext();
        }
    }

    void CycleNext()
    {
        objects_[index_].SetActive(false);

        ++index_;

        if (index_ >= objects_.Count)
        {
            index_ = 0;
        }

        objects_[index_].SetActive(true);
    }

}
