using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour
{
    public enum ZoomLevel
    {
        Default = 0,
        Close = 50,
        Medium = 35,
    }

    private enum SmartLerpMode
    {
        Pan,
        Zoom
    }

    private class QueuedAction
    {
        public readonly Vector3 PanTarget;
        public readonly float PanSpeed;
        public readonly Vector3 ZoomTarget;
        public readonly float ZoomSpeed;

        public bool FinishedPanning;
        public bool FinishedZooming;

        public QueuedAction(Vector3 panTarget, float panSpeed, Vector3 zoomTarget, float zoomSpeed)
        {
            PanTarget = panTarget;
            PanSpeed = panSpeed;
            ZoomTarget = zoomTarget;
            ZoomSpeed = zoomSpeed;
        }
    }

    public bool IsLerping => _queuedActions.Count > 0;
    public float DefaultPanSpeed => _defaultPanSpeed;
    public float DefaultZoomSpeed => _defaultZoomSpeed;

    [Header("Parameters")]
    [SerializeField] float _defaultPanSpeed;
    [SerializeField] float _defaultZoomSpeed;

    [Header("References")]
    [SerializeField] Transform _zoomRoot;

    private Vector3 _cameraStartZoom;
    private Queue<QueuedAction> _queuedActions;

    public void SetPosition(Vector3 pos)
    {
        pos.y = 0;
        transform.position = pos;

        _queuedActions.Clear();
    }

    public void SetZoomLevel(ZoomLevel zoom)
    {
        _zoomRoot.localPosition = CalculateZoomTarget(zoom);
    }

    public void QueueAction(Vector3 panTarget, ZoomLevel zoomTarget = ZoomLevel.Default, float customPanSpeed = -1, float customZoomSpeed = -1)
    {
        // flatten
        panTarget.y = 0;

        // determine speeds
        float panSpeedToUse = customPanSpeed > 0 ? customPanSpeed : _defaultPanSpeed;
        float zoomSpeedTouse = customZoomSpeed > 0 ? customZoomSpeed : _defaultZoomSpeed;

        // queue
        _queuedActions.Enqueue(new QueuedAction(panTarget, panSpeedToUse, CalculateZoomTarget(zoomTarget), zoomSpeedTouse));
    }

    private void Awake()
    {
        _cameraStartZoom = new Vector3(0, _zoomRoot.localPosition.y, _zoomRoot.localPosition.z);
        _queuedActions = new Queue<QueuedAction>();
    }

    private void Update()
    {
        if (Time.timeScale == 0)
        {
            return;
        }

        if (_queuedActions.Count == 0)
        {
            return;
        }

        var move = _queuedActions.Peek();

        if (!move.FinishedPanning)
        {
            HandlePan(move.PanTarget, move.PanSpeed, ref move.FinishedPanning);
        }

        if (!move.FinishedZooming)
        {
            HandleZoom(move.ZoomTarget, move.ZoomSpeed, ref move.FinishedZooming);
        }

        // remove finished action
        if (move.FinishedPanning && move.FinishedZooming)
        {
            _queuedActions.Dequeue();
        }
    }

    private void HandlePan(Vector3 target, float speed, ref bool isFinished)
    {
        Vector3 step = (target - transform.position).normalized * speed * Time.unscaledDeltaTime;
        SmartLerp(SmartLerpMode.Pan, transform, step, target, ref isFinished);
    }

    private void HandleZoom(Vector3 target, float speed, ref bool isFinished)
    {
        Vector3 step = Vector3.Lerp(_zoomRoot.localPosition, target, speed * Time.unscaledDeltaTime);
        SmartLerp(SmartLerpMode.Zoom, _zoomRoot, step, target, ref isFinished);
    }

    private void SmartLerp(SmartLerpMode mode, Transform root, Vector3 step, Vector3 target, ref bool isFinished)
    {
        float dist = Vector3.Distance(root.localPosition, target);

        // move to target
        switch (mode)
        {
            case SmartLerpMode.Pan:
            {
                float stepLength = step.magnitude;

                if (dist > 0 && stepLength < dist)
                {
                    root.localPosition += step;
                }
                else
                {
                    isFinished = true;
                }
            } break;

            case SmartLerpMode.Zoom:
            {
                float stepLength = (root.localPosition - step).magnitude;

                if (stepLength > 1.25f)
                {
                    root.localPosition = step;
                }
                else
                {
                    isFinished = true;
                }
            } break;
        }

        // snap to target
        if (isFinished)
        {
            root.localPosition = target;
        }
    }

    private Vector3 CalculateZoomTarget(ZoomLevel zoom)
    {
        return _cameraStartZoom + (_zoomRoot.forward * ((float)zoom / 10f) * 1000);
    }
}
