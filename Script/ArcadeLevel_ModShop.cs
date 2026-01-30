using HoverTanks.Events;
using HoverTanks.Loadouts;
using LitJson;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcadeLevel_ModShop : ArcadeLevel_Base
{
    [SerializeField] Transform[] _playerSpawns;
    [SerializeField] ArcadeLevelPortal _exitPortal;

    [Header("Shop Floor")]
    [SerializeField] ItemKiosk[] _kiosks;
    [SerializeField] ItemStand[] _stands;
    [SerializeField] UpgradeBay[] _bays;

    private Module[] _modules;
    private Augment[] _augments;

    private Dictionary<string, ShopInfo> _shopInfo;

    protected override Transform[] PlayerSpawns
    {
        get => _playerSpawns;
        set => _playerSpawns = value;
    }

    protected override bool TryInit()
    {
        // set intial camera pos
        var avgSpawnPos = PlayerSpawns.Select(spawn => spawn.position).ToArray().GetAveragePosition();
        GameCamera.SetPosition(avgSpawnPos + EntryDir.DirectionToVector() * 3);
        GameCamera.SetZoomLevel(GameCamera.ZoomLevel.Medium);

        _modules = Resources.LoadAll<Module>("Modules");
        _augments = Resources.LoadAll<Augment>("Augments");

        EnumerateShopInfo();
        PopulateKiosks();
        //InitUpgradeBays();

        _exitPortal.PlayerInteracted += PlayerExtractedViaPortal;

        return true;
    }

    protected override void OnStartedPlaying()
    {
        LocalEvents.Invoke(new ArcadeModShopStartedData()
        {
            Player1Credits = PlayerQueries.GetPlayerCredits(PlayerId.One),
            Player2Credits = PlayerQueries.GetPlayerCredits(PlayerId.Two),
            Player3Credits = PlayerQueries.GetPlayerCredits(PlayerId.Three),
            Player4Credits = PlayerQueries.GetPlayerCredits(PlayerId.Four),
        });
    }

    protected override void RunningOnFixedUpdate()
    {
        if (GameManager.fixedFrameCount % 5 != 0)
        {
            return;
        }

        // check extracted states
        foreach (var data in PlayerDatas.Values)
        {
            if (data.IsPawnAlive
                && !data.IsPawnExtracted)
            {
                return;
            }
        }

        Result = ArcadeLevelResult.ProgressFromModShop;
    }

    private void EnumerateShopInfo()
    {
        _shopInfo = new Dictionary<string, ShopInfo>();

        var file = Resources.Load<TextAsset>("shopinfo");
        JsonData data = JsonMapper.ToObject(file.text);

        EnumerateItems(data["Weapons"]);
        EnumerateItems(data["Modules"]);
        EnumerateItems(data["Augments"]);
    }

    private void EnumerateItems(JsonData root)
    {
        for (int i = 0; i < root.Count; ++i)
        {
            JsonData data = root[i];
            string @class = data["Class"].ToString();

            if (!data.TryGetValue("DisplayName", out var displayName))
            {
                displayName = @class.SpaceOut();
            }

            if (!data.TryGetValue("Description", out var description))
            {
                description = string.Empty;
            }

            if (!data.TryGetValue("Credits", out var creditCost))
            {
                creditCost = "0";
            }

            if (!data.TryGetValue("Reactor", out var reactorCost))
            {
                reactorCost = "0";
            }

            _shopInfo[@class] = new ShopInfo(displayName, description, uint.Parse(creditCost), uint.Parse(reactorCost));
        }
    }

    private void PopulateKiosks()
    {
        var moduleOptions = _modules.Select(elem => elem.ModuleClass.ToString())
            .Where(elem => _modules.FirstOrDefault(module => module.ModuleClass.ToString().Equals(elem)) != null)
            .ToList();

        var augmentOptions = _augments.Select(elem => elem.AugmentClass.ToString())
            .Where(elem => _augments.FirstOrDefault(augment => augment.AugmentClass.ToString().Equals(elem)) != null)
            .ToList();

        for (int i = 0; i < _stands.Length; ++i)
        {
            ItemStand stand = _stands[i];
            string className;

            switch (stand.Type)
            {
                case EquipmentType.Module:
                {
                    className = moduleOptions.SelectRandom();
                    moduleOptions.Remove(className);
                } break;

                case EquipmentType.Augment:
                {
                    className = augmentOptions.SelectRandom();
                    augmentOptions.Remove(className);
                } break;

                default: continue;
            }

            if (string.IsNullOrEmpty(className))
            {
                Log.Warning(LogChannel.ModShop, $"PopulateStands - className was null in list for type: {stand.Type}");
                continue;
            }

            if (!_shopInfo.TryGetValue(className, out var shopInfo))
            {
                continue;
            }

            stand.Init(shopInfo);
        }
    }

    /*
    private void InitUpgradeBays()
    {
        foreach (var bay in _bays)
        {
            bay.Init(this);
        }
    }
    */
}
