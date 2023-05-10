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

    public enum AlignmentCondition
    {
        AlignWithFurniture,
        NotAlignWithFurniture,
        NULL
    }

    public enum FurnitureCondition
    {
        HasFurniture,
        NoFurniture,
        NULL
    }

    public enum SetupParameter { 
        ExperimentNumber,
        TrialNumber
    }
}