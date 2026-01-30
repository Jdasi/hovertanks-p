using UnityEngine;

namespace HoverTanks.Entities
{
	public struct TankSnapshot
	{
		public float Age => Time.time - _time;

		public InputData Input;
	    public PawnStateData State;

		private float _time;

		public void Refresh(InputData input, PawnStateData state)
		{
			_time = Time.time;
			Input = input;
			State = state;
		}
	}
}
