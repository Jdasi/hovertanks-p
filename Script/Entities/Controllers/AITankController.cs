using HoverTanks.AI;
using UnityEngine;
using UnityEngine.AI;

namespace HoverTanks.Entities
{
    [CreateAssetMenu(menuName = "Controllers/AI Tank")]
    public class AITankController : AIPawnController
    {
        private enum MoveStates
        {
            Idle,
            DodgingProjectile,
            MovingToEngage,
            Weaving,
        }

        private enum AimStates
        {
            Idle,
            AimingAtTarget,
            AimingInDir,
        }

        [Header("Target Acquisition")]
        [SerializeField] LayerMask _obstacleLayer;

        [Header("Movement")]
        [SerializeField] float _advanceDist;
        [SerializeField] float _retreatDist;

        [Header("Skills")]
        [SerializeField][Range(0, 100)] int _aggressionSkill;
        [SerializeField][Range(0, 100)] int _leadTargetSkill;
        [SerializeField][Range(0, 10)] int _projectileDodgeSkill;
        [SerializeField] bool _usesTurbo;

        // target acquisition
        private AITargetingModule _targetingModule;
        private ProjectileDirect _dodgeProjectile;

        // pathing
        private NavMeshPath _path;
        private uint _nextCorner;

        private float _nextWeaveTime;
        private Vector3 _weavePos;

        private MoveStates _moveState;
        private AimStates _aimState;

        private float _acquireProjectileCooldown;
        private float _calculatePathCooldown;

        protected override void OnInit()
        {
            base.OnInit();

            _targetingModule = new AITargetingModule(Pawn, _obstacleLayer);
            _path = new NavMeshPath();
        }

        protected override void OnEnabled()
        {
            TriggerShootCooldown(0.5f);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            _acquireProjectileCooldown -= Time.deltaTime;
            _calculatePathCooldown -= Time.deltaTime;

            HandleProjectileDetection();
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            _targetingModule.FixedUpdate();

            HandleMoveState();
            HandleAimState();
        }

        private void HandleProjectileDetection()
        {
            if (_acquireProjectileCooldown > 0)
            {
                return;
            }

            _acquireProjectileCooldown = 0.1f;

            ProjectileDirect closestProj = null;
            float closestDist = Mathf.Infinity;

            foreach (var projectile in EntityManager.ActiveDirectProjectiles.Values)
            {
                if (projectile == null)
                {
                    continue;
                }

                float dist = Vector3.Distance(Pawn.Position, projectile.transform.position);

                if (dist > _projectileDodgeSkill)
                {
                    continue;
                }

                // determine if its a threat
                var threatDir = (Pawn.Position - projectile.transform.position).normalized;
                if (Vector3.Dot(threatDir, projectile.transform.forward) < 0.5f)
                {
                    continue;
                }

                if (dist < closestDist)
                {
                    closestProj = projectile;
                    closestDist = dist;
                }
            }

            if (closestProj == null)
            {
                _dodgeProjectile = null;
                return;
            }

            _dodgeProjectile = closestProj;
            _moveState = MoveStates.DodgingProjectile;
        }

