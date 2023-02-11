using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialMemoryTest
{
    public enum GameState
    {
        Prepare,
        Learning,
        Distractor,
        Recall,
        Result,
        Break,
        NULL
    }

    public enum Layout
    {
        Flat,
        Wraparound,
        NULL
    }

    public enum PhysicalEnvironmentDependence
    {
        High,
        Low,
        NULL
    }
}