using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
	public class ImgChargeItem : MonoBehaviour
	{
		[SerializeField] Image _imgFill;

		public void SetVisible(bool visible)
		{
			if (this.gameObject.activeSelf == visible)
			{
				return;
			}

			this.gameObject.SetActive(visible);
		}

		public void SetFill(bool fill)
		{
			if (_imgFill.enabled == fill)
			{
				return;
			}

			_imgFill.enabled = fill;
		}
	}
}
