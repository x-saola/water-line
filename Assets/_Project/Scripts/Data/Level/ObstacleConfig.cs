using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Delta.ProjectName
{
    [Serializable]
    public class ObstacleConfig
    {
        public GridCell Cell { get; set; } = new();

        [JsonConverter(typeof(StringEnumConverter))]
        public ObstacleType Type { get; set; } = ObstacleType.Rock;
    }
}
