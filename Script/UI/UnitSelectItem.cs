using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace HoverTanks.UI
{
    public class UnitSelectItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum Interactions
        {
            Enter,
            Exit,
            MouseDown,
            MouseUp,
            ControllerClick,
        }

        public Action<int, Interactions> OnInteraction;

        [SerializeField] RawImage _img;
        [SerializeField] Color _colourHighlight;
        [SerializeField] Color _colourDefault;
        [SerializeField] float _highlightScaleMod;

        private int _id;
        private bool _isHovered;
        private bool _isSelected;

        public void Init(int id, Texture texture)
        {
            _id = id;
            _img.color = _colourDefault;
            _img.texture = texture;
            _img.enabled = true;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateGraphic();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SendInteraction(Interactions.Enter);

            _isHovered = true;
            UpdateGraphic();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SendInteraction(Interactions.Exit);

            _isHovered = false;
            UpdateGraphic();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left: SendInteraction(Interactions.MouseDown); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left: SendInteraction(Interactions.MouseUp); break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameManager.IsUsingController)
            {
                SendInteraction(Interactions.ControllerClick);
            }
        }

        private void SendInteraction(Interactions interaction)
        {
            OnInteraction?.Invoke(_id, interaction);
        }

        private void UpdateGraphic()
        {
            if (_isHovered || _isSelected)
            {
                _img.color = _colourHighlight;
                _img.transform.localScale = Vector3.one * _highlightScaleMod;
            }
            else
            {
                _img.color = _colourDefault;
                _img.transform.localScale = Vector3.one;
            }
        }
    }
}
