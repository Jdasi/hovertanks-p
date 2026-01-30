using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.Networking;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class PawnStateUI : MonoBehaviour
    {
        private enum States
        {
            WaitBeforeLerp,
            LerpingDamage,
            Holding,
            Idle
        }

        private const float DELAY_BEFORE_LERP = 0.75f;
        private const float DAMAGE_LERP_SPEED = 6f;
        private const float MAX_IDLE_TIME = 1f;
        private const float FADE_OUT_TIME = 0.5f;

        [SerializeField] FadableGraphic _healthBarFade;
        [SerializeField] FadableGraphic _nameFade;
        [SerializeField] GameObject _heatCriticalRoot;
        [SerializeField] Image _imgHealthBar;
        [SerializeField] Image _imgDamageBar;
        [SerializeField] ShakeModule _lifeShake;
        [SerializeField] Text _lblCredits;
        [SerializeField] GameObject _interactRoot;
        [SerializeField] Text _lblInteractDescription;

        private LifeForce _life;

        private float _nextLerpTime;
        private float _healthIdleTimer;

        private States _state;

        private void Start()
        {
            _life = GetComponentInParent<LifeForce>();
            _life.OnHealthChanged += OnHealthChanged;
            _life.OnHeatLevelChanged += OnHeatLevelChanged;
            _life.identity.OnIdentityChanged += OnOwnerIdentityChanged;
            OnOwnerIdentityChanged(_life.identity);

            _lblCredits.gameObject.SetActive(false);
            _healthBarFade.FadeOut(0);
            _heatCriticalRoot.SetActive(false);
            _interactRoot.SetActive(false);
            _state = States.Idle;

            LocalEvents.Subscribe<ArcadeModShopStartedData>(OnArcadeModShopStarted);
            LocalEvents.Subscribe<ArcadePlayerCreditsChangedData>(OnPlayerCreditsChanged);
            LocalEvents.Subscribe<InteractContextChangedData>(OnInteractContextChanged);
        }

        private void OnDestroy()
        {
            LocalEvents.Unsubscribe<ArcadeModShopStartedData>(OnArcadeModShopStarted);
            LocalEvents.Unsubscribe<ArcadePlayerCreditsChangedData>(OnPlayerCreditsChanged);
            LocalEvents.Unsubscribe<InteractContextChangedData>(OnInteractContextChanged);
        }

        private void OnHealthChanged(HealthChangedData data)
        {
            StartHealthLerp(data.Percent);

            if (data.WasDamage)
            {
                _lifeShake.Shake(0.05f, 0.2f);
            }
        }

        private void OnHeatLevelChanged(HeatLevelChangedData data)
        {
            bool isHeatCritical = data.Level >= HeatLevel.Critical;

            if (_heatCriticalRoot.activeSelf != isHeatCritical)
            {
                _heatCriticalRoot.SetActive(isHeatCritical);
            }
        }

        private void OnOwnerIdentityChanged(NetworkIdentity identity)
        {
            Color color = Color.white;

            if (PlayerManager.GetPlayerInfo(identity.playerId, out var playerInfo))
            {
                color = playerInfo.Colour;
            }

            foreach (var graphic in _nameFade.GetComponentsInChildren<Graphic>())
            {
                graphic.color = color - new Color(0.2f, 0.2f, 0.2f, 0);
                graphic.gameObject.SetActive(identity.playerId >= PlayerId.One);
            }

            var lblName = _nameFade.GetComponentInChildren<Text>();

            if (lblName != null)
            {
                lblName.text = playerInfo.DisplayName;
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case States.WaitBeforeLerp:
                {
                    if (Time.time >= _nextLerpTime)
                    {
                        _state = States.LerpingDamage;
                    }
                } break;

                case States.LerpingDamage:
                {
                    float percent;
                    float diff = Mathf.Abs(_imgHealthBar.fillAmount - _imgDamageBar.fillAmount);

                    if (diff > 0.01f)
                    {
                        // still lerping
                        percent = Mathf.Lerp(_imgDamageBar.fillAmount, _imgHealthBar.fillAmount, DAMAGE_LERP_SPEED * Time.fixedDeltaTime);
                    }
                    else
                    {
                        // close enough
                        percent = _imgHealthBar.fillAmount;
                        _healthIdleTimer = 0;

                        _state = States.Holding;
                    }

                    _imgDamageBar.fillAmount = percent;
                } break;

                case States.Holding:
                {
                    _healthIdleTimer += Time.unscaledDeltaTime;

                    if (_healthIdleTimer >= MAX_IDLE_TIME)
                    {
                        _healthIdleTimer = 0;
                        _state = States.Idle;

                        _healthBarFade.FadeOut(FADE_OUT_TIME);
                    }
                } break;
            }
        }

        private void StartHealthLerp(float healthPercent)
        {
            _imgDamageBar.fillAmount = Mathf.Max(_imgDamageBar.fillAmount, _imgHealthBar.fillAmount);
            _imgHealthBar.fillAmount = healthPercent;

            _nextLerpTime = Time.time + DELAY_BEFORE_LERP;
            _healthBarFade.FadeIn(0);

            _state = States.WaitBeforeLerp;
        }

        private void OnArcadeModShopStarted(ArcadeModShopStartedData data)
        {
            int credits = 0;

            switch (_life.identity.playerId)
            {
                case PlayerId.One:      credits = data.Player1Credits; break;
                case PlayerId.Two:      credits = data.Player2Credits; break;
                case PlayerId.Three:    credits = data.Player3Credits; break;
                case PlayerId.Four:     credits = data.Player4Credits; break;
            }

            UpdateCreditsDisplay(credits);
            _lblCredits.gameObject.SetActive(true);
        }

        private void OnPlayerCreditsChanged(ArcadePlayerCreditsChangedData data)
        {
            if (_life.identity.playerId != data.PlayerId)
            {
                return;
            }

            if (!_lblCredits.gameObject.activeInHierarchy)
            {
                return;
            }

            UpdateCreditsDisplay(data.NewAmount);
        }

        private void UpdateCreditsDisplay(int amount)
        {
            _lblCredits.text = $"{amount} CR";
        }

        private void OnInteractContextChanged(InteractContextChangedData data)
        {
            if (_life.identity.playerId != data.PlayerId)
            {
                return;
            }

            _lblInteractDescription.text = data.Description;
            _interactRoot.SetActive(!string.IsNullOrWhiteSpace(data.Description));
        }
    }
}
