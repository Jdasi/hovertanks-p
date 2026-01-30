using HoverTanks.Entities;
using HoverTanks.Networking;
using UnityEngine;

[RequireComponent(typeof(ProjectileDirect))]
public class ProjectileProximityDetonate : MonoBehaviour
{
    [SerializeField] ProjectileDirect _projectile;
    [SerializeField] float _detonateRadius;
    [SerializeField] float _scanInterval = 0.3f;
    [SerializeField] LayerMask _scanLayer;
    [SerializeField] bool _ignoreSameTeam = true;

    private float _nextScanTime;

    private void Awake()
    {
        TriggerScanCooldown();
    }

    private void FixedUpdate()
    {
        if (!Server.IsActive)
        {
            Destroy(this);
            return;
        }

        if (Time.time < _nextScanTime)
        {
            return;
        }

        TriggerScanCooldown();

        var hits = Physics.OverlapSphere(transform.position, _detonateRadius, _scanLayer);

        foreach (var hit in hits)
        {
            var entity = hit.GetComponent<NetworkEntity>();

            if (entity == null)
            {
                continue;
            }

            if (entity.identity.entityId == _projectile.Owner.EntityId)
            {
                continue;
            }

            if (_ignoreSameTeam
                && JHelper.SameTeam(entity.identity.teamId, _projectile.Owner.TeamId))
            {
                continue;
            }

            _projectile.Detonate();
            break;
        }
    }

    private void TriggerScanCooldown()
    {
        _nextScanTime = Time.time + _scanInterval;
    }
}
