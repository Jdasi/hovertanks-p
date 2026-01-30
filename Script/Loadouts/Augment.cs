using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    public abstract class Augment : ScriptableObject
    {
        public AugmentClass AugmentClass => _augmentClass;

        [SerializeField] AugmentClass _augmentClass;

        public abstract void Apply(Pawn pawn);
    }
}
