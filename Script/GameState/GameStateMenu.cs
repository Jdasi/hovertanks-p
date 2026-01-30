using HoverTanks.Events;
using HoverTanks.IO;
using HoverTanks.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.GameState
{
    public class GameStateMenu : GameState
    {
        private enum States
        {
            Invalid = -1,

            MainMenu,
            UnitSelect
        }

        [Header("Scenes")]
        [SerializeField] string _arcadeScene;

        [Header("Menus")]
        [SerializeField] GameObject _mainMenu;
        [SerializeField] GameObject _vehicleMenu;

        [Header("Misc UI")]
        [SerializeField] InputField _inputName;
        [SerializeField] InputField _inputIp;
        [SerializeField] Button _btnPlayGame;

        private States _state = States.Invalid;

        protected override void OnStateEnter()
        {
            EntityManager.Flush();

            if (string.IsNullOrEmpty(_inputName.text))
            {
                ReadInPlayerName();
            }

            // clear selections
            foreach (var player in PlayerManager.ActivePlayers.Values)
            {
                player.PawnClass = PawnClass.Invalid;
            }

            if (GameClient.IsConnected)
            {
                GoToVehicleSelect();
            }
            else
            {
                GoToMainMenu();
            }

            NetworkEvents.Subscribe<ClientConnectedMsg>(OnClientConnectedMsg);

            LocalEvents.Subscribe<DisconnectedData>(OnDisconnected);
        }

        private void OnDestroy()
        {
            NetworkEvents.Unsubscribe<ClientConnectedMsg>(OnClientConnectedMsg);

            LocalEvents.Unsubscribe<DisconnectedData>(OnDisconnected);
        }

        public void Host()
        {
            _mainMenu.SetActive(false);

            SaveOutPlayerName();
            GameClient.IP = _inputIp.text;
            GameClient.Host();
        }

        public void Join()
        {
            _mainMenu.SetActive(false);

            SaveOutPlayerName();
            GameClient.IP = _inputIp.text;
            GameClient.Connect();
        }

        public void Disconnect()
        {
            GameClient.Disconnect();
        }

        private void GoToVehicleSelect()
        {
            if (_state == States.UnitSelect)
            {
                return;
            }

            if (Server.IsActive)
            {
                Server.AreConnectionsAllowed = true;
            }

            _mainMenu.SetActive(false);
            _vehicleMenu.SetActive(true);

            _state = States.UnitSelect;
        }

        private void GoToMainMenu()
        {
            if (_state == States.MainMenu)
            {
                return;
            }

            _vehicleMenu.SetActive(false);
            _mainMenu.SetActive(true);

            _state = States.MainMenu;
        }

        public void StartGame()
        {
            if (!Server.IsActive)
            {
                return;
            }

            Server.AreConnectionsAllowed = false;

            using (var sendMsg = new LoadSceneMsg()
            {
                SceneName = _arcadeScene,
            })
            {
                ServerSend.ToAll(sendMsg);
            }
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private void ReadInPlayerName()
        {
            var data = ProfileIO.GetProfileData();
            GameClient.Username = _inputName.text = data.Username;
        }

        private void SaveOutPlayerName()
        {
            ProfileIO.SaveProfileData(GameClient.Username = _inputName.text);
        }

        private void OnClientConnectedMsg(ClientConnectedMsg msg)
        {
            if (msg.ClientId != GameClient.ClientId)
            {
                return;
            }

            GoToVehicleSelect();
        }

        private void OnDisconnected(DisconnectedData data)
        {
            GoToMainMenu();
        }
    }
}
