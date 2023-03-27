using System;
using Unity.Entities;
using UnityEngine;
using System.Collections;
using System.IO;
using Unity.Collections;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace V2
{
    /// <summary>
    /// Launcher
    /// Runtime will use DragAndDrop
    /// Editor uses inspector assigned _audioFilePath
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LaunchManager : MonoBehaviour
    {
        public static LaunchManager Instance;

        [SerializeField] private string _audioFilePath;
        [SerializeField] private UIDocument _uiDocument;

        private AudioSource _audioSource;
        private Entity _audioEntity = Entity.Null;

        private VisualElement _loader;
        private Label _label;
        private ProgressBar _progressBar;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            // Get VisualElements
            _loader = _uiDocument.rootVisualElement.Q("loader");
            _loader.style.display = DisplayStyle.Flex;
            _label = _loader.Q("label") as Label;

            _progressBar = _loader.Q("progressbar") as ProgressBar;
            _progressBar.style.display = DisplayStyle.None;
        }

        [ContextMenu("Load audio")]
        public void LoadEditorAudio()
        {
            LoadAudio(_audioFilePath);
        }

        public void LoadAudio(string path)
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            StartCoroutine(GetAudioClip(path, manager));
        }

        private IEnumerator GetAudioClip(string fullPath, EntityManager manager)
        {
            _label.style.display = DisplayStyle.None;
            _progressBar.style.display = DisplayStyle.Flex;

            Debug.Log($"Loading audio clip {fullPath}");

            AudioType audioType = AudioType.UNKNOWN;

            string extension = Path.GetExtension(fullPath);
            switch (extension)
            {
                case ".m4a":
                    audioType = AudioType.AUDIOQUEUE;
                    break;
                case ".mp3":
                    audioType = AudioType.MPEG;
                    break;
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case "ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                default:
                    audioType = AudioType.AUDIOQUEUE;
                    break;
            }

            UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                while (!webRequest.isDone)
                {
                    Debug.Log($"loading{webRequest.downloadProgress}");
                    _progressBar.value = webRequest.downloadProgress;
                    yield return null;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(webRequest);
                clip.name = Path.GetFileNameWithoutExtension(fullPath);

                _audioSource.clip = clip;
                _audioSource.Play();
                _audioSource.Pause();

                // AudioClip loaded
                if (_audioEntity == Entity.Null) _audioEntity = manager.CreateEntity();

                manager.AddComponentData(_audioEntity, new AudioFilePath { Value = new FixedString512Bytes(fullPath) });
                manager.AddComponentData(_audioEntity, new AudioSourceRef { Value = _audioSource });
                manager.AddComponentData(_audioEntity, new AudioClipRef { Value = clip });
            }

            _loader.style.display = DisplayStyle.None;
        }
    }
}