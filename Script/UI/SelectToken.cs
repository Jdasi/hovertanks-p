using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class SelectToken : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public enum Interactions
        {
            Enter,
            Exit,
            Placed,
            Collected
        }

	    public bool IsPlaced => _currentPlaceId >= 0;
        public Action<Interactions> OnInteraction;

	    [SerializeField] Image _img;

        private int _currentPlaceId;

	    public void Place(int id)
	    {
		    if (IsPlaced)
		    {
			    return;
		    }

		    _currentPlaceId = id;
		    _img.raycastTarget = true;

            SendInteraction(Interactions.Placed);
	    }

	    public void Collect()
	    {
		    if (!IsPlaced)
		    {
			    return;
		    }

		    _currentPlaceId = -1;
		    _img.raycastTarget = false;

            SendInteraction(Interactions.Collected);
	    }

	    public void OnPointerEnter(PointerEventData eventData)
        {
            SendInteraction(Interactions.Enter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SendInteraction(Interactions.Exit);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPlaced)
            {
                Collect();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnPointerDown(eventData);
        }

        private void SendInteraction(Interactions interaction)
        {
            OnInteraction?.Invoke(interaction);
        }
    }
}
