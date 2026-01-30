using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoverTanks.UI
{
	public abstract class UISelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public bool interactable
		{
			get => _interactable && enabled && gameObject.activeSelf;
			set
			{
				if (value == _interactable)
				{
					return;
				}

				_interactable = value;

				if (_interactable)
				{
					OnInteractEnabledBase();
				}
				else
				{
					Deselect();
					OnInteractDisabledBase();
				}
			}
		}

		[SerializeField] bool _interactable = true;

		[Header("Scale")]
		[SerializeField] Transform _scaleTransform;
		[SerializeField] float _highlightScale;

		[Header("Image Highlight")]
		[SerializeField] Image _imgHighlight;
		[SerializeField] Color _imgNormalColor = Color.white;
		[SerializeField] Color _imgHighlightColor = Color.white;
		[SerializeField] Color _imgDisabledColor = Color.grey;

		public UnityEvent OnSelect;

		protected ISelectableOwner _owner;

		private Vector3 _startScale;

		public void SetOwner(ISelectableOwner owner)
		{
			if (_owner != null)
			{
				_owner.UnregisterSelectable(this);
			}

			_owner = owner;

			if (_owner == null)
			{
				Deselect();
			}
			else
			{
				_owner.RegisterSelectable(this);
			}
		}

		public void ClearOwner()
		{
			SetOwner(null);
		}

		public void OnPointerEnter(PointerEventData eventData)
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

			if (!GameManager.IsUsingController)
			{
				Select();
			}
		}

		public void OnPointerExit(PointerEventData eventData)
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

			if (!GameManager.IsUsingController)
			{
				Deselect();
			}
		}

		public void Select()
		{
			if (!interactable)
			{
				return;
			}

			if (_owner == null
				|| _owner.PlayerId == PlayerId.Invalid)
			{
				return;
			}

			_owner.SetCurrent(this);

			OnSelectedBase();
			OnSelect?.Invoke();
		}

		public void Deselect()
		{
			if (!interactable)
			{
				return;
			}

			if (_owner == null
				|| _owner.PlayerId == PlayerId.Invalid)
			{
				return;
			}

			_owner.SetCurrent(null, this);
			OnDeselectedBase();
		}

		protected virtual void OnSelected() { }
		protected virtual void OnDeselected() { }
		protected virtual void OnInteractEnabled() { }
		protected virtual void OnInteractDisabled() { }

		private void Start()
		{
			if (_scaleTransform == null)
			{
				_scaleTransform = this.transform;
			}

			_startScale = _scaleTransform.localScale;
		}

		private void OnDisable()
		{
			Deselect();
		}

		private void OnSelectedBase()
		{
			if (_highlightScale != 0)
			{
				_scaleTransform.localScale = Vector3.one * _highlightScale;
			}

			if (_imgHighlight != null)
			{
				_imgHighlight.color = _imgHighlightColor;
			}

			OnSelected();
		}

		private void OnDeselectedBase()
		{
			if (_highlightScale != 0
				&& _scaleTransform != null)
			{
				_scaleTransform.localScale = _startScale;
			}

			if (_imgHighlight != null)
			{
				_imgHighlight.color = _imgNormalColor;
			}

			OnDeselected();
		}

		private void OnInteractEnabledBase()
		{
			if (_imgHighlight != null)
			{
				_imgHighlight.color = _imgNormalColor;
			}

			OnInteractEnabled();
		}

		private void OnInteractDisabledBase()
		{
			if (_imgHighlight != null)
			{
				_imgHighlight.color = _imgDisabledColor;
			}

			OnInteractDisabled();
		}
	}
}
