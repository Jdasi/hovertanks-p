using UnityEngine;

public class Banker : MonoBehaviour
{
    [SerializeField] Transform _forwardRoot;
    [SerializeField] float _amount;
    [SerializeField] float _lerpSpeed;

    private Vector3 _prevFwd;

    private void Awake()
    {
        RefreshPrevFwd();
    }

    private void FixedUpdate()
    {
        float diff = Vector3.Angle(_forwardRoot.forward, _prevFwd);

        if (JHelper.AngleDir(_prevFwd, _forwardRoot.forward, Vector3.up) > 0)
        {
            diff = -diff;
        }

        Vector3 euler = new Vector3(0, 0, diff * _amount);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(euler), _lerpSpeed * Time.deltaTime);

        RefreshPrevFwd();
    }

    private void RefreshPrevFwd()
    {
        _prevFwd = _forwardRoot.forward;
    }
}
