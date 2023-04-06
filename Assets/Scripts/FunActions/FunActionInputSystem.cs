using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace V2
{
    public partial class FunActionInputSystem : SystemBase
    {
        private EntityQuery _funActionQuery;
        private ComponentTypeHandle<FunAction> _funActionTypeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            _funActionQuery = GetEntityQuery(ComponentType.ReadOnly<FunAction>());
            _funActionTypeHandle = GetComponentTypeHandle<FunAction>(true);

            Entity pointSettingsEntity = EntityManager.CreateEntity();
#if UNITY_EDITOR
            EntityManager.AddComponentData(pointSettingsEntity, new EntityName
            {
                Value = new FixedString64Bytes("FunScriptPointModeSettings")
            });
#endif
            EntityManager.AddComponentData(pointSettingsEntity, new FunScriptPointModeSettings
            {
                StepMode = false,
                Snapping = true
            });
            EntityManager.AddComponentData(pointSettingsEntity, new FunScriptModeSelection
            {
                Value = 0
            });


            // Create patterns
            var patternFiles = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "Patterns"), "*.json");
            int index = 0;


            var patternEntity = EntityManager.CreateEntity();
#if UNITY_EDITOR
            EntityManager.AddComponentData(patternEntity, new EntityName
            {
                Value = new FixedString64Bytes("PatternEntity")
            });
