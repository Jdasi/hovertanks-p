using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.AI
{
    public class AITargetingModule
    {
        public Pawn Target { get; private set; }
        public bool HasLOS { get; private set; }
        public float DistToTarget { get; private set; }

        private readonly IPawn _pawn;
        private readonly float _acquireTargetInterval;
        private readonly LayerMask _obstacleLayer;

        private float _acquireTargetCooldownTime;

        public AITargetingModule(IPawn pawn, LayerMask obstacleLayer, float acquireTargetInterval = 0.5f)
        {
            _pawn = pawn;
            _acquireTargetInterval = acquireTargetInterval;
            _obstacleLayer = obstacleLayer;
        }

        public void FixedUpdate()
        {
            if (Target != null)
            {
                DistToTarget = JHelper.FlatDistance(_pawn.Position, Target.Position);
            }

            if (HandleCooldown())
            {
                return;
            }

            AcquireBestTarget();
        }

        private bool HandleCooldown()
        {
            _acquireTargetCooldownTime -= Time.fixedDeltaTime;

            if (_acquireTargetCooldownTime > 0)
            {
                return true;
            }

            _acquireTargetCooldownTime = _acquireTargetInterval;

            return false;
        }

        private void AcquireBestTarget()
        {
            // reset state
            Target = null;
            HasLOS = false;

            int highestScore = 0;

            foreach (var team in EntityManager.ActivePawnsByTeam)
            {
                // skip same team
                if (JHelper.SameTeam(team.Key, _pawn.identity.teamId))
                {
                    continue;
                }

                foreach (var pawn in team.Value.Values)
                {
                    // skip self
                    if (pawn.identity.entityId == _pawn.identity.entityId)
                    {
                        continue;
                    }

                    // get dist to target
                    float distToVeh = JHelper.FlatDistance(pawn.Position, _pawn.Position);

                    bool hasLOStoVeh = true;
                    Vector3 targetPos = pawn.Position;

                    if (_obstacleLayer != default)
                    {
                        // check we can see the vehicle
                        if (!JHelper.CanSeeTransform(_pawn.SightPoint.position, pawn.transform, _obstacleLayer))
                        {
                            hasLOStoVeh = false;
                        }
                    }

                    // determine candidate score
                    int score = 1000 - (int)distToVeh + (hasLOStoVeh ? 1000 : 0);

                    if (score > highestScore)
                    {
                        Target = pawn;
                        HasLOS = hasLOStoVeh;

                        highestScore = score;
                    }
                }
            }

            UpdateDistToTarget();
        }

        private void UpdateDistToTarget()
        {
            DistToTarget = Target != null ? Vector3.Distance(Target.Position, _pawn.Position) : Mathf.Infinity;
        }
    }

    public static class AITargeting
    {
        /// <summary>
        /// Generates a position ahead of the target's movement vector.
        /// </summary>
        /// <param name="skill">Value between 0-100. The higher the greater the lead.</param>
        /// <param name="layerMask">Layers to check, this should probably be Vehicle and Geometry.</param>
        /// <returns>A position ahead of the target.</returns>
        public static Vector3 LeadUnit(this IPawn thisPawn, Pawn target, int skill, LayerMask layerMask)
        {
            // early abort if no ability to lead the target
            if (skill <= 0)
            {
                return target.Position;
            }

            float skillFactor = (float)skill / 100;

            // only lead the target if it's moving
            if (target.TargetMoveDir.sqrMagnitude <= 0)
            {
                return target.Position;
            }

            float distToVeh = Vector3.Distance(thisPawn.Position, target.Position);

            Vector3 leadPos = target.transform.position + target.TargetMoveDir * (distToVeh * (0.3f * skillFactor));
            Vector3 dirToLeadPos = (leadPos - thisPawn.SightPoint.position).normalized;
            float distToLeadPos = Vector3.Distance(target.SightPoint.position, thisPawn.SightPoint.position);

            Physics.Raycast(thisPawn.SightPoint.position, dirToLeadPos, out var hitInfo, distToLeadPos, layerMask);

            // check nothing obscured the leadpos
            if (hitInfo.transform == null
                || Vector3.Distance(hitInfo.point, leadPos) < 0.5f)
            {
                Debug.DrawRay(thisPawn.SightPoint.position, dirToLeadPos * distToLeadPos, Color.green);
                return leadPos;
            }

            Debug.DrawRay(thisPawn.SightPoint.position, dirToLeadPos * distToLeadPos, Color.red);
            return target.Position;
        }
    }
}
