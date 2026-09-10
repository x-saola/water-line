using System.Collections.Generic;
using Delta.GameOps;

namespace Delta.Services
{
    public class RemoteConfigService
    {
        private readonly Dictionary<string, IConfigValue> _cachedConfigs = new();

        public IConfigValue GetConfigValue(string key)
        {
            if (_cachedConfigs.TryGetValue(key, out var value))
            {
                return value;
            }

            value = DeltaApp.Instance.RemoteConfigsManager.GetValue(key);
            _cachedConfigs[key] = value;

            return value;
        }
    }
}
