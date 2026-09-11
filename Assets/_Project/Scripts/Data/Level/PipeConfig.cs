using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Delta.ProjectName
{
    [Serializable]
    public class PipeConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PipeId Id { get; set; } = PipeId.A;

        public GridCell StartCell { get; set; } = new();

        // Fixed number of cells this pipe may extend into, shown as the white digit on its anchor flange.
        // Can grow at runtime via the +1 Pipe cell/booster; this is only the level's starting budget.
        public int MoveBudget { get; set; }
    }
}
