using UnityEngine;

public class Dancer : MonoBehaviour
{
    [SerializeField] float _radius;
    [SerializeField] float _intervalMin;
    [SerializeField] float _intervalMax;

    private float _nextWarpTime;

    private void Start()
    {
        RefreshNextWarpTime();
    }

    private void Update()
    {
        if (Time.time < _nextWarpTime)
        {
            return;
        }

        transform.localPosition = Random.insideUnitSphere * _radius;
        RefreshNextWarpTime();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.localPosition, _radius);
    }

    private void RefreshNextWarpTime()
    {
        _nextWarpTime = Time.time + Random.Range(_intervalMin, _intervalMax);
    }
}
