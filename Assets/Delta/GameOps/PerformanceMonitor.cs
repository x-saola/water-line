using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace Delta.GameOps
{
    public static class PerformanceMonitor
    {
#if UNITY_IOS
        public static bool TraceEnabled = false;
#endif
        public static void StartPMTrace(string name)
        {

        }

        public static void StopPMTrace(string name)
        {

        }

        public static void SetPMTraceAttribute(string name, string attribute, string value)
        {

        }

        public static void IncrementPMTraceMetric(string name, string metric, int value)
        {

        }
    }
}
