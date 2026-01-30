using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;

namespace HoverTanks.StatusEffects
{
    public class StatusEffectManager : IStatusEffectManager
    {
        private enum TickMode
        {
            Update,
            FixedUpdate,
        }

        private enum DictType
        {
            Local,
            Server,
        }

        private readonly struct RegisterResult
        {
            internal enum Types
            {
                Error,
                Refreshed,
                Created,
                Immune,
            }

            internal readonly Types Type;
            internal readonly int Handle;

            public RegisterResult(Types type, int handle)
            {
                Type = type;
                Handle = handle;
            }

            public static readonly RegisterResult Error = new RegisterResult(Types.Error, -1);
            public static readonly RegisterResult Immune = new RegisterResult(Types.Immune, -1);
        }

        public const int INVALID_HANDLE = -1;

        private readonly IStatusEffectTarget _target;
        private readonly Dictionary<int, StatusEffect> _localStatuses;
        private readonly Dictionary<int, StatusEffect> _serverStatuses;
        private List<StatusClass> _statusImmunities;

        private int _nextLocalStatusHandle;
        private int _nextServerStatusHandle;

        public StatusEffectManager(IStatusEffectTarget target)
        {
            _target = target;
            _localStatuses = new Dictionary<int, StatusEffect>();
            _serverStatuses = new Dictionary<int, StatusEffect>();

            NetworkEvents.Subscribe<AddStatusEffectMsg>(OnAddStatusEffectMsg);
            NetworkEvents.Subscribe<RemoveStatusEffectMsg>(OnRemoveStatusEffectMsg);
        }

        public void Cleanup()
        {
            NetworkEvents.Unsubscribe<AddStatusEffectMsg>(OnAddStatusEffectMsg);
            NetworkEvents.Unsubscribe<RemoveStatusEffectMsg>(OnRemoveStatusEffectMsg);
        }

        public bool HasStatus(StatusClass @class)
        {
            foreach (var status in _localStatuses.Values)
            {
                if (status.StatusClass == @class)
                {
                    return true;
                }
            }

            foreach (var status in _serverStatuses.Values)
            {
                if (status.StatusClass == @class)
                {
                    return true;
                }
            }

            return false;
        }

        public bool LocalAdd(StatusClass @class, out int handle, float duration = 0)
        {
            var result = RegisterStatus(_localStatuses, @class, _nextLocalStatusHandle, duration);
            handle = INVALID_HANDLE;

            switch (result.Type)
            {
                case RegisterResult.Types.Created: ++_nextLocalStatusHandle; break;
                case RegisterResult.Types.Error: return false;
            }

            handle = result.Handle;
            return true;
        }

        public bool LocalAdd(StatusClass @class, float duration = 0)
        {
            return LocalAdd(@class, out _, duration);
        }

        public void LocalRemove(ref int handle)
        {
            UnregisterStatus(_localStatuses, handle);
            handle = INVALID_HANDLE;
        }

        public bool Add(StatusClass @class, out int handle, float duration = 0)
        {
            handle = INVALID_HANDLE;

            if (!Server.IsActive)
            {
                return false;
            }

            var result = RegisterStatus(_serverStatuses, @class, _nextServerStatusHandle, duration);

            switch (result.Type)
            {
                case RegisterResult.Types.Created: ++_nextServerStatusHandle; break;
                case RegisterResult.Types.Error: return false;
            }

            using (var sendMsg = new AddStatusEffectMsg()
            {
                EntityId = _target.identity.entityId,
                Class = @class,
                Handle = result.Handle,
                Duration = duration,
            })
            {
                ClientSend.Message(sendMsg, ClientSend.Modes.ToOthersIfHost);
            }

            handle = result.Handle;
            return true;
        }

        public bool Add(StatusClass @class, float duration = 0)
        {
            return Add(@class, out _, duration);
        }

        public void Remove(ref int handle)
        {
            if (!Server.IsActive)
            {
                return;
            }

            if (!UnregisterStatus(_serverStatuses, handle))
            {
                return;
            }

            using (var sendMsg = new RemoveStatusEffectMsg()
            {
                EntityId = _target.identity.entityId,
                Handle = handle,
            })
            {
                ClientSend.Message(sendMsg, ClientSend.Modes.ToOthersIfHost);
            }

            handle = INVALID_HANDLE;
        }

