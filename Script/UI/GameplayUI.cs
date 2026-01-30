using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class GameplayUI : MonoBehaviour
    {
        public bool IsAnimatingHUDs => _tweeningHudIds.Count > 0;

        [Header("Panels")]
        [SerializeField] RectTransform _pnlSegue;
        [SerializeField] RectTransform _pnlOverlay;

        [Header("Fades")]
        [SerializeField] FadableGraphic _fadeSegue;
        [SerializeField] FadableGraphic _fadeAnnouncement;

        [Header("Gameplay Text")]
        [SerializeField] Text _txtAnnouncement;
        [SerializeField] Text _txtLives;

        [Header("Player HUD")]
        [SerializeField] PlayerOverlayHUD[] _playerHuds;

        [Header("Enemies HUD")]
        [SerializeField] RectTransform _pnlEnemies;
        [SerializeField] Text _txtEnemies;

        private const float HUD_HIDE_Y_OFFSET = 200;

        private List<PlayerId> _tweeningHudIds;
        private float _pnlEnemiesStartY;
        private int _remainingEnemiesCount;

        public void Init()
        {
            gameObject.SetActive(true);

            _txtAnnouncement.text = "";
            _txtAnnouncement.gameObject.SetActive(true);
            _tweeningHudIds = new List<PlayerId>(4);
            _pnlEnemiesStartY = _pnlEnemies.localPosition.y;
            _pnlEnemies.localPosition += new Vector3(0, HUD_HIDE_Y_OFFSET, 0);

            for (int i = 0; i < _playerHuds.Length; ++i)
            {
                PlayerId playerId = PlayerId.One + i;
                var hud = _playerHuds[i];

                // prepare for animation
                hud.StartPos = hud.transform.localPosition;
                HidePlayerHud(playerId);

                if (!PlayerManager.ActivePlayers.ContainsKey(playerId))
                {
                    continue;
                }

                hud.Init(playerId);
            }

            LocalEvents.Subscribe<PawnRegisteredData>(OnPawnRegistered);
            LocalEvents.Subscribe<PawnUnregisteredData>(OnPawnUnregistered);
            LocalEvents.Subscribe<ArcadePlayerCreditsChangedData>(OnArcadePlayerCreditsChanged);
        }

        private void OnDestroy()
        {
            LocalEvents.Unsubscribe<PawnRegisteredData>(OnPawnRegistered);
            LocalEvents.Unsubscribe<PawnUnregisteredData>(OnPawnUnregistered);
            LocalEvents.Unsubscribe<ArcadePlayerCreditsChangedData>(OnArcadePlayerCreditsChanged);
        }

        public void RevealActivePlayerHUDs()
        {
            for (int i = 0; i < _playerHuds.Length; ++i)
            {
                PlayerId playerId = PlayerId.One + i;
                HidePlayerHud(playerId);

                if (!PlayerManager.ActivePlayers.ContainsKey(playerId))
                {
                    continue;
                }

                _playerHuds[i].Reset();
                AnimatePlayerHudIn(playerId);
            }
        }

        public void HideAllPlayerHUDs()
        {
            for (int i = 0; i < _playerHuds.Length; ++i)
            {
                AnimatePlayerHudOut(_playerHuds[i].PlayerId);
            }
        }

        public void HidePlayerHUDs(PlayerId[] playerIds)
        {
            foreach (var playerId in playerIds)
            {
                var hud = _playerHuds.FirstOrDefault(elem => elem.PlayerId == playerId);

                if (hud == null)
                {
                    continue;
                }

                AnimatePlayerHudOut(hud.PlayerId);
            }
        }

        public void ResetRemainingEnemyCount()
        {
            _remainingEnemiesCount = 0;
            RefreshRemainingEnemiesDisplay();
        }

        public void RevealRemainingEnemiesHUD()
        {
            _pnlEnemies.LeanMoveLocalY(_pnlEnemiesStartY, 0.4f)
                .setEaseSpring();
        }

        public void HideRemainingEnemiesHUD()
        {
            _pnlEnemies.LeanMoveLocalY(_pnlEnemiesStartY + HUD_HIDE_Y_OFFSET, 0.75f)
                .setEaseInOutBack();
        }

        public void FadeOutSeguePanel(float t)
        {
            if (!_pnlSegue.gameObject.activeSelf)
            {
                _pnlSegue.gameObject.SetActive(true);
            }

            _fadeSegue.FadeOut(t);
        }

        public void FadeInSeguePanel(float t)
        {
            if (!_pnlSegue.gameObject.activeSelf)
            {
                _pnlSegue.gameObject.SetActive(true);
            }

            _fadeSegue.FadeIn(t);
        }

        public void Announce(string text, float t)
        {
            _fadeAnnouncement.StopFade();
            _txtAnnouncement.color = Color.white;
            _txtAnnouncement.text = text;
            _txtAnnouncement.enabled = true;

            var method = nameof(FadeOutOverlayText);
            CancelInvoke(method);
            Invoke(method, t);
        }

        private void FadeOutOverlayText()
        {
            _fadeAnnouncement.FadeOut(0.5f);
        }

        private void UpdateCreditsDisplayForPlayer(PlayerId playerId, int amount)
        {
            for (int i = 0; i < _playerHuds.Length; ++i)
            {
                var hud = _playerHuds[i];

                if (hud.PlayerId != playerId)
                {
                    continue;
                }

                hud.UpdateCreditsDisplay(amount);
                break;
            }
        }

        private void OnPawnRegistered(PawnRegisteredData data)
        {
            if (data.Pawn.identity.teamId != GameManager.DefaultAITeamId)
            {
                return;
            }

            ++_remainingEnemiesCount;
            RefreshRemainingEnemiesDisplay();
        }

        private void OnPawnUnregistered(PawnUnregisteredData data)
        {
            if (data.Pawn.identity.teamId != GameManager.DefaultAITeamId)
            {
                return;
            }

            --_remainingEnemiesCount;
            RefreshRemainingEnemiesDisplay();
        }

        private void OnArcadePlayerCreditsChanged(ArcadePlayerCreditsChangedData data)
        {
            UpdateCreditsDisplayForPlayer(data.PlayerId, data.NewAmount);
        }

        private void HidePlayerHud(PlayerId id)
        {
            if (!TryGetPlayerHudById(id, out var hud))
            {
                return;
            }

            hud.transform.localPosition = hud.StartPos - new Vector3(0, HUD_HIDE_Y_OFFSET);
        }

        private void AnimatePlayerHudIn(PlayerId id)
        {
            if (!TryGetPlayerHudById(id, out var hud))
            {
                return;
            }

            int index = id - PlayerId.One;

            hud.transform.LeanMoveLocalY(hud.StartPos.y, 0.4f)
                .setDelay(Random.Range(index * 0.05f, index * 0.1f))
                .setEaseSpring()
                .setOnComplete(() => _tweeningHudIds.Remove(id));

            _tweeningHudIds.Remove(id);
            _tweeningHudIds.Add(id);
        }

        private void AnimatePlayerHudOut(PlayerId id)
        {
            if (!TryGetPlayerHudById(id, out var hud))
            {
                return;
            }

            int index = id - PlayerId.One;

            hud.transform.LeanMoveLocalY(hud.StartPos.y - HUD_HIDE_Y_OFFSET, 0.8f)
                .setDelay(Random.Range(index * 0.05f, index * 0.1f))
                .setEaseInOutBack()
                .setOnComplete(() => _tweeningHudIds.Remove(id));

            _tweeningHudIds.Remove(id);
            _tweeningHudIds.Add(id);
        }

        private bool TryGetPlayerHudById(PlayerId id, out PlayerOverlayHUD hud)
        {
            hud = null;

            int index = id - PlayerId.One;

            if (index < 0 || index >= _playerHuds.Length)
            {
                return false;
            }

            hud = _playerHuds[index];

            return true;
        }

        private void RefreshRemainingEnemiesDisplay()
        {
            _txtEnemies.text = $"x {_remainingEnemiesCount}";
        }
    }
}
