#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.Core
{
    public static class EventBusCleaner
    {
        static readonly List<Action> _clearActions = new();

        internal static void RegisterClear(Action clearAction) => _clearActions.Add(clearAction);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearAll()
        {
            foreach (var clear in _clearActions)
                clear();
        }
    }
    
    public static class EventBus<T> where T : struct
    {
        static readonly HashSet<IEventBinding<T>> _bindings = new();
        static readonly List<IEventBinding<T>> _snapshot = new();
        static bool _dirty = true;

        static EventBus() => EventBusCleaner.RegisterClear(ClearState);

        static void ClearState()
        {
            _bindings.Clear();
            _snapshot.Clear();
            _dirty = true;
        }

        public static void Register(EventBinding<T> binding)
        {
            if (_bindings.Add(binding))
                _dirty = true;
        }

        public static void Deregister(EventBinding<T> binding)
        {
            if (_bindings.Remove(binding))
                _dirty = true;
        }

        public static void Raise(T @event)
        {
            if (_dirty)
            {
                _snapshot.Clear();
                _snapshot.AddRange(_bindings);
                _dirty = false;
            }

            foreach (var binding in _snapshot)
            {
                binding.OnEvent?.Invoke(@event);
                binding.OnEventNoArgs?.Invoke();
            }
        }

        public static int BindingCount => _bindings.Count;
    }
}
