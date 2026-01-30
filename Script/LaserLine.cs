using UnityEngine;

public class LaserLine : MonoBehaviour
{
	[SerializeField] LineRenderer _laserLine;
    [SerializeField] LayerMask _laserHitLayer;

    private void Awake()
    {
    }

    private void LateUpdate()
	{
        HandleLaserLineSimple();
	}

    private void HandleLaserLineSimple()
    {
        _laserLine.positionCount = 2;
        _laserLine.SetPosition(0, _laserLine.transform.position);

        Vector3 laserLineEnd = _laserLine.transform.position + _laserLine.transform.forward * 5;

        if (Physics.Raycast(_laserLine.transform.position, _laserLine.transform.forward, out var hitInfo, 5, _laserHitLayer))
        {
            laserLineEnd = hitInfo.point;
        }

        _laserLine.SetPosition(1, laserLineEnd);
    }

	private void HandleLaserLine()
    {
        /*
        if (_unit == null)
        {
            return;
        }

        _laserLine.positionCount = 1;
        _laserLine.SetPosition(0, _laserLine.transform.position);

        int maxBounces = _unit.WeaponInfo?.GetProjectileMaxBounces() ?? 0;

        if (maxBounces > 0
            && _unit.IsAiming
            && _unit.ActiveStatusFlags.IsFlagSet(StatusFlags.AdvancedTargeting))
        {
            Vector3 laserPos = _laserLine.transform.position;
            Vector3 laserForward = _laserLine.transform.forward;

            // test for bounces
            for (int i = 0; i < maxBounces + 1; ++i)
            {
                bool didHit = Physics.Raycast(laserPos, laserForward, out var hitInfo, 100, HoverTanks.GameManager.instance.GeometryLayer);
                ++_laserLine.positionCount;

                if (didHit)
                {
                    // calculate bounce
                    Vector3 reflect = Vector3.Reflect(laserForward, hitInfo.normal);
                    Vector3 reflectPos = hitInfo.point + hitInfo.normal * 0.1f;

                    // update origin for subsequent bounces
                    laserPos = reflectPos;
                    laserForward = reflect;

                    _laserLine.SetPosition(_laserLine.positionCount - 1, laserPos);
                }
                else
                {
                    // go to end of raycast
                    _laserLine.SetPosition(_laserLine.positionCount - 1, laserPos + laserForward * 100);

                    break;
                }
            }
        }
        else
        {
            ++_laserLine.positionCount;

            Vector3 laserLineEnd = _laserLine.transform.position + _laserLine.transform.forward * 5;

            if (Physics.Raycast(_laserLine.transform.position, _laserLine.transform.forward, out var hitInfo, 5, _laserHitLayer))
            {
                laserLineEnd = hitInfo.point;
            }

            _laserLine.SetPosition(1, laserLineEnd);
        }
        */
    }
}
