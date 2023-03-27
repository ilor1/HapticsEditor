using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace V2
{
    public struct ConvertFunActionsToCoordinatesJob : IJob
    {
        [ReadOnly] public float Width;
        [ReadOnly] public float Height;
        [ReadOnly] public int StartTimeInMilliseconds;
        [ReadOnly] public int EndTimeInMilliseconds;
        [ReadOnly] public int WidthInMilliseconds;
        [ReadOnly] public NativeArray<FunAction> FunActions;

        [WriteOnly] public NativeList<float2> FunCoords;
        [WriteOnly] public NativeList<FunAction> ActiveFunActions;

        public void Execute()
        {
            bool foundFirst = false;
            FunAction first = new FunAction();

            for (int i = 0; i < FunActions.Length; i++)
            {
                var funAction = FunActions[i];

                // all the points are before current placement. Render the last point
                if (!foundFirst && i == FunActions.Length - 1)
                {
                    first = FunActions[i];
                }

                if (funAction.at < StartTimeInMilliseconds) continue;

                if (!foundFirst && i > 0)
                {
                    ActiveFunActions.Add(FunActions[i - 1]);
                    AddFunAction(FunActions[i - 1]);
                    foundFirst = true;
                }

                ActiveFunActions.Add(funAction);
                AddFunAction(funAction);

                if (funAction.at > EndTimeInMilliseconds)
                {
                    break;
                }
            }

            if (!foundFirst)
            {
                ActiveFunActions.Add(first);
                AddFunAction(first);
            }
        }

        private void AddFunAction(FunAction funAction)
        {
            // x
            float relativeAt = (funAction.at - StartTimeInMilliseconds) / (float)(WidthInMilliseconds);
            float x = relativeAt * Width;

            // y
            float relativePos = math.abs(funAction.pos / 100f - 1);
            float y = relativePos * Height;

            FunCoords.Add(new float2(x, y));
        }
    }
}