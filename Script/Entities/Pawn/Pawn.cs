using HoverTanks.Events;
using HoverTanks.Loadouts;
using HoverTanks.Networking;
using HoverTanks.StatusEffects;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Entities
{
    public partial class Pawn : NetworkEntity, IPawnControl, IStatusEffectTarget, IModuleOwner
    {
        private enum SystemFlags
        {
            None = 0,

            Initialized = Bits.Bit0,
            InitializedLoadout = Bits.Bit1,
        }

        private enum TurnDir
        {
            None,
            Right,
            Left
        }

        public PawnClass PawnClass => _pawnClass;
        public Vector3 Position { get { if (this == null || gameObject == null || transform == null) return default; return transform.position; } }
        public Vector3 TargetMoveDir => _input.MoveDir;
        public Vector3 Velocity => _rb.linearVelocity;
        public Transform SightPoint => _rotationTransform;
        public LifeForce Life => _life;
        public float CurrentMovePower { get; private set; }
		public float CurrentTurnSpeed { get; private set; }
        public float StationaryTimer { get; private set; }
        public bool IsTurboOn => _input.IsTurboOn;
        public bool IsInRamState => _ramStateTimer > RAM_TEST_TIME_THRESHOLD;
        public bool IsOverheating => _life.IsOverheating;
        public IWeaponInfo WeaponInfo => _weapon;
        public IModuleInfo ModuleInfo => _module;
        public IStatusEffectManager StatusEffectManager => _statusEffectManager;

        [SerializeField] PawnClass _pawnClass;

        [Header("Components")]
        [SerializeField] AIPawnController _aiControllerPrefab;
        [SerializeField] PawnVisualization _visualization;
        [SerializeField] Transform _rotationTransform;
        [SerializeField] Rigidbody _rb;
        [SerializeField] LifeForce _life;

        [Header("Movement")]
        [SerializeField][FactoredFloatRange(5, 15)] FactoredFloat _forwardSpeed;
        [SerializeField][FactoredFloatRange(5, 15)] FactoredFloat _strafeSpeed;
        [SerializeField][FactoredFloatRange(5, 15)] FactoredFloat _reverseSpeed;
        [SerializeField][FactoredFloatRange(8, 18)] FactoredFloat _tiltVectorAccel;

        [Space]
        [SerializeField][FactoredFloatRange(80, 200)] FactoredFloat _turnSpeed;
        [SerializeField][FactoredFloatRange(8, 18)] FactoredFloat _turnAccel;

        [Header("Loadout")]
        [SerializeField] WeaponSlot _weaponSlot;
        [SerializeField] Loadout _loadout;
        [SerializeField] Transform _moduleProcPoint;

        private const float RAM_STATE_DOT_THRESHOLD = 0.7f;
        private const float RAM_STATE_SPEED_PERCENT_THRESHOLD = 0.55f;
        private const float RAM_TEST_TIME_THRESHOLD = 0.2f;
        private const float RAM_COOLDOWN = 1f;
        private const float TURBO_HEAT_DAMAGE_DEFAULT = 0.075f;
        private const float TURBO_HEAT_DAMAGE_COOL_TIME_DEFAULT = 0.75f;
        private const float TURBO_FORWARD_SPEED_MODIFIER = 1.6f;
        private const float TURBO_STRAFE_SPEED_MODIFIER = 1.25f;
        private const float TURBO_REVERSE_SPEED_MODIFIER = 1.2f;
        private const float TURBO_TURN_SPEED_PERCENT_REDUCTION = 0.6f;
        private const float TURBO_TILT_ACCEL_MODIFIER = 0.5f;
        private const float MIN_TIME_BEFORE_USE_DIFFERENT_EQUIPMENT = 0.4f;

        private TankSnapshot _lastSendSnapshot;
        private PawnStateData _observerSyncState;
        private Interpolator _interpolator;
        private const float FORCE_SYNC_INTERVAL = 2f;

        private List<int> _yLockRequestSources;
        private int _nextYLockRequestHandle;

        private StatusEffectManager _statusEffectManager;
        private PawnController _controller;
        private Weapon _weapon;
        private Module _module;
        private InputData _input;
        private Vector3 _tiltVector;

        private Bitset _flags;
        private float _nextRamTime;
        private float _ramStateTimer;
        private EquipmentType _lastUsedEquipment;
        private float _useDifferentEquipmentCountdown;

        /// <summary>
        /// Creates a struct containing basic statistical information, e.g. for use with UI. Consider caching.
        /// </summary>
        public PawnBasicInfo GetBasicInfo()
        {
            var info = new PawnBasicInfo
            {
                WeaponName = _weaponSlot.Prefab?.name ?? "None",
                ForwardSpeed = _forwardSpeed,
                WeaponType = _weaponSlot.Prefab?.WeaponType ?? WeaponTypes.Invalid,
            };

            if (_weaponSlot.Prefab != null)
            {
                info.ProjectileInfo = _weaponSlot.Prefab.GetProjectileBasicInfo();
            }

            return info;
        }

        public Configuration Configure()
        {
            return new Configuration(this);
        }

        public bool TryGetMountPoint(MountPoint point, out Transform mount)
        {
            return _visualization.TryGetMountPoint(point, out mount);
        }

        public Color[] GetTintColors()
        {
            return _visualization.GetTintColors();
        }

        public void InitAIController(bool cleanupExisting = false)
        {
            InitController(_aiControllerPrefab, cleanupExisting);
        }

        public void InitController(PawnController controllerPrefab, bool cleanupExisting = false)
        {
            if (controllerPrefab == null)
            {
                return;
            }

            // verify cleanup
            if (_controller != null && !cleanupExisting)
            {
                return;
            }

            // verify controller authority
            if (controllerPrefab.isPlayer)
            {
                if (identity.clientId == ClientId.Invalid
                    || identity.playerId == PlayerId.Invalid)
                {
                    return;
                }
            }
            else // ai controller
            {
                if (!Server.IsActive)
                {
                    return;
                }
            }

            CleanupController();

            _controller = Instantiate(controllerPrefab);
            _controller.Init(this);
        }

        public void CleanupController()
        {
            if (_controller == null)
            {
                return;
            }

            Destroy(_controller);
            _controller = null;

            ClearAllInput();
        }

        public void SyncState(PawnStateMsg msg)
        {
            Vector3 posToUse = msg.HasPosition ? msg.Position : transform.position;
            _observerSyncState.Refresh(posToUse, JHelper.HeadingToRotation(msg.Heading));

            _input.MoveDir = msg.MoveDir;
            _input.AimDir = msg.AimDir;
            _input.IsTurboOn = msg.IsTurboOn;
            CurrentTurnSpeed = msg.TurnSpeed;

            // immediately forward to observers
            if (Server.IsActive)
            {
                ServerSend.ToAllExcept(msg, GameClient.ClientId, identity.clientId);
            }
        }

        public bool CanMove()
        {
            if (_rb.constraints == RigidbodyConstraints.FreezeAll)
            {
                return false;
            }

            return _forwardSpeed > 0
                || _strafeSpeed > 0
                || _reverseSpeed > 0;
        }

        public void Move(float horizontal, float vertical)
        {
            Move(new Vector3(horizontal, 0, vertical));
        }

        public void Move(Vector3 dir)
        {
            if (!CanMove())
            {
                return;
            }

            float mag = dir.magnitude;

	        // small forces don't adequately move the vehicle
	        if (mag < 0.2f)
	        {
		        ClearMove();
		        return;
	        }

	        dir.Normalize();

            _input.MoveDir = dir;
        }

        public void ClearMove()
        {
            _input.MoveDir = default;
        }

        public void AddForce(Vector3 force)
        {
            _rb.AddForce(force * Time.fixedDeltaTime, ForceMode.Force);
        }

        public void AddImpulse(Vector3 impulse)
        {
            _rb.AddForce(impulse, ForceMode.Impulse);
        }

        public int AddYLock()
        {
            _yLockRequestSources.Add(_nextYLockRequestHandle);

            // enable y lock
            _rb.constraints |= RigidbodyConstraints.FreezePositionY;

            return _nextYLockRequestHandle++;
        }

        public void RemoveYLock(int handle)
        {
            if (!_yLockRequestSources.Remove(handle))
            {
                return;
            }

            if (_yLockRequestSources.Count > 0)
            {
                return;
            }

            // disable y lock
            _rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        }

        public void StartAiming(Vector3 pos)
        {
            _input.AimDir = JHelper.FlatDirection(transform.position, pos);
        }

        public void StopAiming()
        {
            _input.AimDir = default;
        }

        public void StartTurbo()
        {
            _input.IsTurboOn = true;
        }

        public void StopTurbo()
        {
            _input.IsTurboOn = false;
        }

        public void Shoot(Vector3 aimPos = default)
        {
            if (_lastUsedEquipment != EquipmentType.Weapon
                && _useDifferentEquipmentCountdown > 0)
            {
                return;
            }

            _weapon?.Use(aimPos);
        }

        public void StopShoot()
        {
            _weapon?.StopUse();
        }

        public void Reload()
        {
            _weapon?.Recharge();
        }

        public void StopReload()
        {
            _weapon?.StopRecharge();
        }

        public void StartModule(Vector3 aimPos = default)
        {
           if (_lastUsedEquipment != EquipmentType.Module
                && _useDifferentEquipmentCountdown > 0)
            {
                return;
            }

            _module?.Use(aimPos);
        }

        public void StopModule()
        {
            _module?.StopUse();
        }

        public void ClearAllInput()
        {
            ClearMove();
            StopAiming();
            StopShoot();
            StopModule();
            StopReload();
            StopTurbo();
        }

        public Slots GetEnabledModsForEquipment(EquipmentType equipmentType)
        {
            switch (equipmentType)
            {
                case EquipmentType.Weapon: return _loadout.EnabledWeaponMods;
                case EquipmentType.Module: return _loadout.EnabledModuleMods;
            }

            return Slots.None;
        }

        public void Init()
        {
            if (_flags.IsSet((int)SystemFlags.Initialized))
            {
                Log.Error(LogChannel.Pawn, "Init - base already initialized, this shouldn't be called twice");
                return;
            }

            _flags.Set((int)SystemFlags.Initialized);

            if (_rotationTransform == null)
            {
                _rotationTransform = transform;
            }

            _yLockRequestSources = new List<int>();
            _statusEffectManager = new StatusEffectManager(this);
            _interpolator = new Interpolator(this);
            _observerSyncState.Refresh(transform.position, _rotationTransform.rotation);
            _visualization.Init(this, _rotationTransform);

            // life listeners
            _life.OnDeathServer += OnDeathServer;
            _life.OnDeathBasic += OnDeathBasic;

            NetworkEvents.Subscribe<EntityImpulseMsg>(OnEntityImpulseMsg);
            NetworkEvents.Subscribe<PawnRamStateChangeMsg>(OnPawnRamStateChangeMsg);
            NetworkEvents.Subscribe<RamRequestMsg>(OnRamRequestMsg);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _statusEffectManager?.Cleanup();

            if (_weapon != null)
            {
                Destroy(_weapon);
                _weapon = null;
            }

            if (_module != null)
            {
                Destroy(_module);
                _module = null;
            }

            CleanupController();

            NetworkEvents.Unsubscribe<EntityImpulseMsg>(OnEntityImpulseMsg);
            NetworkEvents.Unsubscribe<PawnRamStateChangeMsg>(OnPawnRamStateChangeMsg);
            NetworkEvents.Unsubscribe<RamRequestMsg>(OnRamRequestMsg);
        }

        public void OverrideLoadout(Loadout package)
        {
            _loadout = package;
        }

        public void InitLoadout()
        {
            if (!_flags.IsSet((int)SystemFlags.Initialized))
            {
                Log.Error(LogChannel.Pawn, "InitLoadout - base not initialized, ensure Init is called first");
                return;
            }

            if (_flags.IsSet((int)SystemFlags.InitializedLoadout))
            {
                Log.Error(LogChannel.Pawn, "InitLoadout - loadout already initialized, this shouldn't be called twice");
                return;
            }

            _flags.Set((int)SystemFlags.InitializedLoadout);

            // init weapon
            if (_weaponSlot.Prefab != null)
            {
                _weapon = Instantiate(_weaponSlot.Prefab);
                _weapon.Init(this, _weaponSlot.ShootPoints, _rotationTransform);

                // TODO - process weapon upgrades
            }

            // init module
            if (EntityManager.TryGetModulePrefab(_loadout.ModuleClass, out var modulePrefab))
            {
                _module = Instantiate(modulePrefab);
                _module.Init(this, _moduleProcPoint != null ? _moduleProcPoint : _rotationTransform);

                // TODO - process module upgrades
            }

            // init augments
            InitAugment(_loadout.Augment1Class);
            InitAugment(_loadout.Augment2Class);
            InitAugment(_loadout.Augment3Class);

            // setup visualization for loadout
            _visualization.InitLoadout();
        }

        private void InitAugment(AugmentClass id)
        {
            if (!EntityManager.TryGetAugmentPrefab(id, out var augment))
            {
                return;
            }

            augment.Apply(this);
        }

        private void OnDeathServer(LifeForce.DeathEventData data)
        {
            LocalEvents.Invoke(new ServerPawnKilledData()
            {
                Victim = this,
                Killer = data.Inflictor,
                ProjectileStats = data.ProjectileStats,
                Element = data.Element,
            });
        }

        private void OnDeathBasic()
        {
            Destroy(this.gameObject);
        }

        private void OnEntityImpulseMsg(EntityImpulseMsg msg)
	    {
		    if (identity.entityId != msg.EntityId)
		    {
			    return;
		    }

            AddImpulse(msg.Direction * msg.Magnitude);
	    }

        private void OnPawnRamStateChangeMsg(PawnRamStateChangeMsg msg)
        {
            if (identity.entityId != msg.EntityId)
            {
                return;
            }

            _ramStateTimer = msg.IsInRamState ? float.MaxValue : 0;
        }

        private void OnRamRequestMsg(RamRequestMsg msg)
        {
            if (!Server.IsActive)
            {
                return;
            }

            if (msg.OwnerId != identity.entityId)
            {
                return;
            }

            if (StatusEffectManager.HasStatus(StatusClass.DisableRam))
            {
                return;
            }

            if (!EntityManager.TryGetEntity(msg.TargetId, out var entity))
            {
                Log.Warning(LogChannel.Pawn, $"OnRamRequestMsg - no entity with id: {msg.TargetId}");
                return;
            }

            HandleRam(entity as Pawn, msg.Position, msg.Magnitude);

            Log.Info(LogChannel.Pawn, $"OnRamRequestMsg - success! {msg.TargetId} was hit");
        }

        private void Update()
        {
            _statusEffectManager?.Update();
            _controller?.Update();
            _weapon?.Update();
            _module?.Update();

            HandleStationaryTime();
            HandleTurboHeatDamage();
            HandleRamState();
        }

        private void HandleRamState()
        {
            if (!GameClient.HasAuthority(identity))
            {
                return;
            }

            bool prevRamState = IsInRamState;

            if (TestRamConditions())
            {
                _ramStateTimer += Time.deltaTime;
            }
            else
            {
                _ramStateTimer = 0;
            }

            if (IsInRamState != prevRamState)
            {
                using (var sendMsg = new PawnRamStateChangeMsg()
                {
                    EntityId = identity.entityId,
                    IsInRamState = IsInRamState,
                })
                {
                    ClientSend.Message(sendMsg, ClientSend.Modes.ToOthersIfHost);
                }
            }
        }

        private bool TestRamConditions()
        {
            if (StatusEffectManager.HasStatus(StatusClass.DisableRam))
            {
                return false;
            }

            if (!_input.IsTurboOn)
            {
                return false;
            }

            var vel = _rb.linearVelocity;
            var dot = Vector3.Dot(vel.normalized, _rotationTransform.forward);

            if (dot < RAM_STATE_DOT_THRESHOLD)
            {
                return false;
            }

            float speedPercent = vel.magnitude / _forwardSpeed;

            if (speedPercent < RAM_STATE_SPEED_PERCENT_THRESHOLD)
            {
                return false;
            }

            return true;
        }

        private void HandleStationaryTime()
        {
            if (_tiltVector.magnitude > 0.015f)
            {
                StationaryTimer = 0;
            }
            else
            {
                StationaryTimer += Time.deltaTime;
            }
        }

        private void HandleTurboHeatDamage()
        {
            if (!_input.IsTurboOn)
            {
                return;
            }

            if (Server.IsActive)
            {
                Life.HeatDamage(new HeatDamageInfo(identity, TURBO_HEAT_DAMAGE_DEFAULT * Time.deltaTime, TURBO_HEAT_DAMAGE_COOL_TIME_DEFAULT));
            }
        }

        private void SendState()
        {
            // only authority can send
            if (!GameClient.HasAuthority(identity))
            {
                return;
            }

            // stagger send
            if (GameManager.fixedFrameCount % 2 != 0)
            {
                return;
            }

            // only send changes / valid input / or if last send was a while ago
            // only send if something changed or last send was a while ago
            bool shouldSync = HasMoved() || HasRotated()
                || _lastSendSnapshot.Age >= FORCE_SYNC_INTERVAL
                || _lastSendSnapshot.Input.IsTurboOn != _input.IsTurboOn
                || (CanMove() && _input.MoveDir != default)
                || _input.AimDir != default
                || _lastSendSnapshot.Input.MoveDir != _input.MoveDir
                || _lastSendSnapshot.Input.AimDir != _input.AimDir;

            if (!shouldSync)
            {
                return;
            }

            _lastSendSnapshot.Refresh(_input, new PawnStateData()
            {
                Position = transform.position,
                Rotation = _rotationTransform.rotation,
            });

            // client authoritative state
            using (var sendMsg = new PawnStateMsg(CanMove())
            {
                EntityId = identity.entityId,
                Position = transform.position,
                Heading = JHelper.RotationToHeading(_rotationTransform.rotation),
                MoveDir = _input.MoveDir,
                AimDir = _input.AimDir,
                IsTurboOn = _input.IsTurboOn,
                TurnSpeed = CurrentTurnSpeed,
            })
            {
                ClientSend.Message(sendMsg, ClientSend.Modes.ToOthersIfHost);
            }
        }

        private void FixedUpdate()
        {
            _statusEffectManager?.FixedUpdate();
            _controller?.FixedUpdate();
            _weapon?.FixedUpdate();
            _module?.FixedUpdate();

            if (GameClient.HasAuthority(identity))
            {
                DetectEquipmentUse();
                AuthoritySim();
            }
            else
            {
                ObserverSim();
            }

            SendState();
        }

        private void DetectEquipmentUse()
        {
            if (_statusEffectManager.HasStatus(StatusClass.FirmwareUpgrade))
            {
                return;
            }

            var usedEquipmentThisFrame = EquipmentType.Invalid;

            if (_weapon != null
                && _weapon.IsInUse)
            {
                usedEquipmentThisFrame = _lastUsedEquipment = EquipmentType.Weapon;
            }
            else if (_module != null
                && _module.IsInUse)
            {
                usedEquipmentThisFrame = _lastUsedEquipment = EquipmentType.Module;
            }

            if (usedEquipmentThisFrame != EquipmentType.Invalid)
            {
                _useDifferentEquipmentCountdown = MIN_TIME_BEFORE_USE_DIFFERENT_EQUIPMENT;
            }

            if (_useDifferentEquipmentCountdown <= 0)
            {
                return;
            }

            _useDifferentEquipmentCountdown -= Time.fixedDeltaTime;
        }

        private void AuthoritySim()
        {
            // full simulation
            SimulateInput(out var forwardDot, out var rightDot);

            // animation
            _visualization.SetAnimData(new AnimData(forwardDot, rightDot, CurrentTurnSpeed, _input.IsTurboOn));

            // keep observer state in sync for when authority is removed
            _observerSyncState.Refresh(transform.position, _rotationTransform.rotation);
        }

        private void ObserverSim()
        {
            // extrapolate & interpolate
            SimulateInput(out var forwardDot, out var rightDot);
            _interpolator.Run();

            // animation
            _visualization.SetAnimData(new AnimData(forwardDot, rightDot, CurrentTurnSpeed, _input.IsTurboOn));
        }

        private void SimulateInput(out float forwardDot, out float rightDot)
        {
            HandleTurning();

            CalculateTiltVector(out forwardDot, out rightDot);
            CalculateMoveForce(forwardDot, rightDot, out var force);

            CurrentMovePower = force.magnitude;

            // movement
            if (force.sqrMagnitude > 0)
            {
                _rb.AddForce(force, ForceMode.Force);
            }
        }

        private void HandleTurning()
        {
            // default to forward facing
            Vector3 targetFacingDir = _rotationTransform.forward;

            // prioritise aim dir as the facing if valid
            if (_input.AimDir.sqrMagnitude > 0)
            {
                targetFacingDir = _input.AimDir;
            }
            // otherwise use move vector as facing if valid
            else if (_input.MoveDir.sqrMagnitude > 0)
            {
                targetFacingDir = _input.MoveDir;
            }

            // determine turn angle (always positive)
            float turnAngleDiff = Vector3.Angle(targetFacingDir, _rotationTransform.forward);

            // determine turn direction
            TurnDir turnDir = TurnDir.None;
            if (turnAngleDiff >= 0.3f)
            {
                turnDir = JHelper.AngleDir(targetFacingDir, _rotationTransform.forward, _rotationTransform.up) < 0 ? TurnDir.Right : TurnDir.Left;
            }

            float turnSpeedTarget = 0;
            float decelFactor = 1;

            // determine target turn speed
            if (turnDir != TurnDir.None)
            {
                turnSpeedTarget = turnDir == TurnDir.Right ? _turnSpeed : -_turnSpeed;

                if (turnAngleDiff <= 90)
                {
                    decelFactor = 1 - GameManager.instance.TurnDecelCurve.Evaluate(turnAngleDiff / 90);
                }
            }

            // handle turbo turn speed modifier
            if (_input.IsTurboOn)
            {
                float forwardSpeedFactor = _rb.linearVelocity.magnitude / _forwardSpeed;
                turnSpeedTarget *= 1 - (TURBO_TURN_SPEED_PERCENT_REDUCTION * forwardSpeedFactor);
            }

            if (CurrentTurnSpeed != turnSpeedTarget)
            {
                // calculate step
                float diff = Mathf.Abs(CurrentTurnSpeed - turnSpeedTarget);
                diff = Mathf.Clamp(diff, 0, _turnAccel);

                // determine step direction
                float possibleNewTurnSpeed = turnSpeedTarget > CurrentTurnSpeed
                    ? CurrentTurnSpeed + diff
                    : CurrentTurnSpeed - diff;

                // apply clamped step
                CurrentTurnSpeed = Mathf.Clamp(possibleNewTurnSpeed, -_turnSpeed, _turnSpeed);
            }

            // apply deceleration
            CurrentTurnSpeed *= decelFactor;

            // turn the transform
            _rotationTransform.Rotate(0, CurrentTurnSpeed * Time.fixedDeltaTime, 0);
        }

        private void CalculateTiltVector(out float forwardDot, out float rightDot)
        {
            // calculate tilt vector
            float tiltAccelToUse = _input.IsTurboOn ? _tiltVectorAccel * TURBO_TILT_ACCEL_MODIFIER : _tiltVectorAccel;
            _tiltVector = Vector3.Lerp(_tiltVector, _input.MoveDir, tiltAccelToUse * Time.fixedDeltaTime);

            // movement
            forwardDot = Vector3.Dot(transform.forward, _tiltVector);
            rightDot = Vector3.Dot(transform.right, _tiltVector);
        }

        private void CalculateMoveForce(in float forwardDot, in float rightDot, out Vector3 force)
        {
            float forwardSpeed = _forwardSpeed;
            float reverseSpeed = _reverseSpeed;
            float strafeSpeed = _strafeSpeed;

            if (_input.IsTurboOn)
            {
                forwardSpeed *= TURBO_FORWARD_SPEED_MODIFIER;
                reverseSpeed *= TURBO_STRAFE_SPEED_MODIFIER;
                strafeSpeed *= TURBO_REVERSE_SPEED_MODIFIER;
            }

            // pick accel based on forward or back movement
            float forwardAccel = forwardDot > 0 ? forwardSpeed : reverseSpeed;

            float forward = forwardAccel * Mathf.Abs(forwardDot);
            float right = strafeSpeed * Mathf.Abs(rightDot);

            // strafe speed is a ratio of our forward speed
            right *= 1 - (forward / forwardAccel);

            force = _tiltVector * (forward + right);
        }

        private bool HasMoved()
        {
            return Vector3.Distance(transform.position, _lastSendSnapshot.State.Position) >= 0.01f;
        }

        private bool HasRotated()
        {
            return Quaternion.Angle(_rotationTransform.rotation, _lastSendSnapshot.State.Rotation) >= 0.01f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!GameClient.HasAuthority(identity))
            {
                return;
            }

            if (!IsInRamState)
            {
                return;
            }

            if (Time.time < _nextRamTime)
            {
                return;
            }

            if (StatusEffectManager.HasStatus(StatusClass.DisableRam))
            {
                return;
            }

            var collisionPoint = collision.GetContact(0).point;
            var otherPawn = collision.gameObject.GetComponent<Pawn>();

            if (otherPawn != null)
            {
                _nextRamTime = Time.time + RAM_COOLDOWN;

                // cleaner frontal hits increase damage
                Vector3 dirBetween = JHelper.FlatDirection(Position, collisionPoint);
                float dot = Vector3.Dot(_rotationTransform.forward, dirBetween);

                // generate ram force
                var ramForce = _rb.linearVelocity.magnitude * Mathf.Clamp(dot, 0.1f, 1);

                if (Server.IsActive)
                {
                    HandleRam(otherPawn, transform.position, ramForce);
                }
                else
                {
                    ClientSend.RamRequest(identity.entityId, otherPawn.identity.entityId, transform.position, transform.rotation, ramForce);
                }
            }
        }

        private void HandleRam(Pawn target, Vector3 fromPos, float ramForce)
        {
            if (!Server.IsActive)
            {
                return;
            }

            if (target == null)
            {
                return;
            }

            int damage = (int)(ramForce * 4f);
            int selfDamage = Mathf.Max(1, (int)(damage * 0.5f));

            Log.Info(LogChannel.Pawn, $"HandleRam - eid: {identity.entityId}, mag: {ramForce}, dmg: {damage}, self-dmg: {selfDamage}");

            // damage other
            target.Life.Damage(new DamageInfo(identity, new ElementData(ElementType.Ram, damage)));

            // damage self
            Life.Damage(new DamageInfo(identity, new ElementData(ElementType.Ram, selfDamage)));

            if (target.Life.IsAlive
                && target.CanMove())
            {
                // knockback other
                using (var sendMsg = new EntityImpulseMsg()
                {
                    EntityId = target.identity.entityId,
                    Direction = JHelper.FlatDirection(fromPos, target.Position),
                    Magnitude = damage * 0.3f,
                })
				{
					ServerSend.ToAll(sendMsg);
				}
            }

            //Log.Info(LogChannel.Pawn, $"HandleRam - this: {this.name}, other: {target.name}, force: {ramForce}, other dmg: {damage}, self dmg: {selfDamage}");
        }

        private void OnDrawGizmos()
        {
            /*
            if (!Application.isPlaying)
            {
                return;
            }

            Gizmos.color = Vector3.Distance(transform.position, _observerSyncState.Position) <= 0.2f ? Color.green : Color.red;
            Gizmos.DrawSphere(transform.position, 0.65f);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_observerSyncState.Position, 0.55f);
            */
        }
    }
}
