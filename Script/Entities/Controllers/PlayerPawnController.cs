using HoverTanks.Events;
using HoverTanks.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Entities
{
    [CreateAssetMenu(menuName = "Controllers/Player Pawn Controller")]
    public class PlayerPawnController : PawnController
    {
        private class InteractContext
        {
            public int Uid;
            public Action<PlayerId> Callback;
            public string Description;
        }

        [SerializeField] LayerMask _floorMask;
        [SerializeField] CrosshairUI _crosshairUIPrefab;

        [Header("Controller Aiming")]
        [SerializeField] float _aimSpeed;
        [SerializeField] float _delayBeforeLerp;
        [SerializeField] float _lerpBackSpeed;
        [SerializeField] float _distBeforeLerpHide;
        [SerializeField] AnimationCurve _aimFactorCurve;
        [SerializeField] Bounds _crosshairBounds;

        private Transform _crosshairRoot;
        private CrosshairUI _crosshair;

        private Vector3 _controllerCrosshairAimPos { get => _crosshairRoot.position; set => _crosshairRoot.position = value; }
        private float _timeBeforeAimLerp;
        private bool _isUsingController;

        private List<InteractContext> _interactContexts;

        protected override void OnInit()
        {
            _interactContexts = new List<InteractContext>();

            // create crosshair
            _crosshairRoot = new GameObject("PlayerCrosshair").transform;
            _crosshair = Instantiate(_crosshairUIPrefab, _crosshairRoot);
            _crosshair.Init(Pawn);

            // local events
            LocalEvents.Subscribe<AddInteractContextData>(OnAddInteractContext);
            LocalEvents.Subscribe<RemoveInteractContextData>(OnRemoveInteractContext);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // cleanup crosshair
            if (_crosshair != null)
            {
                Destroy(_crosshairRoot.gameObject);
            }

            _interactContexts.Clear();
            NotifyInteractContextChange();

            // local events
            LocalEvents.Unsubscribe<AddInteractContextData>(OnAddInteractContext);
            LocalEvents.Unsubscribe<RemoveInteractContextData>(OnRemoveInteractContext);
        }

        protected override void OnUpdate()
        {
            // moving
            float h = Input.GetAxis("h");
            float v = Input.GetAxis("v");

            if (h == 0)
            {
                // use keyboard horizontal
                h = Input.GetAxis("kh");
            }

            if (v == 0)
            {
                // use keyboard vertical
                v = Input.GetAxis("kv");
            }

            Pawn.Move(h, v);

            // mouse aiming
            if (Input.GetButton("Aim"))
            {
                _isUsingController = false;

                Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		        RaycastHit floorHit;

		        if (Physics.Raycast(camRay, out floorHit, 15000, _floorMask))
                {
                    UpdateCrosshair(true, floorHit.point);
                    Pawn.StartAiming(floorHit.point);
		        }
            }
            // controller aiming
            else
            {
                float lh = Input.GetAxis("lh");
                float lv = Input.GetAxis("lv");
                bool holdingAimButton = Input.GetAxis("Aim") > 0.1f;

                if (Mathf.Abs(lh) >= 0.05f || Mathf.Abs(lv) >= 0.05f
                    || holdingAimButton)
                {
                    _isUsingController = true;

                    Vector3 dir = new Vector3(lh, 0, lv);

                    if (holdingAimButton)
                    {
                        _timeBeforeAimLerp = Time.unscaledTime + _delayBeforeLerp;

                        // configure when first shown
                        if (!_crosshair.IsShowing)
                        {
                            _controllerCrosshairAimPos = Pawn.Position + dir * 3;
                        }

                        _controllerCrosshairAimPos += dir * _aimSpeed * _aimFactorCurve.Evaluate(dir.magnitude) * Time.deltaTime;

                        _crosshairBounds.center = Vector3.zero; // TODO - make dynamic
                        if (!_crosshairBounds.Contains(_controllerCrosshairAimPos))
                        {
                            _controllerCrosshairAimPos = _crosshairBounds.ClosestPoint(_controllerCrosshairAimPos);
                        }

                        UpdateCrosshair(true, _controllerCrosshairAimPos);
                        Pawn.StartAiming(_controllerCrosshairAimPos);
                    }
                    else
                    {
                        UpdateCrosshair(false, _controllerCrosshairAimPos);
                        Pawn.StartAiming(Pawn.Position + dir);
                    }
                }
                else
                {
                    if (_isUsingController)
                    {
                        UpdateCrosshair(false, _controllerCrosshairAimPos);
                    }
                    else
                    {
                        UpdateCrosshair(false);
                    }

                    Pawn.StopAiming();
                }
            }

            // shooting
            if (Input.GetButtonDown("Fire")
                || Input.GetAxis("Fire") > 0.1f)
            {
                Pawn.Shoot(_crosshair.transform.position);
            }
            else
            {
                Pawn.StopShoot();
            }

            // reloading
            if (Input.GetButton("Reload"))
            {
                Pawn.Reload();
            }
            else
            {
                Pawn.StopReload();
            }

            // module activation
            if (Input.GetButton("Module"))
            {
                Pawn.StartModule();
            }
            else
            {
                Pawn.StopModule();
            }

            // turbo
            if (Input.GetButton("Turbo"))
            {
                Pawn.StartTurbo();
            }
            else
            {
                Pawn.StopTurbo();
            }

            // interact
            if (Input.GetButtonDown("Interact"))
            {
                TryInteract();
            }
        }

        protected override void OnDisabled()
        {
            _crosshair.Hide();
        }

        private void UpdateCrosshair(bool show, Vector3 aimPos = default)
        {
            if (_crosshair == null)
            {
                return;
            }

            if (show && !_crosshair.IsShowing)
            {
                _crosshair.Show();
            }
            else if (!show && _crosshair.IsShowing)
            {
                if (_isUsingController)
                {
                    if (Time.unscaledTime >= _timeBeforeAimLerp)
                    {
                        _controllerCrosshairAimPos = Vector3.Lerp(_controllerCrosshairAimPos, Pawn.Position, Time.unscaledDeltaTime * _lerpBackSpeed);
                    }

                    if (Vector3.Distance(Pawn.Position, _controllerCrosshairAimPos) <= _distBeforeLerpHide)
                    {
                        _controllerCrosshairAimPos = Pawn.Position;
                        _crosshair.Hide();
                    }
                }
                else
                {
                    _crosshair.Hide();
                }
            }

            // position crosshair
            _crosshair.transform.position = aimPos;
        }

        private void TryInteract()
        {
            if (_interactContexts.Count == 0)
            {
                return;
            }

            var last = _interactContexts[_interactContexts.Count - 1];
            last.Callback?.Invoke(Pawn.identity.playerId);
        }

        private void OnAddInteractContext(AddInteractContextData data)
        {
            if (Pawn.identity.playerId != data.PlayerId)
            {
                return;
            }

            var index = _interactContexts.FindIndex(elem => elem.Uid == data.Uid);

            // dont add dupes
            if (index >= 0)
            {
                return;
            }

            _interactContexts.Add(new InteractContext()
            {
                Uid = data.Uid,
                Callback = data.Callback,
                Description = data.Description,
            });

            NotifyInteractContextChange();
        }

        private void OnRemoveInteractContext(RemoveInteractContextData data)
        {
            if (Pawn.identity.playerId != data.PlayerId)
            {
                return;
            }

            var index = _interactContexts.FindIndex(elem => elem.Uid == data.Uid);

            // nothing to remove
            if (index < 0)
            {
                return;
            }

            bool wasPreviousActiveContext = index == _interactContexts.Count - 1;
            _interactContexts.RemoveAt(index);

            if (wasPreviousActiveContext)
            {
                NotifyInteractContextChange();
            }
        }

        private void NotifyInteractContextChange()
        {
            string description = _interactContexts.Count > 0
                ? _interactContexts[_interactContexts.Count - 1].Description
                : "";

            LocalEvents.Invoke(new InteractContextChangedData()
            {
                PlayerId = Pawn.identity.playerId,
                Description = description,
            });
        }
    }
}
