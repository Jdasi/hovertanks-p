using HoverTanks.Networking;
using HoverTanks.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DamageThreshold
{
    public float Distance;
    public int Damage;
    public float Knockback;
}

public class Explosion : MonoBehaviour
{
    public bool HasDamageThresholds => _damageThresholds?.Count > 0;

    [SerializeField] List<DamageThreshold> _damageThresholds;
    [SerializeField] ElementType _damageType;
    [SerializeField] float _shake;
    [SerializeField] EffectAudioSettings _audio;

    private IdentityInfo _ownerInfo;
    private ProjectileStats _ownerProjectileInfo;
    private bool _created;

    public void Init(IdentityInfo owner, ProjectileStats ownerProjectileStats = null)
    {
        _ownerInfo = owner;
        _ownerProjectileInfo = ownerProjectileStats;

        Destroy(this.gameObject, 3);
    }

    public void BoostDamage(int amount)
    {
        if (_damageThresholds == null
            || _damageThresholds.Count == 0)
        {
            return;
        }

        foreach (var threshold in _damageThresholds)
        {
            threshold.Damage += amount;
        }
    }

    private void LateUpdate()
    {
        if (_created)
        {
            return;
        }

        _created = true;

        if (_shake > 0)
        {
            CameraShake.Shake(_shake, _shake);
        }

        AudioManager.PlayClipAtPoint(_audio, transform.position);

        HandleExplosionDamage();
    }

    private void HandleExplosionDamage()
    {
        if (!Server.IsActive)
        {
            return;
        }

        if (_damageThresholds == null
            || _damageThresholds.Count == 0)
        {
            return;
        }

        // sort by distance (ascending)
        _damageThresholds.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        var pos = transform.position;
        var hits = Physics.OverlapSphere(pos, _damageThresholds[_damageThresholds.Count - 1].Distance, GameManager.instance.ExplosionLayer);

        var hitEntities = new List<EntityId>();

        foreach (var hit in hits)
        {
            var life = hit.GetComponent<LifeForce>();

            if (life == null)
            {
                continue;
            }

            if (hitEntities.Contains(life.identity.entityId))
            {
                continue;
            }

            float distToHit = (hit.transform.position - pos).magnitude;
            Vector3 dirToHit = (hit.transform.position - pos).normalized;
            Physics.Raycast(pos, dirToHit, out var hitInfo, distToHit, GameManager.instance.GeometryLayer);

            // check if any obstacles blocked the explosion
            if (hitInfo.transform != null)
            {
                continue;
            }

            int damage = 0;
            float knockback = 0;
            float radius = 0;

            foreach (var threshold in _damageThresholds)
            {
                if (distToHit < threshold.Distance)
                {
                    damage = threshold.Damage;
                    knockback = threshold.Knockback;
                    radius = threshold.Distance;

                    break;
                }
            }

            // only affect an entity once
            hitEntities.Add(life.identity.entityId);

            // damage other
            life.Damage(new DamageInfo(_ownerInfo, new ElementData(_damageType, damage, ElementFlags.IsAoe), _ownerProjectileInfo));

            // knockback other
            if (knockback != 0)
            {
                ApplyKnockback(life.gameObject, knockback);
            }
        }
    }

    private void ApplyKnockback(GameObject obj, float knockback)
    {
        if (!obj.gameObject.CompareTag("Unit"))
        {
            return;
        }

        var pawn = obj.GetComponent<Pawn>();

        if (pawn == null)
        {
            return;
        }

        if (!pawn.CanMove())
        {
            return;
        }

        using (var sendMsg = new EntityImpulseMsg()
		{
			EntityId = pawn.identity.entityId,
			Direction = JHelper.FlatDirection(transform.position, pawn.Position),
			Magnitude = knockback,
		})
		{
			ServerSend.ToAll(sendMsg);
		}
    }
}
