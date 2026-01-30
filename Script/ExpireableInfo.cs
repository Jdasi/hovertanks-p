using UnityEngine;

public class ExpireableInfo<T>
{
	public readonly T Value;
	public bool HasExpired => Time.time >= _expireTime;

	private readonly float _expireTime;

	public ExpireableInfo(T value, float expireDelay = 0.05f)
	{
		Value = value;
		_expireTime = Time.time + expireDelay;
	}
}
