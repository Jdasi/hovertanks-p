using HoverTanks.Events;
using HoverTanks.Loadouts;
using HoverTanks.Networking;

public partial class ArcadePlayerInfo
{
    public  PlayerId PlayerId { get; }
    public int Credits { get; private set; }
    public Loadout Loadout { get; }

    private readonly ArcadePlayerShopData _localShopData;

    public ArcadePlayerInfo(PlayerId playerId)
    {
        PlayerId = playerId;

        NetworkEvents.Subscribe<CreditsAwardedMsg>(OnCreditsAwardedMsg);
        NetworkEvents.Subscribe<AccoladeAwardedMsg>(OnAccoladeAwardedMsg);

        if (GameClient.IsLocalPlayer(playerId))
        {
            _localShopData = new ArcadePlayerShopData();
        }
    }

    public void Cleanup()
    {
        NetworkEvents.Unsubscribe<CreditsAwardedMsg>(OnCreditsAwardedMsg);
        NetworkEvents.Unsubscribe<AccoladeAwardedMsg>(OnAccoladeAwardedMsg);
    }

    public ModdingHandle CreateModdingHandle()
    {
        return new ModdingHandle(this);
    }

    private void OnCreditsAwardedMsg(CreditsAwardedMsg msg)
    {
        if (msg.PlayerId != PlayerId)
        {
            return;
        }

        AdjustCredits(msg.Amount);
    }

    private void OnAccoladeAwardedMsg(AccoladeAwardedMsg msg)
    {
        if (msg.PlayerId != PlayerId)
        {
            return;
        }

        if (ScoredEventManager.instance == null)
        {
            return;
        }

        if (!ScoredEventManager.instance.TryGetAccoladeInfo(msg.AccoladeType, out var accoladeInfo))
        {
            return;
        }

        AdjustCredits(accoladeInfo.Score);
    }

    private void AdjustCredits(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        Credits += amount;

        LocalEvents.Invoke(new ArcadePlayerCreditsChangedData()
        {
            PlayerId = PlayerId,
            NewAmount = Credits,
        });
    }
}
