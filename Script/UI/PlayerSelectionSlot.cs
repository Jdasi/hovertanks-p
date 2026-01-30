using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
	public class PlayerSelectionSlot : MonoBehaviour
	{
		[Serializable]
		private class LerpingBar
		{
			public bool isFinished { get; private set; }

			[SerializeField] Image _bar;

			private float _target;

			public void StartLerp(float target)
			{
				_target = target;
				_bar.enabled = true;
				isFinished = false;
			}

			public void HideBar()
			{
				_bar.enabled = false;
				isFinished = true;
			}

			public void Lerp()
			{
				if (isFinished)
				{
					return;
				}

				_bar.fillAmount = Mathf.Lerp(_bar.fillAmount, _target, 3 * Time.deltaTime);

				if (Mathf.Abs(_bar.fillAmount - _target) < 0.01f)
				{
					_bar.fillAmount = _target;
					isFinished = true;
				}
			}
		}

		public PlayerId PlayerId { get; private set; }
		public bool IsOccupied => PlayerId >= PlayerId.One;

		[SerializeField] RawImage _imgVeh;

		[SerializeField] RectTransform _occupiedRoot;
		[SerializeField] RectTransform _unoccupiedRoot;

		[Space]
		[SerializeField] Image _imgNameBar;
		[SerializeField] Text _txtVehicleName;
		[SerializeField] Text _txtPlayerName;
		[SerializeField] Text _txtFactionName;
		[SerializeField] Text _txtWeaponName;
		[SerializeField] Image _imgLogo;

		[Space]
		[SerializeField] Image[] _blipsPower;
		[SerializeField] Image[] _blipsHealth;
		[SerializeField] Image[] _blipsSpeed;
		[SerializeField] Image[] _blipsAgility;

		[Space]
		[SerializeField] Color _statOnColour;
		[SerializeField] Color _statOffColour;

		[Space]
		[SerializeField] Image _iconAoe;
		[SerializeField] Image _iconLine;
		[SerializeField] Image _iconMini;
		[SerializeField] Image _iconBounce;
		[SerializeField] Image _iconHoming;
		[SerializeField] Image _iconCone;
		[SerializeField] Image _iconDisrupt;
		[SerializeField] Image _iconHeat;

		private bool _initialized;

		public void SetOccupied(PlayerId playerId)
		{
			if (playerId < PlayerId.One
				|| IsOccupied)
			{
				return;
			}

			PlayerId = playerId;

			PlayerManager.GetPlayerInfo(playerId, out var playerInfo);
			_imgNameBar.color = playerInfo.Colour * 0.8f;
			_txtPlayerName.text = playerInfo.DisplayName;

			ClearDisplayedUnit();

			_unoccupiedRoot.gameObject.SetActive(false);
			_occupiedRoot.gameObject.SetActive(true);
		}

		public void SetUnoccupied()
		{
			if (!IsOccupied)
			{
				return;
			}

			PlayerId = PlayerId.Invalid;

			ClearDisplayedUnit();

			_occupiedRoot.gameObject.SetActive(false);
			_unoccupiedRoot.gameObject.SetActive(true);
		}

		public void Display(ModelBooth booth, PlayablePawnInfo info)
		{
			_imgVeh.texture = booth.Texture;
			_imgVeh.enabled = true;
			_txtVehicleName.text = info.name;
			_txtFactionName.text = info.Faction;
			_imgLogo.sprite = info.Logo;
			_imgLogo.enabled = info.Logo != null;

			var basicInfo = info.Prefab.GetBasicInfo();
			_txtWeaponName.text = basicInfo.WeaponName.SpaceOut();

			DisplayScore(_blipsPower, info.PowerScore);
			DisplayScore(_blipsHealth, info.HealthScore);
			DisplayScore(_blipsSpeed, info.SpeedScore);
			DisplayScore(_blipsAgility, info.AgilityScore);

			SetAttributeIconStatus(_iconHeat, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.DoesHeatDamage));
			SetAttributeIconStatus(_iconAoe, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.HasExplosionDamage));
			SetAttributeIconStatus(_iconCone, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.IsConeEffect));
			SetAttributeIconStatus(_iconMini, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.IsMini));
			SetAttributeIconStatus(_iconLine, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.IsLineEffect));
			SetAttributeIconStatus(_iconBounce, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.DoesBounce));
			SetAttributeIconStatus(_iconHoming, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.IsHoming));
			SetAttributeIconStatus(_iconDisrupt, basicInfo.ProjectileInfo.Flags.IsSet((int)ProjectileBasicInfo.AttributeFlags.DoesDisrupt));
		}

		public void ClearDisplayedUnit()
		{
			_imgVeh.enabled = false;
			_imgLogo.enabled = false;

			_txtVehicleName.text = "None";
			_txtFactionName.text = "None";
			_txtWeaponName.text = "None";

			DisplayScore(_blipsPower, 0);
			DisplayScore(_blipsHealth, 0);
			DisplayScore(_blipsSpeed, 0);
			DisplayScore(_blipsAgility, 0);

			SetAttributeIconStatus(_iconHeat, false);
			SetAttributeIconStatus(_iconAoe, false);
			SetAttributeIconStatus(_iconCone, false);
			SetAttributeIconStatus(_iconMini, false);
			SetAttributeIconStatus(_iconLine, false);
			SetAttributeIconStatus(_iconBounce, false);
			SetAttributeIconStatus(_iconHoming, false);
			SetAttributeIconStatus(_iconDisrupt, false);
		}

        public void Init()
        {
			if (_initialized)
			{
				return;
			}

			_initialized = true;

			ReverseBlips(ref _blipsPower);
			ReverseBlips(ref _blipsHealth);
			ReverseBlips(ref _blipsSpeed);
			ReverseBlips(ref _blipsAgility);

			_occupiedRoot.gameObject.SetActive(false);
			_unoccupiedRoot.gameObject.SetActive(true);
        }

        private void ReverseBlips(ref Image[] blips)
		{
			blips = blips.Reverse().ToArray();
		}

		private void DisplayScore(in Image[] _images, int score)
		{
			if (_images == null
				|| _images.Length < score)
			{
				return;
			}

			for (int i = 0; i < _images.Length; ++i)
			{
				_images[i].color = i + 1 <= score ? _statOnColour : _statOffColour;
			}
		}

		private void SetAttributeIconStatus(Image icon, bool status)
		{
			icon.color = status ? _statOnColour : _statOffColour;
		}
	}
}
