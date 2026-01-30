
public struct Bitset
{
    private int _bits;

    public bool IsSet(int bit)
    {
        return Bit.IsSet(_bits, bit);
    }

    public bool IsSet(Bits bit)
    {
        return Bit.IsSet(_bits, (int)bit);
    }

    public void Set(int bit, bool set = true)
    {
        if (set)
        {
            Bit.Set(ref _bits, bit);
        }
        else
        {
            Unset(bit);
        }
    }

    public void Set(Bits bit, bool set = true)
    {
        Set((int)bit, set);
    }

    public void Unset(int bit)
    {
        Bit.Unset(ref _bits, bit);
    }

    public void Unset(Bits bit)
    {
        Unset((int)bit);
    }

    public void Reset()
    {
        _bits = 0;
    }
}
