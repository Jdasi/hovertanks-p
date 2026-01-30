using HoverTanks.Events;
using HoverTanks.Networking;
using System;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    public interface IEquipmentInfo
    {
        bool HasCharges { get; }
        int Charges { get; }
        int MaxCharges { get; }
        bool IsRecharging { get; }
        bool IsSpooling { get; }
        bool IsSpoolingUp { get; }
        bool IsSpoolingDown { get; }
        bool IsActive { get; }
        float SpoolUpTime { get; }
        float SpoolDownTime { get; }
        float SpoolTimer { get; }
        Action Triggered { get; set; }
        Action Deactivated { get; set; }
        Action SpoolUpStarted { get; set; }
        Action SpoolDownStarted { get; set; }
        Equipment.ChargeCountChanged OnChargeCountChanged { get; set; }
    }

    public abstract partial class Equipment : ScriptableObject, IEquipmentInfo
    {
        [Flags]
        private enum Flags
        {
            UseAction = Bits.Bit0,
            RechargeAction = Bits.Bit1,
        }

        [Flags]
        private enum DisabledActionMessageFlags { }

        protected enum States
        {
            Idle,
            SpoolingUp,
            Active,
            SpoolingDown,
        }

        public int Charges
        {
            get => _charges;
            protected set
            {
                if (_charges == value)
                {
                    return;
                }

                _charges = value;
                OnChargeCountChanged?.Invoke(value);
            }
        }

        public EquipmentType EquipmentType { get; private set; }
        public IEquipmentOwner Owner { get; private set; }
        public bool HasCharges => Charges > 0;
        public int MaxCharges => _maxCharges;
        public bool IsRecharging { get; protected set; }
        public bool IsSpooling => IsSpoolingUp || IsSpoolingDown;
        public bool IsSpoolingUp => State == States.SpoolingUp;
        public bool IsSpoolingDown => State == States.SpoolingDown;
        public bool IsActive => State == States.Active;
        public bool IsInUse => IsActive || IsSpoolingUp;
        public float SpoolUpTime => _spoolUpTime;
        public float SpoolDownTime => _spoolDownTime;
        public float SpoolTimer => _spoolTimer;
        public Action Triggered { get; set; }
        public Action Deactivated { get; set; }
        public Action SpoolUpStarted { get; set; }
        public Action SpoolDownStarted { get; set; }
        public Action Destroyed { get; set; }

        public delegate void ChargeCountChanged(int newCount);
        public ChargeCountChanged OnChargeCountChanged { get; set; }

        protected EffectsSource Source { get; private set; }
        protected Transform CurrentShootPoint { get; set; }
        protected States State { get; private set; }
        protected Vector3 TargetPos { get; set; }
        protected EntityId TargetEntity { get; set; }

        [Header("Charges")]
        [SerializeField] int _maxCharges;
        [SerializeField] protected float _delayBetweenProcs;

        [Header("Base Effects")]
        [SerializeField] protected float _screenShake;

        [Header("Spooling")]
        [SerializeField] float _spoolUpTime;
        [SerializeField] EffectAudioSettings _spoolUpAudio;
        [SerializeField] GameObject _spoolUpEffectPrefab;
        [SerializeField] bool _forceActivateAfterSpool;

        [Space]
        [SerializeField] float _spoolDownTime;
        [SerializeField] EffectAudioSettings _spoolDownAudio;
        [SerializeField] GameObject _spoolDownEffectPrefab;

        [Header("Modding")]
        [SerializeField] EquipmentMod _modSlot1;
        [SerializeField] EquipmentMod _modSlot2;
        [SerializeField] EquipmentMod _modSlot3;

        protected float _nextProcTime;
        protected float _customNextSpoolUpTime;
        private Bitset _flags;
        private Bitset _disabledActionMessageFlags;
        private float _spoolTimer;

        /// <summary>
        /// Don't set directly. Use <see cref="Charges"/> instead.
        /// </summary>
        private int _charges;

        protected void Init(IEquipmentOwner owner, EquipmentType type)
        {
            Owner = owner;
            Source = AudioManager.CreateEffectsSource();

#if DEBUG
            Source.name = $"[EffectsSource] Equipment: {name} (owner: {owner.Life.name})";
#endif

            EquipmentType = type;
            ProcessMods(owner.GetEnabledModsForEquipment(type));

            NetworkEvents.Subscribe<PawnEquipmentActionMsg>(OnPawnEquipmentStateMsg);
        }

        private void OnDestroy()
        {
            if (State == States.Active)
            {
                SetState(States.Idle);
            }

            if (Source != null)
            {
                Destroy(Source.gameObject, 3);
            }

            Destroyed?.Invoke();
            OnCleanup();

            NetworkEvents.Unsubscribe<PawnEquipmentActionMsg>(OnPawnEquipmentStateMsg);
        }

        protected void SetDisabledActionMessages(PawnEquipmentActionMsg.Actions[] disabledActions)
        {
            if (disabledActions == null)
            {
                return;
            }

            _disabledActionMessageFlags.Reset();

            foreach (var state in disabledActions)
            {
                _disabledActionMessageFlags.Set(1 << (int)state);
            }
        }

        public void Use(Vector3 targetPos = default)
        {
            if (!GameClient.HasAuthority(Owner.identity))
            {
                return;
            }

            TargetPos = targetPos;

            if (_flags.IsSet((int)Flags.UseAction))
            {
                return;
            }

            _flags.Set((int)Flags.UseAction);
        }

        public void StopUse()
        {
            if (!GameClient.HasAuthority(Owner.identity))
            {
                return;
            }

            if (!_flags.IsSet((int)Flags.UseAction))
            {
                return;
            }

            _flags.Unset((int)Flags.UseAction);
        }

        public void Recharge()
        {
            if (!GameClient.HasAuthority(Owner.identity))
            {
                return;
            }

            if (Charges == MaxCharges)
            {
                return;
            }

            if (IsRecharging)
            {
                return;
            }

            if (IsSpoolingUp)
            {
                return;
            }

            if (_flags.IsSet((int)Flags.RechargeAction))
            {
                return;
            }

            _flags.Set((int)Flags.RechargeAction);

            SendActionMessage(PawnEquipmentActionMsg.Actions.RechargeStart);
            OnRechargeRequestStart();
        }

        public void StopRecharge()
        {
            if (!GameClient.HasAuthority(Owner.identity))
            {
                return;
            }

            if (!_flags.IsSet((int)Flags.RechargeAction))
            {
                return;
            }

            _flags.Unset((int)Flags.RechargeAction);

            SendActionMessage(PawnEquipmentActionMsg.Actions.RechargeStop);
            OnRechargeRequestStop();
        }

        public void Update()
        {
            if (GameClient.HasAuthority(Owner.identity))
            {
                switch (State)
                {
                    case States.Idle: IdleOnUpdate(); break;
                    case States.SpoolingUp: SpoolingUpOnUpdate(); break;
                    case States.Active: ActiveOnUpdate(); break;
                    case States.SpoolingDown: SpoolingDownOnUpdate(); break;
                }

                OnUpdateLocal();
            }

            switch (State)
            {
                case States.Active: OnActiveUpdate(); break;
            }
        }

        public void FixedUpdate()
        {
            switch (State)
            {
                case States.Active: OnActiveFixedUpdate(); break;
            }
        }

        protected void SendActionMessage(PawnEquipmentActionMsg.Actions action, Vector3 targetPos = default, EntityId targetEntity = EntityId.Invalid)
        {
            if (!GameClient.HasAuthority(Owner.identity))
            {
                return;
            }

            // skip disabled actions
            if (_disabledActionMessageFlags.IsSet(1 << (int)action))
            {
                return;
            }

            using (var sendMsg = new PawnEquipmentActionMsg()
            {
                EntityId = Owner.identity.entityId,
                EquipmentType = EquipmentType,
                Action = action,
                TargetPos = targetPos,
                TargetId = targetEntity,
            })
            {
                ClientSend.Message(sendMsg, ClientSend.Modes.ToOthersIfHost);
            }
        }

        protected bool TestNextProc()
        {
            if (!HasCharges)
            {
                return false;
            }

            if (Time.time < _nextProcTime)
            {
                return false;
            }

            _nextProcTime = Time.time + _delayBetweenProcs;

            return true;
        }

        // methods that only the authoritative client will run
        protected virtual void OnUpdateLocal() { }
        protected virtual void OnActivateLocal() { }
        protected virtual void OnActiveUpdateLocal() { }

        // methods that all clients run
        protected virtual void OnInit() { }
        protected virtual void OnCleanup() { }
        protected virtual void OnActivate() { }
        protected virtual void OnActiveUpdate() { }
        protected virtual void OnActiveFixedUpdate() { }
        protected virtual void OnProcEffect() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnRechargeRequestStart() { }
        protected virtual void OnRechargeRequestStop() { }
        protected virtual void OnStateMsg(PawnEquipmentActionMsg.Actions state) { }

        private void IdleOnUpdate()
        {
            if (!_flags.IsSet((int)Flags.UseAction))
            {
                return;
            }

            if (IsRecharging)
            {
                return;
            }

            if (!HasCharges)
            {
                return;
            }

            if (_spoolUpTime > 0)
            {
                if (Time.time >= _customNextSpoolUpTime)
                {
                    SetState(States.SpoolingUp);
                }
            }
            else
            {
                SetState(States.Active);
            }
        }

        private void SpoolingUpOnUpdate()
        {
            _spoolTimer += Time.deltaTime;

            if (_spoolTimer < _spoolUpTime)
            {
                return;
            }

            if (_flags.IsSet((int)Flags.UseAction)
                || _forceActivateAfterSpool)
            {
                SetState(States.Active);
            }
            else
            {
                if (_spoolDownTime > 0)
                {
                    SetState(States.SpoolingDown);
                }
                else
                {
                    SetState(States.Idle);
                }
            }
        }

        private void ActiveOnUpdate()
        {
            OnActiveUpdateLocal();

            if (!_flags.IsSet((int)Flags.UseAction)
                || IsRecharging
                || !HasCharges)
            {
                if (_spoolDownTime > 0)
                {
                    SetState(States.SpoolingDown);
                }
                else
                {
                    SetState(States.Idle);
                }
            }
        }

        private void SpoolingDownOnUpdate()
        {
            _spoolTimer += Time.deltaTime;

            if (_spoolTimer < _spoolDownTime)
            {
                return;
            }

            SetState(States.Idle);
        }

        private void CreateSpoolEffect(States state)
        {
            if (state != States.SpoolingUp
                && state != States.SpoolingDown)
            {
                return;
            }

            EffectAudioSettings audio;
            GameObject prefab;
            float time;

            switch (state)
            {
                case States.SpoolingUp:
                {
                    audio = _spoolUpAudio;
                    prefab = _spoolUpEffectPrefab;
                    time = _spoolUpTime;
                } break;

                case States.SpoolingDown:
                {
                    audio = _spoolDownAudio;
                    prefab = _spoolDownEffectPrefab;
                    time = _spoolDownTime;
                } break;

                default: return;
            }

            if (CurrentShootPoint == null)
            {
                Log.Error(LogChannel.Equipment, $"CreateSpoolEffect - SpoolPoint was null");
                return;
            }

            AudioManager.PlayClipAtPoint(audio, CurrentShootPoint.position);

            if (prefab == null)
            {
                return;
            }

            var obj = Instantiate(prefab, CurrentShootPoint.position, CurrentShootPoint.rotation);
            obj.transform.SetParent(CurrentShootPoint);
            Destroy(obj, time + 1);
        }

        private void SetState(States state)
        {
            if (State == state)
            {
                return;
            }

            _spoolTimer = 0;

            // exits
            switch (State)
            {
                case States.Active:
                {
                    SendActionMessage(PawnEquipmentActionMsg.Actions.OnDeactivate);
                    OnDeactivate();

                    Deactivated?.Invoke();
                } break;
            }

            State = state;

            // enters
            switch (State)
            {
                case States.Active:
                {
                    SendActionMessage(PawnEquipmentActionMsg.Actions.OnActivate);
                    OnActivateLocal();
                    OnActivate();
                } break;

                case States.SpoolingUp:
                {
                    SendActionMessage(PawnEquipmentActionMsg.Actions.SpoolUp);
                    CreateSpoolEffect(States.SpoolingUp);

                    SpoolUpStarted?.Invoke();
                } break;

                case States.SpoolingDown:
                {
                    SendActionMessage(PawnEquipmentActionMsg.Actions.SpoolDown);
                    CreateSpoolEffect(States.SpoolingDown);

                    SpoolDownStarted?.Invoke();
                } break;
            }
        }

        private void SetStateObserver(States state)
        {
            State = state;

            _spoolTimer = 0;

            switch (State)
            {
                case States.Active:
                {
                    OnActivate();
                } break;

                case States.Idle:
                {
                    OnDeactivate();
                    Deactivated?.Invoke();
                } break;

                case States.SpoolingUp:
                {
                    CreateSpoolEffect(States.SpoolingUp);
                    SpoolUpStarted?.Invoke();
                } break;

                case States.SpoolingDown:
                {
                    CreateSpoolEffect(States.SpoolingDown);
                    SpoolDownStarted?.Invoke();
                } break;
            }
        }

        private void OnPawnEquipmentStateMsg(PawnEquipmentActionMsg msg)
        {
            // skip if not for us
            if (Owner.identity.entityId != msg.EntityId)
            {
                return;
            }

            // skip if for other equipment
            if (EquipmentType != msg.EquipmentType)
            {
                return;
            }

            if (Server.IsActive)
            {
                // forward to other clients
                ServerSend.ToAllExcept(msg, GameClient.ClientId, Owner.identity.clientId);
            }

            switch (msg.Action)
            {
                case PawnEquipmentActionMsg.Actions.OnActivate: SetStateObserver(States.Active); break;
                case PawnEquipmentActionMsg.Actions.OnDeactivate: SetStateObserver(States.Idle); break;
                case PawnEquipmentActionMsg.Actions.SpoolUp: SetStateObserver(States.SpoolingUp); break;
                case PawnEquipmentActionMsg.Actions.SpoolDown: SetStateObserver(States.SpoolingDown); break;
                case PawnEquipmentActionMsg.Actions.RechargeStart: OnRechargeRequestStart(); break;
                case PawnEquipmentActionMsg.Actions.RechargeStop: OnRechargeRequestStop(); break;

                case PawnEquipmentActionMsg.Actions.Proc:
                {
                    TargetPos = msg.TargetPos;
                    TargetEntity = msg.TargetId;

                    OnProcEffect();
                    Triggered?.Invoke();
                } break;
            }

            OnStateMsg(msg.Action);
        }

        private void ProcessMods(Slots enabledMods)
        {
            if (enabledMods.HasFlag(Slots.Slot1))
            {
                _modSlot1?.Apply(this);
            }

            if (enabledMods.HasFlag(Slots.Slot2))
            {
                _modSlot2?.Apply(this);
            }

            if (enabledMods.HasFlag(Slots.Slot3))
            {
                _modSlot3?.Apply(this);
            }

            // clear redundant refs
            _modSlot1 = null;
            _modSlot2 = null;
            _modSlot3 = null;
        }
    }
}
