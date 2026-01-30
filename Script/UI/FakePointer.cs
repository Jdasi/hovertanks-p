using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HoverTanks.UI
{
    public class FakePointer : PointerInputModule
    {
        public PlayerId PlayerId { get; private set; }

        private PointerEventData _eventData;
        private GameObject _currentLookAt;
        private RaycastResult _currentRaycast;

        public void Init(PlayerId playerId)
        {
            PlayerId = playerId;
        }

        public override void Process()
        {
            if (_eventData == null)
            {
                _eventData = new PointerEventData(eventSystem);
            }

            Vector2 prevPos = _eventData.position;
            _eventData.position = transform.position;
            _eventData.delta = prevPos - _eventData.position;

            HandleRaycast();
        }

        public void Click()
        {
            if (_eventData.pointerEnter != null)
            {
                GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_eventData.pointerEnter);

                if (_currentLookAt != handler)
                {
                    _currentLookAt = handler;
                }

                if (_currentLookAt != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_currentLookAt, _eventData, ExecuteEvents.pointerClickHandler);
                }
            }
            else
            {
                _currentLookAt = null;
            }
        }

        private void HandleRaycast()
        {
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(_eventData, raycastResults);
            _currentRaycast = _eventData.pointerCurrentRaycast = FindFirstRaycast(raycastResults);

            ProcessMove(_eventData);
        }
    }
}
