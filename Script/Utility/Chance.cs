using System;
using UnityEngine;

using Random = UnityEngine.Random;

[Serializable]
public class Chance
{
    private const uint UPPER_RANGE = 100;

    [SerializeField][Range(0, UPPER_RANGE)] uint _value;

    public bool Test()
    {
        if (_value == 0)
        {
            return false;
        }

        return Random.Range(0, UPPER_RANGE) <= _value;
    }
}
