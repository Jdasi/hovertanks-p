using HoverTanks.UI;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeBayStorageItemSlot : UIButton
{
	public ShopInfo ShopInfo { get; private set; }

	[SerializeField] Image _imgIcon;

	public void SetShopInfo(ShopInfo shopInfo)
	{
		ShopInfo = shopInfo;
	}
}
