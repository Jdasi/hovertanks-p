using HoverTanks.Events;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
    public abstract class PawnController : ScriptableObject
    {
        public bool isPlayer => _isPlayer;

        [SerializeField] bool _isPlayer;

        protected IPawnControl Pawn { get; private set; }
        protected PlayerId PlayerId => Pawn?.identity.playerId ?? PlayerId.Invalid;

        private bool _enabled;

        public void Init(IPawnControl pawn)
        {
            Pawn = pawn;
            SmartSetEnabled();

            LocalEvents.Subscribe<PauseMenuToggledData>(OnPauseMenuToggled);

            OnInit();
        }

        protected virtual void OnDestroy()
        {
            LocalEvents.Unsubscribe<PauseMenuToggledData>(OnPauseMenuToggled);
        }

        public void Update()
        {
            if (!_enabled)
            {
                return;
            }

            OnUpdate();
        }

        public void FixedUpdate()
        {
            if (!_enabled)
            {
                return;
            }

            OnFixedUpdate();
        }

        protected virtual void OnInit() { }
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFixedUpdate() { }

        private void SetEnabled(bool enabled)
        {
            if (_enabled == enabled)
            {
                return;
            }

            _enabled = enabled;

            if (_enabled)
            {
                OnEnabled();
            }
            else
            {
                Pawn?.ClearAllInput();
                OnDisabled();
            }
        }

        private void OnPauseMenuToggled(PauseMenuToggledData _)
        {
            SmartSetEnabled();
        }

        private void SmartSetEnabled()
        {
            if (isPlayer)
            {
                SetEnabled(!Game.isPauseMenuOpen);
            }
            else
            {
                SetEnabled(Time.timeScale > 0);
            }
        }

#if DEBUG
        public virtual void OnDrawGizmos() { }
    #endif
    }
}
