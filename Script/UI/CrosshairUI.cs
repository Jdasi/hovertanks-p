using HoverTanks.Entities;
using HoverTanks.Loadouts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class CrosshairUI : MonoBehaviour
    {
        public bool IsShowing => _grpMain.alpha > 0;

        [Header("Parameters")]
        [SerializeField] float _spreadOffsetFactor;

        [Space]
        [SerializeField] Image _crosshairDotPrefab;

        [Space]
        [SerializeField] Text _txtAmmoRemaining;
        [SerializeField] Text _txtAmmoMax;

        [Space]
        [SerializeField] Image _imgReload;

        [Header("References")]
        [SerializeField] Image _crosshairTL;
        [SerializeField] Image _crosshairTR;
        [SerializeField] Image _crosshairBL;
        [SerializeField] Image _crosshairBR;
        [SerializeField] RectTransform _chargesRoot;
        [SerializeField] CanvasGroup _grpMain;

        private const int NUM_CROSSHAIR_DOTS = 8;

        private IPawn _pawn;
        private IWeaponInfo _weapon;
        private IModuleInfo _module;

        private List<Image> _imgDots;
        private Image[] _chargeImgs;
        private Vector3 _camOffset;

        private Vector3 _tlStart;
        private Vector3 _trStart;
        private Vector3 _blStart;
        private Vector3 _brStart;

        private bool _isShowRequested;

        public void Init(IPawn pawn)
        {
            _pawn = pawn;

            // setup weapon ui
            ProcessWeapon(pawn.WeaponInfo);

            _imgDots = new List<Image>(NUM_CROSSHAIR_DOTS);
            _camOffset = Camera.main.transform.forward * 15;
            _grpMain.transform.localPosition -= _camOffset;

            // create crosshair dots
            for (int i = 0; i < NUM_CROSSHAIR_DOTS; ++i)
            {
                var dot = Instantiate(_crosshairDotPrefab, _grpMain.transform);
                _imgDots.Add(dot);
            }

            _chargeImgs = _chargesRoot.GetComponentsInChildren<Image>();

            // setup module ui
            ProcessModule(pawn.ModuleInfo);

            _tlStart = _crosshairTL.rectTransform.localPosition;
            _trStart = _crosshairTR.rectTransform.localPosition;
            _blStart = _crosshairBL.rectTransform.localPosition;
            _brStart = _crosshairBR.rectTransform.localPosition;

            // start hidden
            SetVisible(false);
        }

        private void OnDestroy()
        {
            SetVisible(false);

            ProcessWeapon(null);
            ProcessModule(null);
        }

        public void Show()
        {
            _isShowRequested = true;
        }

        public void Hide()
        {
            _isShowRequested = false;
            SetVisible(false);
        }

        private void Update()
        {
            if (_pawn == null)
            {
                return;
            }

            HandleDots();
        }

        private void FixedUpdate()
        {
            if (_pawn == null)
            {
                return;
            }

            HandleVisibility();
            HandleCurrentSpread();
            HandleReloadBar();
        }

        private void SetVisible(bool visible)
        {
            if (IsShowing == visible)
            {
                return;
            }

            _grpMain.alpha = visible ? 1 : 0;
        }

        private void HandleDots()
        {
            Vector3 vehPos = _pawn.Position - _camOffset;
            Vector3 crossPos = _grpMain.transform.position;

            // place the dots towards the crosshair
            for (int i = 0; i < NUM_CROSSHAIR_DOTS; ++i)
            {
                float step = (float)(i + 1) / (NUM_CROSSHAIR_DOTS + 1);
                Vector3 pos = Vector3.Lerp(vehPos, crossPos, step);
                _imgDots[i].transform.position = pos;
            }
        }

        private void HandleCurrentSpread()
        {
            float currentSpread = _weapon != null ? _weapon.CurrentShotSpread : 0;

            _crosshairTL.rectTransform.localPosition = _tlStart + new Vector3(-_spreadOffsetFactor * currentSpread, _spreadOffsetFactor * currentSpread);
            _crosshairTR.rectTransform.localPosition = _trStart + new Vector3(_spreadOffsetFactor * currentSpread, _spreadOffsetFactor * currentSpread);
            _crosshairBL.rectTransform.localPosition = _blStart + new Vector3(-_spreadOffsetFactor * currentSpread, -_spreadOffsetFactor * currentSpread);
            _crosshairBR.rectTransform.localPosition = _brStart + new Vector3(_spreadOffsetFactor * currentSpread, -_spreadOffsetFactor * currentSpread);
        }

        private void HandleReloadBar()
        {
            if (_weapon == null)
            {
                return;
            }

            if (_imgReload == null
                || !_weapon.IsRecharging)
            {
                return;
            }

            _imgReload.fillAmount = 1 - (_weapon.ReloadTimer / _weapon.TimeToReload);
        }

        private void HandleVisibility()
        {
            if (IsShowing
                && !_isShowRequested)
            {
                SetVisible(false);
            }
            else if (!IsShowing
                && _isShowRequested)
            {
                SetVisible(true);
            }
        }

        private void ProcessWeapon(IWeaponInfo weapon)
        {
            if (_weapon != null)
            {
                _weapon.OnChargeCountChanged -= OnChargeCountChanged;
                _weapon.ReloadStarted -= OnReloadStarted;
                _weapon.ReloadFinished -= OnReloadFinished;
            }

            _weapon = weapon;

            if (_weapon != null)
            {
                _weapon.OnChargeCountChanged += OnChargeCountChanged;
                _weapon.ReloadStarted += OnReloadStarted;
                _weapon.ReloadFinished += OnReloadFinished;
            }

            UpdateAmmoText(_weapon);

            if (_weapon == null)
            {
                _imgReload.fillAmount = 0;

                return;
            }

            if (_weapon.IsRecharging)
            {
                OnReloadStarted();
            }
            else // cancel reload fx
            {
                _txtAmmoRemaining.enabled = true;
                _txtAmmoMax.enabled = true;

                _imgReload.fillAmount = 0;
            }
        }

        private void ProcessModule(IModuleInfo module)
        {
            if (_module != null)
            {
                _module.OnChargeCountChanged -= OnModuleChargeCountChanged;
            }

            _module = module;

            if (_module != null)
            {
                _module.OnChargeCountChanged += OnModuleChargeCountChanged;
                UpdateModuleCharges(_module.Charges);
            }
            else
            {
                foreach (var img in _chargeImgs)
                {
                    img.gameObject.SetActive(false);
                }
            }
        }

        private void OnChargeCountChanged(int count)
        {
            UpdateAmmoText(_weapon);
        }

        private void OnReloadStarted()
        {
            _txtAmmoRemaining.enabled = false;
            _txtAmmoMax.enabled = false;

            _imgReload.fillAmount = 0;
        }

        private void OnReloadFinished()
        {
            _txtAmmoRemaining.enabled = true;
            _txtAmmoMax.enabled = true;

            _imgReload.fillAmount = 0;
        }

        private void UpdateAmmoText(IWeaponInfo weapon)
        {
            bool isValid = weapon != null;

            _txtAmmoRemaining.text = $"{(isValid ? weapon.Charges : 0)}";
            _txtAmmoMax.text = $"/ {(isValid ? weapon.MaxCharges : 0)}";

            _txtAmmoRemaining.enabled = isValid;
            _txtAmmoMax.enabled = isValid;
        }

        private void OnModuleChargeCountChanged(int count)
        {
            UpdateModuleCharges(count);
        }

        private void UpdateModuleCharges(int count)
        {
            if (_pawn.ModuleInfo == null)
            {
                return;
            }

            for (int i = 0; i < _chargeImgs.Length; ++i)
            {
                _chargeImgs[i].gameObject.SetActive(i < count);
            }
        }
    }
}
