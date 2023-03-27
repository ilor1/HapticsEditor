using Unity.Entities;

public struct FunScriptModeSelection : IComponentData
{
    // 0 = point mode
    // 1 = pattern mode
    public int Value; 
}