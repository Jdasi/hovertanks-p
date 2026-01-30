using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class UIButton : UISelectable, IPointerClickHandler
    {
        [Header("Text Highlight")]
        [SerializeField] Text _txtHighlight;
        [SerializeField] Color _txtNormalColor = Color.white;
        [SerializeField] Color _txtHighlightColor = Color.white;

        public UnityEvent OnClick;

        public string text
        {
            get
            {
                if (_txtHighlight == null)
                {
                    return "";
                }

                return _txtHighlight.text;
            }

            set
            {
                if (_txtHighlight == null)
                {
                    return;
                }

                _txtHighlight.text = value;
            }
        }

        protected override void OnSelected()
        {
            if (_txtHighlight != null)
            {
                _txtHighlight.color = _txtHighlightColor;
            }
        }

        protected override void OnDeselected()
        {
            if (_txtHighlight != null)
            {
                _txtHighlight.color = _txtNormalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
		    {
			    return;
		    }

            if (_owner == null
			    || _owner.PlayerId != PlayerId.One)
		    {
			    return;
		    }

            OnClick?.Invoke();
        }
    }
}
