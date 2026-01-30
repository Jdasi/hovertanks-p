using UnityEngine;

namespace HoverTanks.GameState
{
    public abstract class GameState : MonoBehaviour
    {
        [Header("Quality")]
        public bool _isCloseUpScene;

        public void TriggerState()
        {
            if (this.enabled)
            {
                Log.Warning(LogChannel.GameState, $"[GameState] TriggerState called when {this} was already enabled!");
                return;
            }

            if (_isCloseUpScene)
            {
                QualitySettings.shadowCascades = 4;
            }
            else
            {
                QualitySettings.shadowCascades = 0;
            }

            this.enabled = true;
            OnStateEnter();
        }

        protected abstract void OnStateEnter();
    }
}
