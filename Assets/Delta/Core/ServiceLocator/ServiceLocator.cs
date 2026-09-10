using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _registry = new();

        public static void Register<T>(T service) where T : class
        {
            _registry[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_registry.TryGetValue(type, out var service))
                return (T)service;

            throw new Exception($"[ServiceLocator] Service of type {type} not found. Make sure it is registered before use.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_registry.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        public static void Unregister<T>() where T : class
        {
            if (!_registry.Remove(typeof(T)))
                Debug.LogWarning($"[ServiceLocator] Attempted to unregister {typeof(T)} which is not registered.");
        }

        public static void ClearAll() => _registry.Clear();
    }
}