#endif
            var buffer = EntityManager.AddBuffer<PatternData>(patternEntity);
            foreach (var json in patternFiles)
            {
                var pattern = JsonUtility.FromJson<Pattern>(File.ReadAllText(json));
                buffer.Add(new PatternData
                {
                    Index = index,
                    actions = new NativeArray<FunAction>(pattern.actions, Allocator.Persistent)
                });
                index++;
            }

            EntityManager.AddComponentData(pointSettingsEntity, new FunScriptPatternModeSettings
            {
                PatternCount = index,
                PatternIndex = 0,
                Repeat = 1,
                Spacing = 0,
                Scale = new float2(1, 1)
            });

            RequireForUpdate<AudioPlaybackTime>();
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            // Register callbacks to the display container
            foreach (var (visualElementRef, entity) in SystemAPI.Query<VisualElementRef>().WithEntityAccess().WithAll<FunScriptContainer>().WithNone<UICallbacksRegistered>())
            {
                VisualElement visualElement = visualElementRef.Value;
                EntityManager manager = EntityManager;

                // OnClick -> Add FunAction
                visualElement.RegisterCallback<ClickEvent>(evt => OnClick(evt, manager));

                // OnRMB -> Remove previous FunAction
                // OnRMB+CTRL -> Remove next FunAction
                visualElement.RegisterCallback<MouseDownEvent>(evt => OnMouseDown(evt, manager));

                ecb.AddComponent<UICallbacksRegistered>(entity);
            }

            var funscriptModeSelection = SystemAPI.GetSingleton<FunScriptModeSelection>();

            // Adjust point mode settings
            if (funscriptModeSelection.Value == 0)
            {
                var pointModeSettings = SystemAPI.GetSingleton<FunScriptPointModeSettings>();
                // StepMode toggle
                if (Input.mouseScrollDelta.y != 0)
                {
                    pointModeSettings.StepMode = !pointModeSettings.StepMode;
                }

                // Snapping toggle (Q)
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    pointModeSettings.Snapping = !pointModeSettings.Snapping;
                }

                SystemAPI.SetSingleton(pointModeSettings);
            }

            // Adjust pattern mode settings
            if (funscriptModeSelection.Value == 1)
            {
                var patternModeSettings = SystemAPI.GetSingleton<FunScriptPatternModeSettings>();

                // pattern change cycle (Q)
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    patternModeSettings.PatternIndex = patternModeSettings.PatternIndex < patternModeSettings.PatternCount - 1
                        ? patternModeSettings.PatternIndex + 1
                        : 0;
                }

                if (Input.mouseScrollDelta.y > 0)
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        patternModeSettings.Scale.x += 0.05f;
                    }
                    else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        patternModeSettings.Repeat += 1;
                    }
                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        patternModeSettings.Spacing += 100;
                    }
                    else
                    {
                        patternModeSettings.Scale.y += 0.05f;
                        patternModeSettings.Scale.y = math.min(patternModeSettings.Scale.y, 2);
                    }
                }

                if (Input.mouseScrollDelta.y < 0)
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        patternModeSettings.Scale.x -= 0.05f;
                        patternModeSettings.Scale.x = math.max(patternModeSettings.Scale.x, 0);
                    }
                    else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        patternModeSettings.Repeat -= 1;
                        patternModeSettings.Repeat = math.max(patternModeSettings.Repeat, 1);
                    }
                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        patternModeSettings.Spacing -= 100;
                    }
                    else
                    {
                        patternModeSettings.Scale.y -= 0.05f;
                        patternModeSettings.Scale.y = math.max(patternModeSettings.Scale.y, -1);
                    }
                }

                SystemAPI.SetSingleton(patternModeSettings);
            }

            // Set PointMode
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                funscriptModeSelection = new FunScriptModeSelection { Value = 0 };
                SystemAPI.SetSingleton(funscriptModeSelection);
            }

            // Set PatternMode
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                funscriptModeSelection = new FunScriptModeSelection { Value = 1 };
                SystemAPI.SetSingleton(funscriptModeSelection);
            }
        }

        private void OnMouseDown(MouseDownEvent evt, EntityManager manager)
        {
            bool leftMouseButtonPressed = 0 != (evt.pressedButtons & (1 << (int)MouseButton.LeftMouse));
            bool rightMouseButtonPressed = 0 != (evt.pressedButtons & (1 << (int)MouseButton.RightMouse));
            bool middleMouseButtonPressed = 0 != (evt.pressedButtons & (1 << (int)MouseButton.MiddleMouse));

            if (rightMouseButtonPressed && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                // Remove previous FunAction
                // Get mouse position as At value
                GetMouseAtPosition(evt.localMousePosition, evt.target as VisualElement, SystemAPI.GetSingleton<AudioPlaybackTime>(), out int mouseAt);

                int previousAt = -1;
                Entity previousFunActionEntity = Entity.Null;

                // Get previous at
                foreach (var (funAction, entity) in SystemAPI.Query<RefRO<FunAction>>().WithEntityAccess())
                {
                    int at = funAction.ValueRO.at;
                    if (at > mouseAt) continue;
                    if (at < previousAt) continue;

                    previousAt = at;
                    previousFunActionEntity = entity;
                }

                // Destroy previousFunActionEntity
                if (previousFunActionEntity != Entity.Null && previousAt > 0)
                {
                    manager.DestroyEntity(previousFunActionEntity);
                    MarkFunScriptNotSaved(manager);
                }
            }
            else if (rightMouseButtonPressed)
            {
                // Remove next FunAction
                // Get mouse position as At value
                GetMouseAtPosition(evt.localMousePosition, evt.target as VisualElement, SystemAPI.GetSingleton<AudioPlaybackTime>(), out int mouseAt);

                int nextAt = SystemAPI.GetSingleton<AudioPlaybackTime>().LengthInMilliseconds + 1;
                Entity nextFunActionEntity = Entity.Null;

                // Get next at
                foreach (var (funAction, entity) in SystemAPI.Query<RefRO<FunAction>>().WithEntityAccess())
                {
                    int at = funAction.ValueRO.at;
                    if (at < mouseAt) continue;
                    if (at > nextAt) continue;

                    nextAt = at;
                    nextFunActionEntity = entity;
                }

                // Destroy nextFunActionEntity
                if (nextFunActionEntity != Entity.Null && nextAt > 0)
                {
                    manager.DestroyEntity(nextFunActionEntity);
                    MarkFunScriptNotSaved(manager);
                }
            }
        }

        private void OnClick(ClickEvent evt, EntityManager manager)
        {
            var mode = SystemAPI.GetSingleton<FunScriptModeSelection>();
            if (mode.Value == 0)
            {
                AddPoint(evt, manager);
            }
            else if (mode.Value == 1)
            {
                AddPattern(evt, manager);
            }

            MarkFunScriptNotSaved(manager);
        }

        private void AddPattern(ClickEvent evt, EntityManager manager)
        {
            GetAtAndPos(evt.localPosition, evt.target as VisualElement, SystemAPI.GetSingleton<AudioPlaybackTime>(), out int mouseAt, out int mousePos);

            if (mouseAt <= 0) return;

            // Get active pattern
            var patternSettings = SystemAPI.GetSingleton<FunScriptPatternModeSettings>();
            var pattern = SystemAPI.GetSingletonBuffer<PatternData>()[patternSettings.PatternIndex];

            int length = (int)math.round(pattern.actions[pattern.actions.Length - 1].at * patternSettings.Scale.x + patternSettings.Spacing);
            int totalLength = length * patternSettings.Repeat;

            // clear existing values from under the pattern
            var funActions = _funActionQuery.ToComponentDataArray<FunAction>(Allocator.Temp);
            var funEntities = _funActionQuery.ToEntityArray(Allocator.Temp);
            var entitiesToRemove = new NativeList<Entity>(1000, Allocator.Temp);

            for (int i = 0; i < funActions.Length; i++)
            {
                if (funActions[i].at >= mouseAt && funActions[i].at <= mouseAt + totalLength)
                {
                    entitiesToRemove.Add(funEntities[i]);
                }
            }

            manager.DestroyEntity(entitiesToRemove.AsArray());
            funEntities.Dispose();
            entitiesToRemove.Dispose();
            funActions.Dispose();

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

            for (int i = 0; i < cleanedActions.Length; i++)
            {
                CreateFunActionEntity(manager, cleanedActions[i].at, cleanedActions[i].pos);
            }


            cleanedActions.Dispose();
        }

        private void AddPoint(ClickEvent evt, EntityManager manager)
        {
            GetAtAndPos(evt.localPosition, evt.target as VisualElement, SystemAPI.GetSingleton<AudioPlaybackTime>(), out int at, out int pos);
            if (at <= 2)
            {
                // TODO: Remove existing points at 0?
                CreateFunActionEntity(manager, 0, pos);
                return;
            }

            var pointMode = SystemAPI.GetSingleton<FunScriptPointModeSettings>();

            // If StepMode -> Add a point to at-1 with previous FunAction's position value 
            if (pointMode.StepMode)
            {
                var funActions = _funActionQuery.ToComponentDataArray<FunAction>(Allocator.Temp);
                funActions.Sort(new FunActionSorter());

                bool foundNextAt = false;

                for (int i = 0; i < funActions.Length; i++)
                {
                    // Found next at
                    // add an extra point before current "at" that matches the previous point's "pos"
                    if (funActions[i].at > at && i > 0)
                    {
                        CreateFunActionEntity(manager, at - 2, funActions[i - 1].pos);
                        foundNextAt = true;
                        break;
                    }

                    // no previous points
                    if (funActions[i].at > at && i <= 0)
                    {
                        break;
                    }
                }

                // No next at exists
                // add an extra point before current "at" that matches the previous point's "pos"
                if (!foundNextAt && funActions.Length > 0)
                {
                    CreateFunActionEntity(manager, at - 2, funActions[^1].pos);
                }

                funActions.Dispose();
            }

            // If Snapping -> convert at to closest value dividable by 5
            if (pointMode.Snapping)
            {
                pos = (int)(math.round(pos / 5f) * 5f);
            }

            CreateFunActionEntity(manager, at, pos);
        }

        private void CreateFunActionEntity(EntityManager manager, int at, int pos)
        {
            // Clear existing point if there is one at the "at"
            var funActions = _funActionQuery.ToComponentDataArray<FunAction>(Allocator.Temp);
            var funActionEntities = _funActionQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < funActions.Length; i++)
            {
                if (funActions[i].at == at)
                {
                    manager.DestroyEntity(funActionEntities[i]);
                    break;
                }
            }

            // Create new point
            var entity = manager.CreateEntity();
#if UNITY_EDITOR
            manager.AddComponentData(entity, new EntityName
            {
                Value = new FixedString64Bytes(UIConstants.FUN_ACTION)
            });
#endif
            manager.AddComponentData(entity, new FunAction()
            {
                at = at,
                pos = pos
            });
        }

        private void MarkFunScriptNotSaved(EntityManager manager)
        {
            if (!SystemAPI.HasSingleton<FunScriptNotSaved>())
            {
                var notSavedEntity = manager.CreateEntity();
#if UNITY_EDITOR
                manager.AddComponentData(notSavedEntity, new EntityName
                {
                    Value = new FixedString64Bytes("FunScript Not Saved")
                });
#endif
                manager.AddComponent<FunScriptNotSaved>(notSavedEntity);
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

        private static void GetMouseAtPosition(Vector2 localPosition, VisualElement visualElement, AudioPlaybackTime audioPlayback, out int at)
        {
            at = -1;
            float relativeX = localPosition.x / visualElement.contentRect.width;
            at = (int)((relativeX * 16000) + audioPlayback.TimeInMilliseconds - 8000);
        }
    }
}