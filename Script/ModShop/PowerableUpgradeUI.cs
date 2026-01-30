using HoverTanks.UI;
using UnityEngine;
using UnityEngine.UI;

public class PowerableUpgradeUI : MonoBehaviour
{
	public bool IsUpgradePowered { get; private set; }
	public IClassName Class { get; private set; }

	[SerializeField] Image _imgPowered;
	[SerializeField] UIButton _btn;

	public void Init(IClassName className, bool powered)
	{
		Class = className;
		SetPowered(powered);

		_btn.text = className.Name;
	}

	public void SetPowered(bool powered)
	{
		IsUpgradePowered = powered;
		_imgPowered.enabled = powered;
	}
}
