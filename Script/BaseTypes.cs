using System;

public enum ClientId
{
    Invalid = 0,

    Server = 1,
    Client1 = 2,
    Client2 = 3,
    Client3 = 4,
}

public enum PlayerId
{
    Invalid = 0,
    AI = 1,

    One = 2,
    Two = 3,
    Three = 4,
    Four = 5,
}

public enum EntityId
{
    Invalid = -1,
}

public enum TeamId
{
    None = 0,

    Alpha,
    Beta,
    Charlie,
    Delta,
    Echo,
    Foxtrot,
    Golf,
    Hotel,
    India,
    Juliett,
    Kilo,
    Lima,
    Mike,
    November,
    Oscar,
    Papa,
    Quebec,
    Romeo,
    Sierra,
    Tango,
    Uniform,
    Victor,
    Whiskey,
    XRay,
    Yankee,
    Zulu,
}

public enum StatusClass
{
    Invalid = 0,

    DisableRam = 1,
    Disrupted = 2,
    StealthChip = 3,
    FirmwareUpgrade = 4,
}

public enum EntityType
{
	Invalid = 0,

	Pawn = 1,
	Projectile = 2,
	Prop = 3,
	Pickup = 4,
    Structure = 5,
}

public enum PawnClass
{
    Invalid = 0,

    // faction 0 - UNSF
    Tank_F0_0 = 1,
    Tank_F0_1 = 2,
    Tank_F0_2 = 3,
    Tank_F0_3 = 4,
    Tank_F0_4 = 5,
    Tank_F0_5 = 6,
    Tank_F0_6 = 7,
    Turret_F0_0 = 8,
    Turret_F0_1 = 9,
    Turret_F0_2 = 10,
    Turret_F0_3 = 11,

    // faction 1 - Darkstar
    Tank_F1_0 = 13,
    Tank_F1_1 = 14,
    Tank_F1_2 = 15,
    Tank_F1_3 = 16,
    Tank_F1_4 = 17,
    Tank_F1_5 = 18,
    Tank_F1_6 = 19,
    Turret_F1_0 = 20,
    Turret_F1_1 = 21,
    Turret_F1_2 = 22,
    Turret_F1_3 = 23,

    // faction 2 - Junkers
    Tank_F2_0 = 25,
    Tank_F2_1 = 26,
    Tank_F2_2 = 27,
    Tank_F2_3 = 28,
    Tank_F2_4 = 29,
    Tank_F2_5 = 30,
    Tank_F2_6 = 31,
    Turret_F2_0 = 32,
    Turret_F2_1 = 33,
    Turret_F2_2 = 34,
    Turret_F2_3 = 35,

    // faction 3 - SWORD
    Tank_F3_0 = 37,
    Tank_F3_1 = 38,
    Tank_F3_2 = 39,
    Tank_F3_3 = 40,
    Tank_F3_4 = 41,
    Tank_F3_5 = 42,
    Tank_F3_6 = 43,
    Turret_F3_0 = 44,
    Turret_F3_1 = 45,
    Turret_F3_2 = 46,
    Turret_F3_3 = 47,

    // faction 4 - Phasetech
    Tank_F4_0 = 49,
    Tank_F4_1 = 50,
    Tank_F4_2 = 51,
    Tank_F4_3 = 52,
    Tank_F4_4 = 53,
    Tank_F4_5 = 54,
    Tank_F4_6 = 55,
    Turret_F4_0 = 56,
    Turret_F4_1 = 57,
    Turret_F4_2 = 58,
    Turret_F4_3 = 59,
}

public enum ProjectileClass
{
    Invalid = 0,

    ATShell = 1,
    BulletHeavy = 2,
    AirburstShell = 3,
    GaussRound = 4,
    Grenade = 5,
    HEShell = 6,
    MicroMissile = 7,
    Mortar = 8,
    PlasmaBolt = 9,
    PlasmaOrb = 10,
    Rivet = 11,
    Rocket = 12,
    LRShell = 13,
    RMShell = 14,
    DualShot = 15,
    BulletLight = 16,
    LightningBolt = 17,
    ArcLance = 18,
    ElectricOrb = 19,
    MicroRocket = 20,
    MicroShell = 21,
    MicroFlame = 22,
    PulsarShot = 23,
    CinderShot = 24,
    ForceWave = 25,
    RestorerShot = 26,
    BlastWave = 27,

    Dummy9 = 28,
    Dummy10 = 29,
    Dummy11 = 30,
    Dummy12 = 31,
    Dummy13 = 32,
    Dummy14 = 33,
    Dummy15 = 34,
    Dummy16 = 35,
    Dummy17 = 36,
}

public enum PickupClass
{
    Invalid = 0,

    HealthOrb = 1,
}

public enum PropClass
{
    Invalid = 0,

    ProximityMine = 1,
}

/// <summary>
/// Entries must match the name of their respective prefabs.
/// </summary>
public enum ModuleClass
{
    Invalid = 0,

