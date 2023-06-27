using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public enum LayoutCondition
{
    Regular,
    Irregular,
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