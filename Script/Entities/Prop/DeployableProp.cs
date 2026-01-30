using UnityEngine;

namespace HoverTanks.Entities
{
    public class DeployableProp : Prop
    {
        private Transform _surfaceTransform;
		private Vector3 _offset;

        protected override void Awake()
        {
            base.Awake();

            if (!JHelper.TryGetFloorAtPos(transform.position, out _surfaceTransform))
            {
                Log.Error(LogChannel.DeployableProp, $"[{name}] Awake - TestPosition failed, was the position tested before deploying?");
                return;
            }

			_offset = _surfaceTransform.position - transform.position;
        }

        private void FixedUpdate()
        {
            // fake position parenting
			transform.position = _surfaceTransform.position - _offset;

            OnFixedUpdate();
        }

        protected virtual void OnFixedUpdate() { }
    }
}