    MissilePod = 1,
    AirburstPod = 2,
    MGauss = 3,
    RocketPod = 4,
    MCannon = 5,
    MScorcher = 6,
    MPulsar = 7,
    ForceWave = 8,
    MRestorer = 9,
    BlastWave = 10,
    ProximityMine = 11,
    XBomb = 12,
    BlitzerBeacon = 13,
    DeployableShield = 14,
    Flares = 15,
    EMP = 16,
    HealingNova = 17,
    Vent = 18,
    Dash = 19,
    Phase = 20,
}

/// <summary>
/// Entries must match the name of their respective prefabs.
/// </summary>
public enum AugmentClass
{
    Invalid = 0,

    AutoLoader = 1,
    ResourcePack = 2,
    StealthChip = 3,
    EssenceExtractor = 4,
    GyroUnlocker = 5,
    HeavyFrame = 6,

    DUMMY_0 = 7,

    ForceBumper = 8,
    LightFrame = 9,
    RacingMode = 10,
    Stabilizers = 11,
    WaveDeflector = 12,
    ThermalRegulator = 13,
    ShockDampeners = 14,
    SurgeProtection = 15,
    VerticalThrusters = 16,
    FirmwareUpgrade = 17,
    AuxiliaryPower = 18,
    PersonalForcefield = 19,
    AeonShard = 20,
}

public enum Direction
{
    None = 0,

    Up = 1,
    Down = 2,
    Left = 3,
    Right = 4,
}

public enum WeaponTypes
{
    Invalid = 0,

    ProjectileDirect = 1,
    ProjectileIndirect = 2,
    SustainedDirect = 3,
    Melee = 4,
}

public enum SustainedEffectTypes
{
    Invalid = 0,

    Line = 1,
    Cone = 2,
}

public enum HitRequestType
{
    Invalid = 0,

    SustainedWeapon = 1,
    MeleeWeapon = 2,
    Module = 3,
}

public enum EquipmentType
{
    Invalid = 0,

    Weapon = 1,
    Module = 2,
    Augment = 3,
}

public enum DamageLevel
{
    None = 0,
    Low = 1,
    Medium = 25,
    Heavy = 50,
    Critical = 75,
}

public enum HeatLevel
{
    Trivial = 0,
    Medium = 40,
    High = 65,
    Critical = 90,
    Overheating = 100,
}

public enum AccoladeType
{
    None = 0,

    // micro sprees
    DoubleKill = 1,
    TripleKill = 2,
    MegaKill = 3,
    UltraKill = 4,
    Overkill = 5,
    KillCrazy = 6,

    // macro sprees
    KillingSpree = 10,
    Formidable = 11,
    Dominating = 12,
    Unstoppable = 13,
    Invincible = 14,
    Godlike = 15,
    Omnipotent = 16,
    TheOne = 17,

    // special kills
    Demolition = 20,
    WallBounce = 21,
    LongShot = 22,
    Riposte = 23,
    Interrupt = 24,
    Eviction = 25,
    Sabotage = 26,
    ChillOut = 27,
    HotHead = 28,
    DodgeThis = 29,
    Assist = 30,
    CoveringFire = 31,

    // special sprees
    Planner = 50,
    Tactician = 51,
    Strategist = 52,
    Sniper = 53,
    EagleEye = 54,
    WizzBang = 55,
    CloseQuarters = 56,
    SwiftJustice = 57,
    DownTheBarrel = 58,
    RoadRage = 59,
    WreckingBall = 60,
    BeTheBullet = 61,
    QuickRepair = 62,
    Scavenger = 63,
    ComebackKid = 64,
    Saviour = 65,
    Protector = 66,
    GuardianAngel = 67,

    // special events
    FromTheBrink = 100,
    NotEvenClose = 101,
    Perfectionist = 102,
}

public enum ElementType
{
    Invalid = 0,

    Bullet = 1,
    Explosive = 2,
    Plasma = 3,
    Force = 4,
    Ram = 5,
    Overheat = 6,
    HealthOrb = 7,
    Fire = 8,
    Void = 9,
    Laser = 10,
    Lightning = 11,
    Restorer = 12,
}

[Flags]
public enum ElementFlags
{
	None = 0,

	IsAoe = Bits.Bit0,
	WasGlancingBlow = Bits.Bit1,
}

public enum MountPoint
{
    None = 0,

    Turret = 1,
}

public enum AwardCreditsReason
{
    Invalid = 0,

    EnemyDefeated = 1,
}

public enum ArcadeLevelResult
{
    Pending = 0,

    ProgressToNext,
    ProgressToModShop,
    ProgressFromModShop,
    Failed,
}

[Flags]
public enum TeleportFlags
{
    None = 0,

    TeleportIn = Bits.Bit0,
    TeleportOut = Bits.Bit1,
}

public readonly struct AccoladeInfo
{
    public readonly AccoladeType Type;
    public readonly string Name;
    public readonly short Score;
    public readonly short Threshold;

    public AccoladeInfo(AccoladeType type, short score, short threshold)
    {
        Type = type;
        Name = type.ToString().SpaceOut();
        Score = score;
        Threshold = threshold;
    }
}
