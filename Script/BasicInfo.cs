using System;

public struct PawnBasicInfo
{
    public string WeaponName;
    public float ForwardSpeed;
    public WeaponTypes WeaponType;
    public ProjectileBasicInfo ProjectileInfo;
}

public struct ProjectileBasicInfo
{
    [Flags]
    public enum AttributeFlags
    {
        None = 0,

        DoesBounce = Bits.Bit0,
        IsMini = Bits.Bit1,
        IsHoming = Bits.Bit2,
        DoesHeatDamage = Bits.Bit3,
        HasExplosionDamage = Bits.Bit4,
        IsLineEffect = Bits.Bit5,
        IsConeEffect = Bits.Bit6,
        DoesDisrupt = Bits.Bit7,
    }

    public Bitset Flags;
}
