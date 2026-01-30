
public class ShopInfo
{
    public string DisplayName { get; }
    public string Description { get; }
    public uint CreditCost { get; }
    public uint ReactorCost { get; }

    public ShopInfo(string displayName, string description, uint creditCost, uint reactorCost)
    {
        DisplayName = displayName;
        Description = description;
        CreditCost = creditCost;
        ReactorCost = reactorCost;
    }
}
