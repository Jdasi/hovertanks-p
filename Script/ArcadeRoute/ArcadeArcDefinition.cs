using UnityEngine;

namespace HoverTanks.ArcadeRoute
{
    [CreateAssetMenu(menuName = "Arcade/Arc Definition")]
    public class ArcadeArcDefinition : ScriptableObject
    {
        public ArcadeLevelDefinition[] Levels;
    }
}
