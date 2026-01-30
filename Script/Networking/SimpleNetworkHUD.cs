using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.Networking
{
    public class SimpleNetworkHUD : MonoBehaviour
    {
        /// <summary>
        /// Whether to show the default control HUD at runtime.
        /// </summary>
        [SerializeField] bool _showUi = true;

        /// <summary>
        /// The horizontal offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        [SerializeField] int _offsetX;

        /// <summary>
        /// The vertical offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        [SerializeField] int _offsetY;

        private void OnGUI()
        {
            if (!_showUi)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(_offsetX, _offsetY, 300, 9999));

            if (!GameClient.IsConnected)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
                StopButtons();
            }

            GUILayout.EndArea();
        }

        private void StartButtons()
        {
            if (GameClient.IsConnecting)
            {
                GUILayout.Label("Connecting to " + GameClient.IP + "..");

                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    GameClient.Disconnect();
                }
            }
            else if (!GameClient.IsConnected)
            {
                if (GUILayout.Button("Host (Server + Client)"))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        GameClient.IP = "hovertanks.aeons.dev";
                    }

                    GameClient.Host();
                }

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Client"))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        GameClient.IP = "hovertanks.aeons.dev";
                    }
                    GameClient.Connect();
                }

                GameClient.IP = GUILayout.TextField(GameClient.IP);

                GUILayout.EndHorizontal();
            }
        }

        private void StatusLabels()
        {
            if (Server.IsActive)
            {
                GUILayout.Label("Server: active");
                GUILayout.Label($"Last Ping Timestamp: {GameClient.LastPingTimestamp}");

                GUILayout.Label("Clients:");
                foreach (var client in Server.Clients.Values)
                {
                    GUILayout.Label($"[{client.Id}] Latency: {client.Latency}");
                }
            }
            else if (GameClient.IsConnected)
            {
                GUILayout.Label($"Client: address({GameClient.IP})");
                GUILayout.Label($"Last Ping Timestamp: {GameClient.LastPingTimestamp}");
                GUILayout.Label($"Latency: {GameClient.Latency}");
            }
        }

        private void StopButtons()
        {
            if (!GameClient.IsConnected)
            {
                return;
            }

            if (GUILayout.Button(Server.IsActive ? "Stop Server" : "Disconnect"))
            {
                GameClient.Disconnect();
            }
        }
    }
}
