public static class FirebaseUtils
{
    public struct ConfigValue
    {
        public bool BooleanValue { get; }
        public double DoubleValue { get; }
        public long LongValue { get; }
        public string StringValue { get; }
    }

    public static ConfigValue GetRemoteConfigValue(string key)
    {
        return new ConfigValue();
    }
}
