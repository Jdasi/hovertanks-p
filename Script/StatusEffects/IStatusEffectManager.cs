
namespace HoverTanks.StatusEffects
{
    public interface IStatusEffectManager
    {
        /// <summary>
        /// Check if a status is active.
        /// </summary>
        bool HasStatus(StatusClass @class);

        /// <summary>
        /// Add a status locally only.
        /// </summary>
        bool LocalAdd(StatusClass @class, out int handle, float duration = StatusEffect.PERMANENT_DURATION);

        /// <summary>
        /// Add a status locally only.
        /// </summary>
        bool LocalAdd(StatusClass @class, float duration = StatusEffect.PERMANENT_DURATION);

        /// <summary>
        /// Remove a status that was added locally.
        /// </summary>
        void LocalRemove(ref int handle);

        /// <summary>
        /// Add a status globally. Server only.
        /// </summary>
        bool Add(StatusClass @class, out int handle, float duration = StatusEffect.PERMANENT_DURATION);

        /// <summary>
        /// Add a status globally. Server only.
        /// </summary>
        bool Add(StatusClass @class, float duration = StatusEffect.PERMANENT_DURATION);

        /// <summary>
        /// Remove a status that was added globally. Server only.
        /// </summary>
        void Remove(ref int handle);

        /// <summary>
        /// Adds a status immunity locally only.
        /// </summary>
        void LocalAddImmunity(StatusClass @class);
    }
}
