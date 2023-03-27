using Unity.Entities;
using Unity.Mathematics;

public struct FunScriptPatternModeSettings : IComponentData
{
    public int PatternIndex;
    public int PatternCount;
    public int Repeat;
    public float2 Scale;
    public int Spacing;
}