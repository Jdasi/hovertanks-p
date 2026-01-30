using System;
using System.Collections.Generic;

public static class ThreadManager
{
    private static Queue<Action> _actions = new Queue<Action>();
    private static Queue<Action> _actionsCopy = new Queue<Action>();
    private static bool _isUpdateRequired = false;

    /// <summary>Sets an action to be executed on the main thread.</summary>
    /// <param name="action">The action to be executed on the main thread.</param>
    public static void Invoke(Action action)
    {
        if (action == null)
        {
            Log.Error(LogChannel.Default, "[ThreadManager] Invoke - action was null");
            return;
        }

        lock (_actions)
        {
            _actions.Enqueue(action);
            _isUpdateRequired = true;
        }
    }

    public static void Update()
    {
        if (!_isUpdateRequired)
        {
            return;
        }

        lock (_actions)
        {
            _actionsCopy = new Queue<Action>(_actions);
            _actions.Clear();

            _isUpdateRequired = false;
        }

        foreach (var action in _actionsCopy)
        {
            action();
        }

        _actionsCopy.Clear();
    }
}
