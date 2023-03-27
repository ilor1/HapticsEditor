using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace V2
{
    [UpdateAfter(typeof(AudioUpdateSystem))]
    [UpdateAfter(typeof(FunActionInputSystem))]
    public partial class PatternStampRendererSystem : SystemBase
    {
        private EntityQuery _funActionCoordQuery;
        private NativeList<float2> _positions;
        private bool _generateVisualContentCallBackAdded = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            _positions = new NativeList<float2>(1000, Allocator.Persistent);

            RequireForUpdate<AudioPlaybackTime>();

            // Create patternColorEntity
            Entity patternColorEntity = EntityManager.CreateEntity();
            ColorUtility.TryParseHtmlString("#731064", out var lineColor);
            EntityManager.AddComponentData(patternColorEntity, new PatternLineColor()
            {
                Value = lineColor
            });
            ColorUtility.TryParseHtmlString("#5B2173", out var pointColor);
            EntityManager.AddComponentData(patternColorEntity, new PatternPointColor()
            {
                Value = pointColor
            });
#if UNITY_EDITOR
            EntityManager.AddComponentData(patternColorEntity, new EntityName
            {
                Value = new FixedString64Bytes("PatternColorSettings")
            });
#endif
        }

        protected override void OnUpdate()
        {
            if (!_generateVisualContentCallBackAdded)
            {
                // Create FunScriptRenderer if it doesn't exist.
                foreach (var visualElementRef in SystemAPI.Query<VisualElementRef>().WithAll<PatternStamp>())
                {
                    // Turn the FunScript container into FunScript LineRenderer
                    visualElementRef.Value.generateVisualContent += DrawLine;
                    _generateVisualContentCallBackAdded = true;
                }
            }

            var audioPlaybackTime = SystemAPI.GetSingleton<AudioPlaybackTime>();
            var modeSelection = SystemAPI.GetSingleton<FunScriptModeSelection>();
            var patternSettings = SystemAPI.GetSingleton<FunScriptPatternModeSettings>();

            // Repaint
            foreach (var (visualElementRef, stamp) in SystemAPI.Query<VisualElementRef, RefRO<PatternStamp>>())
            {
                if (modeSelection.Value == 0)
                {
                    _positions.Clear();
                    visualElementRef.Value.MarkDirtyRepaint();
                    visualElementRef.Value.style.display = DisplayStyle.None;
                }
                else
                {
                    visualElementRef.Value.style.display = DisplayStyle.Flex;
                    _positions.Clear();
                    var mousePosition = Input.mousePosition;
                    Vector2 mousePositionCorrected = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
                    var mousePosOnPanel = RuntimePanelUtils.ScreenToPanel(visualElementRef.Value.panel, mousePositionCorrected);
                    mousePosOnPanel.y -= visualElementRef.Value.worldBound.position.y;

                    GetAtAndPos(mousePosOnPanel, visualElementRef.Value, SystemAPI.GetSingleton<AudioPlaybackTime>(), out int mouseAt, out int mousePos);

                    // Get active pattern
                    var pattern = SystemAPI.GetSingletonBuffer<PatternData>()[patternSettings.PatternIndex];
                    int length = (int)math.round(pattern.actions[pattern.actions.Length - 1].at * patternSettings.Scale.x + patternSettings.Spacing);

                    // paste the pattern
                    var cleanedActions = new NativeList<FunAction>(1000, Allocator.Temp);

                    int index = 0;
                    for (int i = 0; i < patternSettings.Repeat; i++)
                    {
                        for (int j = 0; j < pattern.actions.Length; j++)
                        {
                            var action = pattern.actions[j];
                            int at = mouseAt + length * i + (int)math.round(action.at * patternSettings.Scale.x);
                            int pos = (int)math.round(action.pos * patternSettings.Scale.y);
                            pos = (int)(math.round(mousePos / 5f) * 5f + math.round(pos / 5f) * 5f);

                            pos = math.min(pos, 100);
                            pos = math.max(pos, 0);

                            // if there's three consecutive identical pos values, remove the middle one
                            if (index >= 2 && pos == cleanedActions[index - 1].pos && pos == cleanedActions[index - 2].pos)
                            {
                                cleanedActions.RemoveAt(index - 1);
                            }
                            else
                            {
                                index++;
                            }

                            cleanedActions.Add(new FunAction { at = at, pos = pos });
                        }
                    }

                    var activeFunActions = new NativeList<FunAction>(1000, Allocator.TempJob);

                    var job = new ConvertFunActionsToCoordinatesJob
                    {
                        Width = visualElementRef.Value.contentRect.width,
                        Height = visualElementRef.Value.contentRect.height,
                        StartTimeInMilliseconds = audioPlaybackTime.TimeInMilliseconds - 8000,
                        EndTimeInMilliseconds = audioPlaybackTime.TimeInMilliseconds + 8000,
                        WidthInMilliseconds = 16000,
                        FunActions = cleanedActions.AsArray(),
                        FunCoords = _positions,
                        ActiveFunActions = activeFunActions
                    };
                    job.Schedule().Complete();
                    activeFunActions.Dispose();

                    visualElementRef.Value.MarkDirtyRepaint();
                }
            }
        }

        private static void GetAtAndPos(Vector2 localPosition, VisualElement visualElement, AudioPlaybackTime audioPlayback, out int at, out int pos)
        {
            at = -1;
            pos = -1;
            float relativeX = localPosition.x / visualElement.contentRect.width;
            float start = (audioPlayback.TimeInMilliseconds - 8000);
            at = (int)(start + relativeX * 16000);
            float relativeY = (visualElement.contentRect.height - localPosition.y) / visualElement.contentRect.height;
            pos = (int)(relativeY * 100);
        }

        private void DrawLine(MeshGenerationContext mgc)
        {
            // Draw
            var painter = mgc.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = SystemAPI.GetSingleton<PatternLineColor>().Value;
            painter.fillColor = SystemAPI.GetSingleton<PatternPointColor>().Value;
            painter.lineWidth = 6f;
            painter.BeginPath();

            // Draw line
            if (_positions.IsCreated && _positions.Length > 0)
            {
                painter.MoveTo(_positions[0]);
                for (int i = 1; i < _positions.Length; i++)
                {
                    painter.LineTo(_positions[i]);
                }
            }

            painter.Stroke();

            // Draw points
            if (_positions.IsCreated && _positions.Length > 0)
            {
                for (int i = 0; i < _positions.Length; i++)
                {
                    painter.BeginPath();
                    painter.Arc(_positions[i], 8.0f, 0.0f, 360.0f);
                    painter.Fill();
                }
            }
        }
    }
}