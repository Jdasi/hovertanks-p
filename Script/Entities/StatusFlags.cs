using System;

namespace HoverTanks.Entities
{
	[Flags]
	public enum StatusFlags
	{
		FireGuard = Bits.Bit0,
		RadarJammer = Bits.Bit1,
		AdvancedTargeting = Bits.Bit2,
	}
}