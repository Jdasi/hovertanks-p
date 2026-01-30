using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
	private enum States
	{
		Waiting,
		Moving,
	}

	private enum Direction
	{
		Backwards,
		Forwards,
	}

	[SerializeField] float _speed;
	[SerializeField] float _waitTime;
	[SerializeField] List<Vector3> _waypoints;

	private States _state;
	private Direction _direction;
	private float _waitTimer;
	private float _progress;

	private int _targetWaypointIndex;
	private Vector3 _targetWaypoint;

	private void Start()
	{
		if (_waypoints == null
			|| _waypoints.Count == 0)
		{
			_waypoints = null;
			return;
		}

		// current position is the last waypoint
		_waypoints.Add(transform.position);
		_targetWaypointIndex = _waypoints.Count;

		OnDestinationReached();
		TriggerWait();
	}

	private void FixedUpdate()
	{
		if (_waypoints == null)
		{
			return;
		}

		switch (_state)
		{
			case States.Waiting:
			{
				_waitTimer -= Time.fixedDeltaTime;

				if (_waitTimer <= 0)
				{
					_state = States.Moving;
				}
			} break;

			case States.Moving:
			{
				_progress += Time.fixedDeltaTime * _speed;
				Vector3 step = Vector3.Lerp(transform.position, _targetWaypoint, _progress);

				transform.position = step;

				if (Vector3.Distance(transform.position, _targetWaypoint) < 0.01f)
				{
					transform.position = _targetWaypoint;
					OnDestinationReached();
				}
			} break;
		}
	}

	private void OnDestinationReached()
	{
		if (_direction == Direction.Forwards)
		{
			++_targetWaypointIndex;

			if (_targetWaypointIndex >= _waypoints.Count)
			{
				_targetWaypointIndex = _waypoints.Count - 1;
				_direction = Direction.Backwards;

				TriggerWait();
			}
		}
		else if (_direction == Direction.Backwards)
		{
			--_targetWaypointIndex;

			if (_targetWaypointIndex < 0)
			{
				_targetWaypointIndex = 1;
				_direction = Direction.Forwards;

				TriggerWait();
			}
		}

		UpdateTargetWaypoint();
	}

	private void UpdateTargetWaypoint()
	{
		_targetWaypoint = _waypoints[_targetWaypointIndex];
		_progress = 0;
	}

	private void TriggerWait()
	{
		_waitTimer = _waitTime;
		_state = States.Waiting;
	}

	private void OnTriggerEnter(Collider other)
	{
		AttachToThis(other, true);
	}

	private void OnTriggerExit(Collider other)
	{
		AttachToThis(other, false);
	}

	private void AttachToThis(Collider other, bool attach)
	{
		Transform transformToUse = other.transform;

		if (other.transform.parent != null)
		{
			transformToUse = other.transform.parent;
		}

		transformToUse.SetParent(attach ? this.transform : null);
	}
}
