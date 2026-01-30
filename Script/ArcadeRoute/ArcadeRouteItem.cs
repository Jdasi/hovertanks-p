
namespace HoverTanks.ArcadeRoute
{
    public class ArcadeRouteItem
    {
        public readonly ArcadeLevelDefinition Level;
        public readonly Direction ExitDir;

        public ArcadeRouteItem(ArcadeLevelDefinition level, Direction exitDir)
        {
            Level = level;
            ExitDir = exitDir;
        }
    }
}
