using HoverTanks.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class UnitBaseUI : MonoBehaviour
    {
        [SerializeField] Image[] _imgs;

        private void Awake()
        {
            var owner = GetComponentInParent<Pawn>().identity;

            owner.OnIdentityChanged += (identity) =>
            {
                Color color = Color.white;

                if (PlayerManager.GetPlayerInfo(identity.playerId, out var playerInfo))
                {
                    color = playerInfo.Colour;
                }

                foreach (var img in _imgs)
                {
                    img.color = new Color(color.r, color.g, color.b, img.color.a);
                }
            };
        }
    }
}