        private void HandleMoveState()
        {
            switch (_moveState)
            {
                case MoveStates.Idle:
                {
                    if (_targetingModule.Target != null)
                    {
                        if (_targetingModule.DistToTarget >= _advanceDist
                            || !_targetingModule.HasLOS)
                        {
                            _moveState = MoveStates.MovingToEngage;
                        }
                        else
                        {
                            _moveState = MoveStates.Weaving;
                        }

                        return;
                    }

                    Pawn.ClearMove();
                } break;

                case MoveStates.DodgingProjectile:
                {
                    if (_dodgeProjectile == null
                        || _dodgeProjectile.IsDetonated)
                    {
                        Pawn.StopTurbo();
                        _moveState = MoveStates.Idle;
                        return;
                    }

                    Vector3 threatDir = (Pawn.Position - _dodgeProjectile.transform.position).normalized;
                    Vector3 dodgeDir = JHelper.AngleDir(_dodgeProjectile.transform.forward, threatDir, Vector3.up) < 0 ?
                        -_dodgeProjectile.transform.right :
                        _dodgeProjectile.transform.right;

                    if (_usesTurbo)
                    {
                        if (!Pawn.IsOverheating)
                        {
                            Pawn.StartTurbo();
                        }
                        else
                        {
                            Pawn.StopTurbo();
                        }
                    }

                    if (NavMesh.SamplePosition(Pawn.Position + dodgeDir * 2, out var hit, 1, NavMesh.AllAreas))
                    {
                        Pawn.Move(dodgeDir);
                    }
                    else
                    {
                        Pawn.Move(-dodgeDir);
                    }
                } break;

                case MoveStates.MovingToEngage:
                {
                    if (_targetingModule.Target == null)
                    {
                        _moveState = MoveStates.Idle;
                        return;
                    }

                    if (_targetingModule.DistToTarget < _advanceDist
                        && _targetingModule.HasLOS
                        && _aggressionSkill >= 70)
                    {
                        _moveState = MoveStates.Weaving;
                        return;
                    }

                    // calc a path to the target
                    if (_calculatePathCooldown <= 0)
                    {
                        _calculatePathCooldown = 1;
                        var pos = DetermineNextDestination();

                        if (NavMesh.SamplePosition(pos, out var hit, 100, NavMesh.AllAreas))
                        {
                            CalculatePath(hit.position);
                        }
                    }

                    FollowPath();
                } break;

                case MoveStates.Weaving:
                {
                    if (_targetingModule.Target == null)
                    {
                        _moveState = MoveStates.Idle;
                        return;
                    }

                    if (_targetingModule.DistToTarget >= _advanceDist
                        || !_targetingModule.HasLOS)
                    {
                        _moveState = MoveStates.MovingToEngage;
                        return;
                    }

                    // move way from target
                    if (_targetingModule.DistToTarget < _retreatDist)
                    {
                        Vector3 retreatDir = (Pawn.Position - _targetingModule.Target.Position).normalized;

                        if (NavMesh.SamplePosition(Pawn.Position + retreatDir * 2, out var hit, 1, NavMesh.AllAreas))
                        {
                            Pawn.Move(retreatDir);
                        }
                        else
                        {
                            Pawn.Move(-retreatDir);
                        }
                    }
                    // move about at random
                    else
                    {
                        // determine weave
                        if (Time.time >= _nextWeaveTime
                            || _weavePos == default)
                        {
                            _nextWeaveTime = Time.time + Random.Range(0.5f, 1);
                            _weavePos = Pawn.Position + new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));

                            if (!NavMesh.SamplePosition(_weavePos, out var hit, 1, NavMesh.AllAreas))
                            {
                                _weavePos = default;
                            }
                        }

                        // check if weave done
                        if (JHelper.FlatDistance(Pawn.Position, _weavePos) <= 0.5f
                            || Time.time - _nextWeaveTime > 4)
                        {
                            _weavePos = default;
                            _nextWeaveTime = 0;
                        }

                        // move to weave pos
                        if (_weavePos != default)
                        {
                            Vector3 dodgeDir = _weavePos - Pawn.Position;
                            Pawn.Move(dodgeDir);
                        }
                    }
                } break;
            }
        }

        private void HandleAimState()
        {
            switch (_aimState)
            {
                case AimStates.Idle:
                {
                    if (_targetingModule.Target != null
                        && _targetingModule.HasLOS)
                    {
                        TriggerShootCooldown(0.5f);
                        _aimState = AimStates.AimingAtTarget;
                    }
                    else
                    {
                        Pawn.StopAiming();
                        Pawn.StopShoot();
                    }
                } break;

                case AimStates.AimingAtTarget:
                {
                    if (_targetingModule.Target == null
                        || !_targetingModule.HasLOS)
                    {
                        _aimState = AimStates.Idle;
                        return;
                    }

                    // try leading the target
                    var targetPos = AITargeting.LeadUnit(Pawn, _targetingModule.Target, _leadTargetSkill, _obstacleLayer);

                    Pawn.StartAiming(targetPos);

                    // handle shooting
                    if (CanShoot())
                    {
                        bool shouldStopShoot = false;

                        if (_targetingModule.DistToTarget <= _maxShootDist
                            && Pawn.IsPawnFacing(_targetingModule.Target.Position, 20))
                        {
                            // perform one last raycast to make sure we don't shoot a wall with old info
                            var dir = (_targetingModule.Target.SightPoint.position - Pawn.Position).normalized;
                            Physics.Raycast(Pawn.Position, dir, out var hitInfo, 100, _obstacleLayer);

                            if (hitInfo.transform == _targetingModule.Target.transform)
                            {
                                Pawn.Shoot();
                                _burstCountdown -= Time.deltaTime;
                            }
                            else
                            {
                                shouldStopShoot = true;
                            }

                            if (!_isBurstInProgress)
                            {
                                TriggerShootCooldown();
                            }
                        }
                        else
                        {
                            shouldStopShoot = true;
                        }

                        if (shouldStopShoot)
                        {
                            RestartBurstCountdown();
                            Pawn.StopShoot();
                        }
                    }
                    else
                    {
                        Pawn.StopShoot();
                    }
                } break;
            }
        }

        private Vector3 DetermineNextDestination()
        {
            var origin = GetSearchOrigin();
            var searchRadius = GetSearchRadius();

            Vector2 randomCirclePos = Random.insideUnitCircle * searchRadius;
            Vector3 searchArea = new Vector3(randomCirclePos.x, 0, randomCirclePos.y);

            return origin + searchArea;
        }

        private Vector3 GetSearchOrigin()
        {
            Vector3 origin;

            if (_targetingModule.Target != null)
            {
                Vector3 dirToTarget = (_targetingModule.Target.Position - Pawn.Position).normalized;
                float aggressionMod = (float)_aggressionSkill / 100;
                origin = Pawn.Position + dirToTarget * _targetingModule.DistToTarget * aggressionMod;
            }
            else
            {
                origin = Pawn.Position;
            }
        
            return origin;
        }

        private float GetSearchRadius()
        {
            return (100 - _aggressionSkill) * 0.1f;
        }

        private void CalculatePath(Vector3 destination)
        {
            NavMesh.CalculatePath(Pawn.Position, destination, NavMesh.AllAreas, _path);
            _nextCorner = 0;
        }

        private void FollowPath()
        {
            // abort if no path
            if (_path == null
                || _path.corners == null
                || _path.corners.Length == 0)
            {
                return;
            }

            // move along path
            float distToNext = Vector3.Distance(Pawn.Position, _path.corners[_nextCorner]);
            if (distToNext < 1)
            {
                ++_nextCorner;
            }

            // abort if path complete
            if (_nextCorner >= _path.corners.Length)
            {
                _path.ClearCorners();
                Pawn.StopTurbo();
                Pawn.ClearMove();

                return;
            }

            // height fudging
            Vector3 cornerPos = _path.corners[_nextCorner];
            cornerPos.y = Pawn.Position.y;

            if (_usesTurbo)
            {
                if (_targetingModule.DistToTarget / _advanceDist >= 1.5f)
                {
                    Pawn.StartTurbo();
                }
                else
                {
                    Pawn.StopTurbo();
                }
            }

            Vector3 moveDir = cornerPos - Pawn.Position;
            Pawn.Move(moveDir);
        }

    #if DEBUG
        public override void OnDrawGizmos()
        {
            if (_path != null && _path.corners.Length >= 2)
            {
                for (int i = 0; i < _path.corners.Length - 1; ++i)
                {
                    Vector3 thisPoint = _path.corners[i];
                    Vector3 nextPoint = _path.corners[i + 1];

                    Gizmos.color = i + 1 == _nextCorner ? Color.red : Color.white;
                    Gizmos.DrawLine(thisPoint, nextPoint);
                    Gizmos.DrawSphere(nextPoint, 0.1f);

                    Gizmos.color = i == _nextCorner ? Color.red : Color.white;
                    Gizmos.DrawSphere(thisPoint, 0.1f);
                }
            }
        }

        /*
        public override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetSearchOrigin(), Mathf.Max(1, GetSearchRadius()));
        }
        */
    #endif
    }
}
