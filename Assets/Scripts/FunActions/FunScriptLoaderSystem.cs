using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace V2
{
    /// <summary>
    /// Loads .funscript file and creates one FunAction entity for each action inside it.
    /// TODO: load and set metadata 
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    public partial class FunScriptLoaderSystem : SystemBase
    {
        private EntityQuery _funactionQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<AudioPlaybackTime>();

            _funactionQuery = GetEntityQuery(ComponentType.ReadOnly<FunAction>());
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            var audioPlayback = SystemAPI.GetSingleton<AudioPlaybackTime>();

            // Load FunActions from json
            foreach (var (audioFilePath, entity) in SystemAPI.Query<RefRO<AudioFilePath>>().WithChangeFilter<AudioFilePath>().WithEntityAccess())
            {
                string funScriptPath = GetFunScriptPath(audioFilePath.ValueRO.Value.ToString());
                if (string.IsNullOrEmpty(funScriptPath)) continue;

                FunScriptJson json;

                // Read json
                if (File.Exists(funScriptPath))
                {
                    Debug.Log($"Loading FunScript: '{funScriptPath}'");
                    json = JsonUtility.FromJson<FunScriptJson>(File.ReadAllText(funScriptPath));
                }
                // Create json
                else
                {
                    Debug.LogWarning($"'{funScriptPath}' did not exist. New one created");
                    json = new FunScriptJson
                    {
                        actions = new FunAction[] { new FunAction { at = 0, pos = 0 } },
                        inverted = false,
                        metadata = new Metadata
                        {
                            tags = new string[] { },
                            performers = new string[] { },
                            duration = (int)math.round(audioPlayback.LengthInMilliseconds * 0.001f),
                            creator = "",
                            type = "basic",
                            range = 100,
                            version = "1.0",
                            description = "",
                            notes = "",
                            license = "",
                            script_url = "",
                            video_url = "",
                            title = Path.GetFileNameWithoutExtension(funScriptPath)
                        }
                    };
                    File.WriteAllText(funScriptPath, JsonUtility.ToJson(json));
                }

                // Create FunActions
                CreateFunActions(ref ecb, json.actions);

                // Create MetaData
                CreateMetaDataComponent(ref ecb, json.inverted, json.metadata, funScriptPath, audioPlayback.LengthInMilliseconds);
            }
        }

        private void CreateFunActions(ref EntityCommandBuffer ecb, FunAction[] funActions)
        {
            // remove existing FunActions
            ecb.DestroyEntity(_funactionQuery);

            // create new ones
            for (int i = 0; i < funActions.Length; i++)
            {
                // not first point and not last point
                if (i > 0 && funActions.Length > i + 1)
                {
                    int pos = funActions[i].pos;
                    if (funActions[i - 1].pos == pos && funActions[i + 1].pos == pos)
                    {
                        // this point's position is the same as the next one and previous one, so it doesn't contribute.
                        // skip to next one
                        continue;
                    }
                }

                Entity funActionEntity = ecb.CreateEntity();
                ecb.AddComponent(funActionEntity, new FunAction
                {
                    at = funActions[i].at,
                    pos = funActions[i].pos
                });

#if UNITY_EDITOR
                ecb.AddComponent(funActionEntity, new EntityName
                {
                    Value = new FixedString64Bytes(UIConstants.FUN_ACTION)
                });
#endif
            }
        }

        private void CreateMetaDataComponent(ref EntityCommandBuffer ecb, bool inverted, Metadata metadata, string funScriptPath, int lengthInMilliseconds)
        {
            var performers = new NativeList<FixedString64Bytes>(metadata.performers.Length, Allocator.Persistent);
            for (int i = 0; i < metadata.performers.Length; i++)
            {
                performers.Add(new FixedString64Bytes(metadata.performers[i]));
            }

            var tags = new NativeList<FixedString32Bytes>(metadata.tags.Length, Allocator.Persistent);
            for (int i = 0; i < metadata.tags.Length; i++)
            {
                tags.Add(new FixedString32Bytes(metadata.tags[i]));
            }

            var metadataComponent = new MetadataComponent
            {
                inverted = inverted,
                creator = new FixedString64Bytes(metadata.creator),
                description = new FixedString512Bytes(metadata.description),
                duration = metadata.duration == 0 ? (int)math.round(lengthInMilliseconds * 0.001f) : metadata.duration,
                license = new FixedString128Bytes(metadata.license),
                notes = new FixedString512Bytes(metadata.notes),
                performers = performers,
                script_url = new FixedString128Bytes(metadata.script_url),
                tags = tags,
                title = string.IsNullOrEmpty(metadata.title) ? new FixedString128Bytes(Path.GetFileNameWithoutExtension(funScriptPath)) : new FixedString128Bytes(metadata.title),
                type = string.IsNullOrEmpty(metadata.type) ? new FixedString32Bytes("basic") : new FixedString32Bytes(metadata.type),
                video_url = new FixedString128Bytes(metadata.video_url),
                range = metadata.range,
                version = string.IsNullOrEmpty(metadata.version) ? new FixedString32Bytes("1.0") : new FixedString32Bytes(metadata.version)
            };

            SystemAPI.SetSingleton(metadataComponent);
        }

        private string GetFunScriptPath(string audioFilePath)
        {
            string dir = Path.GetDirectoryName(audioFilePath);
            string filename = Path.GetFileNameWithoutExtension(audioFilePath) + ".funscript";
            return Path.Combine(dir, filename);
        }
    }
}