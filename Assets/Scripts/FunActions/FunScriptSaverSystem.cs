using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace V2
{
    [BurstCompile]
    public partial class FunScriptSaverSystem : SystemBase
    {
        private EntityQuery _funActionQuery;

        [BurstCompile]
        protected override void OnCreate()
        {
            _funActionQuery = GetEntityQuery(typeof(FunAction));

            RequireForUpdate<AudioPlaybackTime>();
            RequireForUpdate<AudioFilePath>();
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            // Save (ctrl-s)
            if (Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                string path = GetFunScriptPath(SystemAPI.GetSingleton<AudioFilePath>().Value.ToString());
                int length = SystemAPI.GetSingleton<AudioPlaybackTime>().LengthInMilliseconds;

                var funActions = _funActionQuery.ToComponentDataArray<FunAction>(Allocator.Temp);
                funActions.Sort(new FunActionSorter());

                // Metadata
                var metadataComponent = SystemAPI.GetSingleton<MetadataComponent>();

                string[] performers = new string[metadataComponent.performers.Length];
                for (int i = 0; i < performers.Length; i++)
                {
                    performers[i] = metadataComponent.performers[i].ToString();
                }
                
                string[] tags = new string[metadataComponent.tags.Length];
                for (int i = 0; i < tags.Length; i++)
                {
                    tags[i] = metadataComponent.tags[i].ToString();
                }

                bool inverted = metadataComponent.inverted;
                
                Metadata metadata = new Metadata
                {
                    creator = metadataComponent.creator.ToString(),
                    description = metadataComponent.description.ToString(),
                    duration = metadataComponent.duration,
                    license = metadataComponent.license.ToString(),
                    notes = metadataComponent.notes.ToString(),
                    performers = performers,
                    script_url = metadataComponent.script_url.ToString(),
                    tags=tags,
                    title = metadataComponent.title.ToString(),
                    type = metadataComponent.type.ToString(),
                    video_url = metadataComponent.video_url.ToString(),
                    range = metadataComponent.range,
                    version = metadataComponent.version.ToString()
                };
                
                // Actions
                var validatedFunActions = ValidatedFunScript(funActions.ToList(), length);

                // Write
                var json = new FunScriptJson
                {
                    actions = validatedFunActions,
                    inverted = inverted,
                    metadata = metadata
                };
                File.WriteAllText(path, JsonUtility.ToJson(json));
                funActions.Dispose();
                Debug.Log($"FunScript saved: '{path}'");
                
                if (SystemAPI.HasSingleton<FunScriptNotSaved>())
                {
                    ecb.DestroyEntity(SystemAPI.GetSingletonEntity<FunScriptNotSaved>());
                }
            }
        }

        private string GetFunScriptPath(string audioFilePath)
        {
            string dir = Path.GetDirectoryName(audioFilePath);
            string filename = Path.GetFileNameWithoutExtension(audioFilePath) + ".funscript";
            return Path.Combine(dir, filename);
        }

        private FunAction[] ValidatedFunScript(List<FunAction> funActionsList, int lengthInMilliseconds)
        {
            int fixedCount = 0;

            for (int i = 0; i < funActionsList.Count; i++)
            {
                // clamp between 0 and 100
                var funAction = funActionsList[i];
                if (funAction.pos > 100 || funAction.pos < 0)
                {
                    funAction.pos = Mathf.Clamp(funAction.pos, 0, 100);
                    fixedCount++;
                }

                // round to increments of 5
                if (funAction.pos % 5 != 0)
                {
                    float v = (float)funAction.pos / 5f;
                    funAction.pos = Mathf.RoundToInt(v) * 5;
                    fixedCount++;
                }

                funActionsList[i] = funAction;
            }

            if (fixedCount > 0)
            {
                Debug.LogWarning($"Fixed {fixedCount} position values");
            }

            // Add funaction in the beginning
            if (funActionsList.Count == 0)
            {
                funActionsList.Add(new FunAction { at = 0, pos = 0 });
            }
            
            // Add value to the end if the last value is too soon
            if (funActionsList[funActionsList.Count - 1].at < lengthInMilliseconds - 29000)
            {
                funActionsList.Add(new FunAction { at = lengthInMilliseconds, pos = funActionsList[funActionsList.Count - 1].pos });
            }

            // Add extra values
            var funActions = new List<FunAction>();
            for (int i = 0; i < funActionsList.Count; i++)
            {
                if (i == 0 && funActionsList[i].at != 0)
                {
                    // start at 0:0 if no action was set at 0
                    funActions.Add(new FunAction { at = 0, pos = 0 });
                }

                // Add this value
                funActions.Add(funActionsList[i]);

                // Add values every 30s to avoid timeouts
                // if the recurring pos values are the same
                if (i < funActionsList.Count - 1)
                {
                    if (funActionsList[i].pos == funActionsList[i + 1].pos)
                    {
                        int distance = funActionsList[i + 1].at - funActionsList[i].at;
                        int at = funActionsList[i].at;
                        int pos = funActionsList[i].pos;
                        int j = 1;
                        while (distance > 30000)
                        {
                            funActions.Add(new FunAction { at = at + (30000 * j), pos = pos });
                            j++;
                            distance -= 30000;
                        }
                    }
                }
            }

            return funActions.ToArray();
        }
    }
}