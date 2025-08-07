using System.Collections.Generic;
using UnityEngine;

public enum StepType
{
    PermanentDestroy,
    AnyN
}

[System.Serializable]
public class PhaseStep
{
    public StepType stepType;
    public List<ObjectChecker> objects;
    public int requiredCount = 1; // AllNÀÏ ¶§ n°ª
}

