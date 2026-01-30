using HoverTanks.Events;
using HoverTanks.Networking;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HoverTanks.UI
{
    public class GamePauseUI : MonoBehaviour
    {
        [SerializeField] GameObject _pausePanel;
        [SerializeField] Button _btnResume;

        public void Resume()
        {
            TogglePause();
        }

        public void ReturnToLobby()
        {
            string menuScene = "GameStateMenu";

            _pausePanel.SetActive(false);

            if (Server.IsActive)
            {
                using (var sendMsg = new LoadSceneMsg()
                {
                    SceneName = menuScene,
                })
                {
                    ServerSend.ToAll(sendMsg);
                }
            }
            else
            {
                GameClient.Disconnect();
                GameManager.LoadScene(menuScene);
            }
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void OnDestroy()
        {
            Game.isPauseMenuOpen = false;
        }

        private void Update()
        {
            if (Input.GetButtonDown("Menu"))
            {
                TogglePause();
            }
        }

        private void TogglePause()
        {
            Game.isPauseMenuOpen = !Game.isPauseMenuOpen;
            _pausePanel.SetActive(Game.isPauseMenuOpen);

            if (Game.isPauseMenuOpen)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // control time if only one client
            if (Server.IsActive
                && Server.Clients.Count == 1)
            {
                Time.timeScale = Game.isPauseMenuOpen ? 0 : 1;
            }

            LocalEvents.Invoke(new PauseMenuToggledData());
        }
    }
}
