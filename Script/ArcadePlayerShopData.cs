using System.Collections.Generic;

public class ArcadePlayerShopData
{
	public const int MAX_REACTOR_SLOT_UNLOCKS = 9;
	public const int MAX_MOD_SLOTS = 3;
	public const int MAX_AUGMENT_SLOTS = 3;

    public int UnlockedReactorSlots = 1;
	public int UsedReactorSlots;
	public int RemainingReactorSlots => UnlockedReactorSlots - UsedReactorSlots;

	public readonly List<ModuleClass> _moduleStorage = new List<ModuleClass>();
	public readonly List<AugmentClass> _augmentStorage = new List<AugmentClass>();
}
