using HoverTanks.Networking;

namespace HoverTanks.StatusEffects
{
    public interface IStatusEffectTarget
    {
        EntityType EntityType { get; }
        NetworkIdentity identity { get; }
        IStatusEffectManager StatusEffectManager { get; }
    }
}
