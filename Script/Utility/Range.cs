using System;

public class Range<T>
{
    public T Min;
    public T Max;
}

[Serializable] public class IntRange : Range<int> {}
[Serializable] public class FloatRange : Range<float> {}