        public void LocalAddImmunity(StatusClass @class)
        {
            if (_statusImmunities == null)
            {
                _statusImmunities = new List<StatusClass>() { @class };
            }
            else if (!_statusImmunities.Contains(@class))
            {
                _statusImmunities.Add(@class);
            }
        }

        public void Update()
        {
            TickStatuses(_localStatuses, TickMode.Update, DictType.Local);
            TickStatuses(_serverStatuses, TickMode.Update, DictType.Server);
        }

        public void FixedUpdate()
        {
            TickStatuses(_localStatuses, TickMode.FixedUpdate, DictType.Local);
            TickStatuses(_serverStatuses, TickMode.FixedUpdate, DictType.Server);
        }

        private void TickStatuses(Dictionary<int, StatusEffect> statuses, TickMode mode, DictType type)
        {
            if (statuses.Count == 0)
            {
                return;
            }

            List<int> expiredStatuses = null;
            bool canRemove = type == DictType.Local
                || (type == DictType.Server && Server.IsActive);

            foreach (var elem in statuses)
            {
                // skip expired statuses
                if (canRemove
                    && elem.Value.HasExpired)
                {
                    if (expiredStatuses == null)
                    {
                        expiredStatuses = new List<int>();
                    }

                    expiredStatuses.Add(elem.Key);

                    continue;
                }

                switch (mode)
                {
                    case TickMode.Update: elem.Value.Update(); break;
                    case TickMode.FixedUpdate: elem.Value.FixedUpdate(); break;
                }
            }

            // remove expired statuses
            if (expiredStatuses != null)
            {
                for (int i = 0; i < expiredStatuses.Count; ++i)
                {
                    int id = expiredStatuses[i];

                    switch (type)
                    {
                        case DictType.Local: LocalRemove(ref id); break;
                        case DictType.Server: Remove(ref id); break;
                    }
                }
            }
        }

        private void OnAddStatusEffectMsg(AddStatusEffectMsg msg)
        {
            if (_target.identity.entityId != msg.EntityId)
            {
                return;
            }

            RegisterStatus(_serverStatuses, msg.Class, msg.Handle, msg.Duration);
        }

        private void OnRemoveStatusEffectMsg(RemoveStatusEffectMsg msg)
        {
            if (_target.identity.entityId != msg.EntityId)
            {
                return;
            }

            UnregisterStatus(_serverStatuses, msg.Handle);
        }

        private RegisterResult RegisterStatus(Dictionary<int, StatusEffect> dict, StatusClass @class, int handle, float duration)
        {
            if (_statusImmunities != null
                && _statusImmunities.Contains(@class))
            {
                return RegisterResult.Immune;
            }

            if (dict.ContainsKey(handle))
            {
                Log.Error(LogChannel.StatusEffectManager, $"RegisterStatus - already contained a status with handle({handle}), is incrementation working?");
                return RegisterResult.Error;
            }

            // try refresh existing status
            foreach (var existingStatus in dict)
            {
                if (existingStatus.Value.StatusClass != @class)
                {
                    continue;
                }

                //Log.Info(LogChannel.StatusEffectManager, $"RegisterStatus - refreshed existing status({@class}) on entity: {_entity}, handle({existingStatus.Key})");

                existingStatus.Value.Refresh(duration);
                return new RegisterResult(RegisterResult.Types.Refreshed, existingStatus.Key);
            }

            // create new status
            if (!StatusEffectFactory.TryCreate(@class, out var status))
            {
                return RegisterResult.Error;
            }

            // try init for entity
            if (!status.TryInitialize(_target, duration))
            {
                return RegisterResult.Error;
            }

            //Log.Info(LogChannel.StatusEffectManager, $"RegisterStatus - initialized new status({@class}) on entity: {_entity}, handle({handle})");

            status.Start();

            dict.Add(handle, status);
            return new RegisterResult(RegisterResult.Types.Created, handle);
        }

        private bool UnregisterStatus(Dictionary<int, StatusEffect> dict, int id)
        {
            if (!dict.TryGetValue(id, out var status))
            {
                return false;
            }

            //Log.Info(LogChannel.StatusEffectManager, $"UnregisterStatus - removed status({status.Class}) on entity: {_entity}, handle({id})");

            status.Stop();

            dict.Remove(id);
            return true;
        }
    }
}
