using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

namespace V2
{
    public partial class TitleFilePathUpdater : SystemBase
    {
        private bool _titleHasNotSavedMarker;

        private FixedString512Bytes _path;

        protected override void OnUpdate()
        {
            bool isSaved = !SystemAPI.HasSingleton<FunScriptNotSaved>();

            foreach (var (visualElementRef, filepathLabel) in SystemAPI.Query<VisualElementRef, RefRO<FilePathLabel>>())
            {
                // before save
                if (!_titleHasNotSavedMarker && !isSaved)
                {
                    Label label = visualElementRef.Value as Label;
                    label.text = $"{filepathLabel.ValueRO.Value.ToString()}*";
                    _titleHasNotSavedMarker = true;
                }
                // after save
                else if (_titleHasNotSavedMarker && isSaved)
                {
                    Label label = visualElementRef.Value as Label;
                    label.text = $"{filepathLabel.ValueRO.Value.ToString()}";
                    _titleHasNotSavedMarker = false;
                }
                else if (filepathLabel.ValueRO.Value != _path)
                {
                    _path = filepathLabel.ValueRO.Value;
                    Label label = visualElementRef.Value as Label;
                    label.text = $"{filepathLabel.ValueRO.Value.ToString()}";
                    _titleHasNotSavedMarker = false;
                }
            }
        }
    }
}