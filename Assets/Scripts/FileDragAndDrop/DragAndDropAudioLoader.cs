using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using B83.Win32;
using UnityEngine;
using UnityEngine.Networking;
using V2;


public class DragAndDropAudioLoader : MonoBehaviour
{
#if UNITY_EDITOR
    private void Start()
    {
        Destroy(gameObject);
    }
#else
    public static DragAndDropAudioLoader Instance;
    public static Action<AudioSource, AudioClip> AudioClipLoaded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    private void OnEnable()
    {

        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnDroppedFiles;

    }

    private void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
    }

    async void OnDroppedFiles(List<string> pathNames, POINT droppedPoint)
    {
        // Only allow single file.
        if (pathNames.Count > 1)
        {
            Debug.LogError("DragAndDropAudioLoader: Received too many files. Only single file is supported.");
            return;
        }

        string file = null;
        foreach (string receivedFile in pathNames)
        {
            FileInfo fileInfo = new FileInfo(receivedFile);
            var ext = fileInfo.Extension.ToLower();

            // Only allow wav files
            if (ext != ".wav" && ext != ".mp3")
            {
                Debug.LogError("DragAndDropAudioLoader: Received wrong filetype. Only .wav or .mp3 is supported.");
                return;
            }

            file = receivedFile;
        }

        // Load AudioClip
        LaunchManager.Instance.LoadAudio(file);
    }

    async Task<AudioClip> LoadClip(string path)
    {
        AudioClip clip = null;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV))
        {
            uwr.SendWebRequest();

            // wrap tasks in try/catch, otherwise it'll fail silently
            try
            {
                while (!uwr.isDone) await Task.Delay(5);

                if (uwr.isNetworkError || uwr.isHttpError) Debug.Log($"{uwr.error}");
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(uwr);
                }
            }
            catch (Exception err)
            {
                Debug.Log($"{err.Message}, {err.StackTrace}");
            }
        }

        return clip;
    }
#endif
}