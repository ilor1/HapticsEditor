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

        [SerializeField] private UIDocument _uiDocument;

        private AudioSource _audioSource;
        private Entity _audioEntity = Entity.Null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _audioSource = GetComponent<AudioSource>();
        }

        public void LoadAudio(string path)
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            StartCoroutine(GetAudioClip(path, manager));
        }

        private IEnumerator GetAudioClip(string fullPath, EntityManager manager)
        {
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
                    
                    // TODO: create a progress bar that updates properly.
                    
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
        }
    }
}