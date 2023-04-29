using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using V2;

// Editor tool for creating patterns from curves and serializing them to json
public class PatternCreator : MonoBehaviour
{
    public string PatternName;
    public AnimationCurve Curve;

    [ContextMenu("Create Pattern From Curve")]
    public void CreatePatternFromCurve()
    {
        CurveToPattern(Curve);
    }

    public void CurveToPattern(AnimationCurve curve)
    {
        var actionsList = new NativeList<FunAction>(100, Allocator.Temp);

        int lengthInMilliseconds = (int)math.round(curve.keys[curve.length - 1].time * 1000f);

        int prevPos = 0;
        for (int t = 0; t < lengthInMilliseconds; t++)
        {
            float value = curve.Evaluate(t * 0.001f) * 100f;
            //int pos = Mathf.RoundToInt(value / 5f) * 5;
            int pos = Mathf.RoundToInt(value);
            pos = Mathf.Clamp(pos, 0, 100);

            // Add value at start
            if (t == 0)
            {
                actionsList.Add(new FunAction { at = 0, pos = pos });
                prevPos = pos;
            }
            else if (prevPos != pos)
            {
                // Also add value at previous at.. ?
                actionsList.Add(new FunAction { at = t - 1, pos = prevPos });
                actionsList.Add(new FunAction { at = t, pos = pos });
                prevPos = pos;
            }
            else if (t == lengthInMilliseconds - 1)
            {
                // Add value at end
                actionsList.Add(new FunAction { at = t, pos = pos });
                prevPos = pos;
            }
        }

        // Write to json
        FunAction[] actions = new FunAction[actionsList.Length];
        for (int i = 0; i < actionsList.Length; i++)
        {
            actions[i] = actionsList[i];
        }

        var pattern = new Pattern { name = PatternName, actions = actions };
        string path = $"{Application.streamingAssetsPath}\\Patterns\\{PatternName}.json";

        File.WriteAllText(path, JsonUtility.ToJson(pattern));
        Debug.Log($"pattern saved: '{path}'");

        actionsList.Dispose();
    }
}

[Serializable]
public struct Pattern
{
    public string name;
    public FunAction[] actions;
}

public struct PatternData : IBufferElementData
{
    public int Index;
    public NativeArray<FunAction> actions;
}