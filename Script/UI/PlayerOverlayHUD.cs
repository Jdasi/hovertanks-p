using HoverTanks.Entities;
using HoverTanks.Events;
using UnityEngine;
using UnityEngine.UI;

public class PlayerOverlayHUD : MonoBehaviour
{
    public PlayerId PlayerId { get; private set; }
    public Vector3 StartPos { get; set; }

    [Header("Parameters")]
    [SerializeField] float _healthBarLerpSpeed = 5;

    [Header("References")]
    [SerializeField] CanvasGroup _grpMain;
    [SerializeField] CanvasGroup _grpHealthBar;
    [SerializeField] Text _lblName;
    [SerializeField] Image _imgHeader;
    [SerializeField] Slider _healthBar;
    [SerializeField] Text _lblState;
    [SerializeField] Text _lblScoreReadout;

    private LifeForce _pawnLife;
    private bool _lerpingHealthBar;

    public void Init(PlayerId playerId)
    {
        if (PlayerId != PlayerId.Invalid)
        {
            Log.Error(LogChannel.GameplayUI, $"[PlayerOverlayHUD] Init - called when PlayerId already set: {PlayerId}");
            return;
        }

        if (!PlayerManager.GetPlayerInfo(playerId, out var playerInfo))
        {
            return;
        }

        PlayerId = playerId;
        _lblName.text = playerInfo.DisplayName;

        var color = playerInfo.Colour;
        _imgHeader.color = new Color(color.r - 0.2f, color.g - 0.2f, color.b - 0.2f, color.a);

        LocalEvents.Subscribe<PawnRegisteredData>(OnPawnRegistered);
        LocalEvents.Subscribe<PawnUnregisteredData>(OnPawnUnregistered);
        LocalEvents.Subscribe<ArcadeLevelPlayerExtractedData>(OnArcadeMapPlayerExtracted);
    }

    private void OnDestroy()
    {
        LocalEvents.Unsubscribe<PawnRegisteredData>(OnPawnRegistered);
        LocalEvents.Unsubscribe<PawnUnregisteredData>(OnPawnUnregistered);
        LocalEvents.Unsubscribe<ArcadeLevelPlayerExtractedData>(OnArcadeMapPlayerExtracted);
    }

    public void Reset()
    {
        _grpMain.alpha = 1;
        _grpHealthBar.alpha = 1;
        _lblState.enabled = false;
    }

    public void UpdateCreditsDisplay(int amount)
    {
        _lblScoreReadout.text = $"{amount}";
    }

    private void Update()
    {
        if (_lerpingHealthBar)
        {
            HandleHealthBar();
        }
    }

    private void HandleHealthBar()
    {
        float target = _pawnLife.GetHealthPercent();
        _healthBar.value = Mathf.Lerp(_healthBar.value, target, Time.deltaTime * _healthBarLerpSpeed);

        if (Mathf.Abs(_healthBar.value - target) < 0.001f)
        {
            _healthBar.value = target;
            _lerpingHealthBar = false;
        }
    }

    private void OnPawnRegistered(PawnRegisteredData data)
    {
        if (PlayerId != data.Pawn.identity.playerId)
        {
            return;
        }

        _pawnLife = data.Pawn.Life;
        _pawnLife.OnHealthChanged += OnPawnHealthChanged;
        _healthBar.value = _pawnLife.GetHealthPercent();
    }

    private void OnPawnUnregistered(PawnUnregisteredData data)
    {
        if (PlayerId != data.Pawn.identity.playerId)
        {
            return;
        }

        if (_pawnLife == null)
        {
            return;
        }

        _lerpingHealthBar = false;
        _pawnLife.OnHealthChanged -= OnPawnHealthChanged;
        _pawnLife = null;
    }

    private void OnArcadeMapPlayerExtracted(ArcadeLevelPlayerExtractedData data)
    {
        if (PlayerId != data.PlayerId)
        {
            return;
        }

        DisplayState("-- Extracted -- ");
    }

    private void OnPawnHealthChanged(HealthChangedData data)
    {
        if (data.Percent == 0)
        {
            DisplayState("-- Destroyed -- ");
        }
        else
        {
            _lerpingHealthBar = true;
        }
    }

    private void DisplayState(string state)
    {
        _grpHealthBar.alpha = 0;
        _lblState.text = state;
        _lblState.enabled = true;
    }
}
