using System;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

namespace V2
{
    public partial class MetaDataUpdater : SystemBase
    {
        private const string INVERTED = "inverted";
        private const string CREATOR = "creator";
        private const string DESCRIPTION = "description";
        private const string DURATION = "duration";
        private const string LICENSE = "license";
        private const string NOTES = "notes";
        private const string PERFORMERS = "performers";
        private const string SCRIPT_URL = "script_url";
        private const string TAGS = "tags";
        private const string TITLE = "title";
        private const string TYPE = "type";
        private const string VIDEO_URL = "video_url";
        private const string RANGE = "range";
        private const string VERSION = "version";

        protected override void OnUpdate()
        {
            // Read the metadata into the UI
            foreach (var (visualElementRef, metadata) in SystemAPI.Query<VisualElementRef, RefRO<MetadataComponent>>().WithChangeFilter<MetadataComponent>())
            {
                var e = visualElementRef.Value;
                ((Toggle)e.Q(INVERTED)).value = metadata.ValueRO.inverted;
                ((TextField)e.Q(CREATOR)).value = metadata.ValueRO.creator.ToString();
                ((TextField)e.Q(DESCRIPTION)).value = metadata.ValueRO.description.ToString();
                ((TextField)e.Q(DURATION)).value = metadata.ValueRO.duration.ToString();
                ((TextField)e.Q(LICENSE)).value = metadata.ValueRO.license.ToString();
                ((TextField)e.Q(NOTES)).value = metadata.ValueRO.notes.ToString();

                string performers = "";
                if (metadata.ValueRO.performers.IsCreated)
                {
                    for (int i = 0; i < metadata.ValueRO.performers.Length; i++)
                    {
                        if (i > 0) performers += ", ";
                        performers += metadata.ValueRO.performers[i].ToString();
                    }
                }

                ((TextField)e.Q(PERFORMERS)).value = performers;
                ((TextField)e.Q(SCRIPT_URL)).value = metadata.ValueRO.script_url.ToString();

                string tags = "";
                if (metadata.ValueRO.tags.IsCreated)
                {
                    for (int i = 0; i < metadata.ValueRO.tags.Length; i++)
                    {
                        if (i > 0) tags += ", ";
                        tags += metadata.ValueRO.tags[i].ToString();
                    }
                }

                ((TextField)e.Q(TAGS)).value = tags;
                ((TextField)e.Q(TITLE)).value = metadata.ValueRO.title.ToString();
                ((TextField)e.Q(TYPE)).value = metadata.ValueRO.type.ToString();
                ((TextField)e.Q(VIDEO_URL)).value = metadata.ValueRO.video_url.ToString();
                ((TextField)e.Q(RANGE)).value = metadata.ValueRO.range.ToString();
                ((TextField)e.Q(VERSION)).value = metadata.ValueRO.version.ToString();
            }

            // Read metadata from the UI to the component
            // TODO: optimize
            foreach (var (visualElementRef, metadata) in SystemAPI.Query<VisualElementRef, RefRW<MetadataComponent>>())
            {
                var e = visualElementRef.Value;
                metadata.ValueRW.inverted = ((Toggle)e.Q(INVERTED)).value;
                metadata.ValueRW.creator = new FixedString64Bytes(((TextField)e.Q(CREATOR)).value);
                metadata.ValueRW.description = new FixedString512Bytes(((TextField)e.Q(DESCRIPTION)).value);
                metadata.ValueRW.duration = Int32.Parse(((TextField)e.Q(DURATION)).value);
                metadata.ValueRW.license = new FixedString128Bytes(((TextField)e.Q(LICENSE)).value);
                metadata.ValueRW.notes = new FixedString512Bytes(((TextField)e.Q(NOTES)).value);
                metadata.ValueRW.title = new FixedString128Bytes(((TextField)e.Q(TITLE)).value);
                metadata.ValueRW.type = new FixedString32Bytes(((TextField)e.Q(TYPE)).value);
                metadata.ValueRW.video_url = new FixedString128Bytes(((TextField)e.Q(VIDEO_URL)).value);
                metadata.ValueRW.range = Int32.Parse(((TextField)e.Q(RANGE)).value);
                metadata.ValueRW.version = new FixedString32Bytes(((TextField)e.Q(VERSION)).value);
                metadata.ValueRW.script_url  = new FixedString128Bytes( ((TextField)e.Q(SCRIPT_URL)).value);

                // drop spaces and split to array using "," as separator
                var tmp = Regex.Replace(((TextField)e.Q(PERFORMERS)).value, @"\s+", "");
                string[] performers = tmp.Split(',');
                metadata.ValueRW.performers.Clear();
                for (int i = 0; i < performers.Length; i++)
                {
                    metadata.ValueRW.performers.Add(performers[i]);
                }
                
                // drop spaces and split to array using "," as separator
                tmp = Regex.Replace(((TextField)e.Q(TAGS)).value, @"\s+", "");
                string[] tags = tmp.Split(',');
                metadata.ValueRW.tags.Clear();
                for (int i = 0; i < tags.Length; i++)
                {
                    metadata.ValueRW.tags.Add(tags[i]);
                }
            }
        }
    }
}