using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace V2
{
    public class Waveform : MonoBehaviour
    {
        public Color32 ColorCenter;
        public Color32 ColorOuter;
        public int TimelineLength = 16;

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private UIDocument _uiDocument;

        private Texture2D _texture;
        private VisualElement _waveformVisualElement;
        private AudioClip _clip;
        private float[] _samples;
        private float _maxSample;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            // Update texture if it changes
            if (_texture == null || _waveformVisualElement == null)
            {
                GetTexture();
                return;
            }

            if (_clip == null)
            {
                GetAudioClip();
                return;
            }

            // get max sample (this might not be needed, but it allows us to normalize the values)
            if (_maxSample <= 0)
            {
                float[] allSamples = new float[_clip.channels * _clip.samples];
                _clip.GetData(allSamples, _audioSource.timeSamples);
                var samplesAllNative = new NativeArray<float>(allSamples, Allocator.TempJob);
                var maxSample = new NativeArray<float>(1, Allocator.TempJob);
                new GetMaxSampleJob
                {
                    Samples = samplesAllNative,
                    MaxSample = maxSample,
                }.Schedule().Complete();
                _maxSample = maxSample[0];
                maxSample.Dispose();
                samplesAllNative.Dispose();
            }

            // get data
            int offset = _audioSource.timeSamples - _clip.frequency * TimelineLength / 2 >= 0
                ? _audioSource.timeSamples - _clip.frequency * TimelineLength / 2
                : _audioSource.timeSamples - _clip.frequency * TimelineLength / 2 + _clip.samples;

            _clip.GetData(_samples, offset);
            var samplesNative = new NativeArray<float>(_samples, Allocator.TempJob);

            // process samples
            int samplesPerPixel = (int)math.floor((_clip.frequency * TimelineLength) / (float)_texture.width);
            var leftHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
            var rightHighestValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
            var leftRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
            var rightRmsValues = new NativeArray<float>(_texture.width, Allocator.TempJob);
            new ProcessSamplesParallelJob
            {
                Channels = _clip.channels,
                LeftHighestSamples = leftHighestValues,
                RightHighestSamples = rightHighestValues,
                LeftRms = leftRmsValues,
                RightRms = rightRmsValues,
                Samples = samplesNative,
                SamplesPerPixel = samplesPerPixel,
                MaxSampleValue = _maxSample
            }.Schedule(_texture.width, 64).Complete();
            samplesNative.Dispose();

            // Get colors
            var colors = _texture.GetRawTextureData<Color32>();
            new GetColorsJob
            {
                ColorCenter = ColorCenter,
                ColorOuter = ColorOuter,
                LeftRmsValues = leftRmsValues,
                RightRmsValues = rightRmsValues,
                LeftHighestValues = leftHighestValues,
                RightHighestValues = rightHighestValues,
                Height = _texture.height,
                Width = _texture.width,
                Offset = (int)(0.25f * _texture.height),
                Colors = colors
            }.Schedule().Complete();
            _texture.Apply();
        }

        private void GetTexture()
        {
            _waveformVisualElement = _uiDocument.rootVisualElement.Q<VisualElement>(UIConstants.WAVEFORM_CONTAINER);
            if (_waveformVisualElement != null)
            {
                int width = (int)_waveformVisualElement.contentRect.width;
                int height = (int)_waveformVisualElement.contentRect.height;
                if (width < 0 || height < 0) return;

                //_texture = new Texture2D(width, height, TextureFormat.RGBA32, 0, true);
                _texture = new Texture2D(width, height);
                _waveformVisualElement.style.backgroundImage = _texture;
            }
        }

        private void GetAudioClip()
        {
            // init clip
            if (_audioSource.clip != null)
            {
                _clip = _audioSource.clip;

                // 16s worth of samples
                _samples = new float[_clip.frequency * TimelineLength * _clip.channels];
            }
        }
    }
}