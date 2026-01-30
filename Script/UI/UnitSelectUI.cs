using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class UnitSelectUI : MonoBehaviour
    {
        private enum PointerStates
        {
            Control,
            Lerping
        }

        [Header("Parameters")]
        [SerializeField] FactionPlayablePawnsInfo[] _factionPawnInfos;
        [SerializeField] float _fakePointerSpeed;
        [SerializeField] AnimationCurve _pointerSpeedCurve;

        [Space]
        [SerializeField] ModelBooth _boothPrefab;
        [SerializeField] UnitSelectItem _itemPrefab;

        [Header("Audio")]
        [SerializeField] AudioClip _clipTokenEnter;
        [SerializeField] AudioClip _clipTokenPlace;
        [SerializeField] AudioClip _clipTokenCollect;

        [Header("References")]
        [SerializeField] PlayerSelectionSlot _playerSlotPrefab;
        [SerializeField] FakePointer _fakePointer;
        [SerializeField] SelectToken _selectToken;
        [SerializeField] Image _imgHighlightPointer;
        [SerializeField] Image _imgHighlightToken;
        [SerializeField] RectTransform _itemRoot;
        [SerializeField] Button _btnStart;
        [SerializeField] PlayerSelectionSlot[] _slots;

        private PlayerId _hackyLocalPlayerOneId => GameClient.PlayerIds[0];

        private List<PlayablePawnInfo> _units;
        private List<ModelBooth> _booths;
        private List<UnitSelectItem> _items;

        private PointerStates _pointerState;
        private int _highlightPointerId;
        private int _selectedId;

        private bool _areUnitsEnumerated;
        private bool _areSlotsDirty;

        public void Awake()
        {
            _units = new List<PlayablePawnInfo>();
            _booths = new List<ModelBooth>();
            _items = new List<UnitSelectItem>();

            foreach (var slot in _slots)
            {
                slot.Init();
            }

            if (_factionPawnInfos != null)
            {
                StartCoroutine(EnumerateUnits());
            }

            _selectToken.OnInteraction += OnTokenInteraction;
            _highlightPointerId = -1;
            _selectedId = -1;
        }

        public void OnEnable()
        {
            // create player slots
            foreach (var elem in PlayerManager.ActivePlayers)
            {
                AssignSlotForPlayer(elem.Key);
            }

            _btnStart.interactable = false;
            _btnStart.gameObject.SetActive(Server.IsActive);

            _selectToken.Collect();
            _fakePointer.Init(_hackyLocalPlayerOneId);

            _areSlotsDirty = true;

            NetworkEvents.Subscribe<ClientConnectedMsg>(OnClientConnectedMsg);
            NetworkEvents.Subscribe<ClientDisconnectedMsg>(OnClientDisconnectedMsg);
            NetworkEvents.Subscribe<PawnClassSelectMsg>(OnPawnClassSelectMsg);
        }

        public void OnDisable()
        {
            foreach (var slot in _slots)
            {
                slot.SetUnoccupied();
            }

            NetworkEvents.Unsubscribe<ClientConnectedMsg>(OnClientConnectedMsg);
            NetworkEvents.Unsubscribe<ClientDisconnectedMsg>(OnClientDisconnectedMsg);
            NetworkEvents.Unsubscribe<PawnClassSelectMsg>(OnPawnClassSelectMsg);
        }

        private IEnumerator EnumerateUnits()
        {
            Vector3 spawnPos = new Vector3(0, 0, -200);
            int id = 0;

            foreach (var faction in _factionPawnInfos)
            {
                if (faction.Pawns == null
                    || faction.Pawns.Length == 0)
                {
                    continue;
                }

                foreach (var unit in faction.Pawns)
                {
                    _units.Add(unit);

                    // create model booth for vehicle
                    var booth = Instantiate(_boothPrefab, spawnPos, Quaternion.identity);
                    booth.Init(unit.Model);
                    _booths.Add(booth);

                    // create UI item to display booth output
                    var item = Instantiate(_itemPrefab, _itemRoot);
                    item.Init(id, booth.Texture);
                    item.OnInteraction += OnItemInteraction;
                    _items.Add(item);

                    // prepare for next booth
                    spawnPos += new Vector3(10, 0, 0);

                    ++id;

                    // spread out across frames..
                    yield return null;
                }
            }

            _areUnitsEnumerated = true;
            _areSlotsDirty = true;
        }

        private void OnItemInteraction(int id, UnitSelectItem.Interactions interaction)
        {
            switch (interaction)
            {
                case UnitSelectItem.Interactions.MouseDown:
                case UnitSelectItem.Interactions.ControllerClick:
                {
                    AudioManager.PlayClip(_clipTokenPlace);

                    UpdateSelectedVehicle(id);
                    UpdateUnitDisplay(_hackyLocalPlayerOneId, id);

                    _selectToken.transform.position = _fakePointer.transform.position;
                } break;

                case UnitSelectItem.Interactions.Enter:
                {
                    if (id != _selectedId)
                    {
                        AudioManager.PlayClip(_clipTokenEnter);
                    }

                    _highlightPointerId = id;
                    _imgHighlightPointer.transform.position = _items[id].transform.position;

                    if (!_selectToken.IsPlaced)
                    {
                        UpdateUnitDisplay(_hackyLocalPlayerOneId, id);
                    }
                } break;

                case UnitSelectItem.Interactions.Exit:
                {
                    _highlightPointerId = -1;
                    _imgHighlightPointer.enabled = false;
                    _imgHighlightPointer.transform.position = default;

                    if (!_selectToken.IsPlaced)
                    {
                        UpdateUnitDisplay(_hackyLocalPlayerOneId, -1);
                    }
                } break;
            }
        }

        private void OnTokenInteraction(SelectToken.Interactions interaction)
        {
            switch (interaction)
            {
                // collected by clicking on the token
                case SelectToken.Interactions.Collected:
                {
                    AudioManager.PlayClip(_clipTokenCollect);

                    UpdateSelectedVehicle(-1);
                } break;
            }
        }

        private void UpdateSelectedVehicle(int id)
        {
            if (_pointerState == PointerStates.Lerping)
            {
                return;
            }

            if (_selectedId == id)
            {
                return;
            }

            if (_selectedId >= 0)
            {
                _items[_selectedId].SetSelected(false);
            }

            _selectedId = id;

            // handle valid id (e.g. selecting an item)
            if (id >= 0)
            {
                PawnClass pawnClass = _units[id].Prefab.PawnClass;
                PlayerManager.ChangePawnClass(_hackyLocalPlayerOneId, pawnClass);

                _selectToken.Place(id);

                _imgHighlightToken.transform.position = _items[id].transform.position;
                _imgHighlightToken.enabled = true;

                _items[id].SetSelected(true);
            }
            // handle invalid id (e.g. clearing selection)
            else
            {
                PlayerManager.ChangePawnClass(_hackyLocalPlayerOneId, PawnClass.Invalid);

                _selectToken.Collect();

                _imgHighlightToken.enabled = false;
            }
        }

        private void Update()
        {
            // place token with controller button
            if (GameManager.IsUsingController
                && Input.GetButtonDown("Submit"))
            {
                _fakePointer.Click();
            }

            if (_selectToken.IsPlaced)
            {
                // collect token with controller button / right-click
                if (Input.GetButtonDown("Cancel")
                    ||
                    (!GameManager.IsUsingController
                    && Input.GetMouseButton(1)))
                {
                    if (GameManager.IsUsingController)
                    {
                        _imgHighlightPointer.transform.position = _items[_selectedId].transform.position;
                    }

                    UpdateSelectedVehicle(-1);

                    if (GameManager.IsUsingController)
                    {
                        _pointerState = PointerStates.Lerping;
                    }
                    else
                    {
                        // mouse could be hovering over a new choice
                        UpdateUnitDisplay(_hackyLocalPlayerOneId, _highlightPointerId);
                    }
                }
            }

            if (_btnStart.interactable
                && Input.GetButtonDown("Start"))
            {
                _btnStart.onClick.Invoke();
            }
        }

        private void FixedUpdate()
        {
            /* TODO - change cursor based on if mouse is inside _itemRoot
            Vector2 localMousePosition = _itemRoot.InverseTransformPoint(Input.mousePosition);
            bool isMouseInRoot = _itemRoot.rect.Contains(localMousePosition);
            */

            HandleFakePointer();
            HandlePointerHighlight();
            HandleSelectToken();
            HandleDirtySlots();
            HandleStartButton();
        }

        private void HandleFakePointer()
        {
            if (GameManager.IsUsingController)
            {
                switch (_pointerState)
                {
                    // handle controller movement of fake pointer
                    case PointerStates.Control:
                    {
                        float h = Input.GetAxis("h");
                        float v = Input.GetAxis("v");
                        Vector3 dir =  new Vector3(h, v, 0);

                        _fakePointer.transform.position += dir * _fakePointerSpeed * _pointerSpeedCurve.Evaluate(dir.magnitude) * Time.fixedDeltaTime;
                        _fakePointer.Process();
                    } break;

                    // prevent controller movement of fake pointer until lerp complete
                    case PointerStates.Lerping:
                    {
                        _fakePointer.transform.position = Vector3.Lerp(_fakePointer.transform.position, _selectToken.transform.position, 20 * Time.fixedDeltaTime);

                        // snap when close enough
                        if (Vector3.Distance(_fakePointer.transform.position, _selectToken.transform.position) <= 3)
                        {
                            _fakePointer.transform.position = _selectToken.transform.position;
                            _pointerState = PointerStates.Control;
                        }
                    } break;
                }
            }
            else
            {
                // have fake pointer follow mouse cursor
                _fakePointer.transform.position = Input.mousePosition;
                _pointerState = PointerStates.Control;
            }
        }

        private void HandlePointerHighlight()
        {
            if (_imgHighlightPointer.enabled)
            {
                // hide if invalid position or token placed
                if (_highlightPointerId < 0
                    || _selectToken.IsPlaced)
                {
                    _imgHighlightPointer.enabled = false;
                }
            }
            else
            {
                // show if valid position and token not placed
                if (_highlightPointerId >= 0
                    && !_selectToken.IsPlaced)
                {
                    _imgHighlightPointer.enabled = true;
                }
            }
        }

        private void HandleSelectToken()
        {
            // move token with fake pointer if not placed
            if (!_selectToken.IsPlaced
                && _pointerState != PointerStates.Lerping)
            {
                _selectToken.transform.position = _fakePointer.transform.position;
            }
        }

        private void HandleDirtySlots()
        {
            if (!_areSlotsDirty)
            {
                return;
            }

            if (!_areUnitsEnumerated)
            {
                return;
            }

            foreach (var player in PlayerManager.ActivePlayers)
            {
                if (player.Value.PawnClass == PawnClass.Invalid)
                {
                    continue;
                }

                int index = _units.FindIndex(e => e.Prefab.PawnClass == player.Value.PawnClass);

                if (index < 0)
                {
                    continue;
                }

                UpdateUnitDisplay(player.Key, index);
            }

            _areSlotsDirty = false;
        }

        private void HandleStartButton()
        {
            if (GameManager.fixedFrameCount % 2 != 0)
            {
                return;
            }

            bool allReady = true;

            foreach (var player in PlayerManager.ActivePlayers)
            {
                if (player.Value.PawnClass == PawnClass.Invalid)
                {
                    allReady = false;
                    break;
                }
            }

            if (_btnStart.interactable != allReady)
            {
                _btnStart.interactable = allReady;
            }
        }

        private void UpdateUnitDisplay(PlayerId playerId, int id)
        {
            if (!TryGetSlotForPlayer(playerId, out var slot))
            {
                return;
            }

            if (id >= 0)
            {
                slot.Display(_booths[id], _units[id]);
            }
            else
            {
                slot.ClearDisplayedUnit();
            }
        }

        private void OnClientConnectedMsg(ClientConnectedMsg msg)
        {
            if (msg.ClientId == GameClient.ClientId)
            {
                return;
            }

            AssignSlotForPlayer(msg.PlayerId);
        }

        private void OnClientDisconnectedMsg(ClientDisconnectedMsg msg)
        {
            foreach (var playerId in msg.PlayerIds)
		    {
			    ClearSlotForPlayer(playerId);
		    }
        }

        private void OnPawnClassSelectMsg(PawnClassSelectMsg msg)
        {
            Log.Info(LogChannel.UnitSelectUI, $"OnPawnClassSelectMsg - pid: {msg.PlayerId}, class: {msg.PawnClass}");

            int index = -1;

            if (msg.PawnClass != PawnClass.Invalid)
            {
                index = 0;

                foreach (var unit in _units)
                {
                    if (unit.Prefab.PawnClass != msg.PawnClass)
                    {
                        ++index;
                        continue;
                    }

                    break;
                }
            }

            UpdateUnitDisplay(msg.PlayerId, index);
        }

        private void AssignSlotForPlayer(PlayerId playerId)
        {
            int slotId = -1;

            switch (playerId)
            {
                case PlayerId.One: slotId = 0; break;
                case PlayerId.Two: slotId = 1; break;
                case PlayerId.Three: slotId = 2; break;
                case PlayerId.Four: slotId = 3; break;
            }

            if (slotId < 0)
            {
                Log.Error(LogChannel.UnitSelectUI, $"AssignSlotForPlayer - tried to assign invalid playerId: {playerId}");
                return;
            }

            var slot = _slots[slotId];

            if (slot.IsOccupied)
            {
                Log.Error(LogChannel.UnitSelectUI, $"AssignSlotForPlayer - slot for {playerId} was already occupied");
                return;
            }

            slot.SetOccupied(playerId);
        }

        private void ClearSlotForPlayer(PlayerId playerId)
        {
            if (!TryGetSlotForPlayer(playerId, out var slot))
            {
                return;
            }

            slot.SetUnoccupied();
        }

        private bool TryGetSlotForPlayer(PlayerId playerId, out PlayerSelectionSlot playerSlot)
        {
            playerSlot = null;

            foreach (var slot in _slots)
            {
                if (slot.PlayerId == playerId)
                {
                    playerSlot = slot;
                    return true;
                }
            }

            return false;
        }
    }
}
