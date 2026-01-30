using HoverTanks.Entities;
using UnityEngine;

[CreateAssetMenu(menuName = "Info/PlayablePawn")]
public class PlayablePawnInfo : ScriptableObject
{
    public Pawn Prefab;
    public GameObject Model;

    [Space]
    public Sprite Logo;
    public string Faction;

    [Space]
    [Range(1, 5)] public int PowerScore = 1;
    [Range(1, 5)] public int HealthScore = 1;
    [Range(1, 5)] public int SpeedScore = 1;
    [Range(1, 5)] public int AgilityScore = 1;
}
