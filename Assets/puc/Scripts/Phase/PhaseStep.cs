using System.Collections.Generic;
using UnityEngine;

public enum StepType
{
    PermanentDestroy, // 새로운 타입
    AllOnce,
    AllN,
}

[System.Serializable]
public class PhaseStep
{
    public StepType stepType;
    public List<ObjectChecker> objects;
    public int requiredCount = 1; // AllN일 때 n값
}

