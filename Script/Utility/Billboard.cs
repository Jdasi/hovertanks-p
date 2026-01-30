using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] bool _zeroRotation;

    void LateUpdate()
    {
        if (_zeroRotation)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            if (Camera.main == null)
            {
                return;
            }

            transform.LookAt(Camera.main.transform.position);
        }
    }
}
