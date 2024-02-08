using System.Collections;
using UnityEngine;
using SimpleFileBrowser;
using UnityEngine.UIElements;
using V2;

public class FileLoader : MonoBehaviour
{
    private const string MP3_EXT = ".mp3";
    private const string WAV_EXT = ".wav";

    [SerializeField] private UIDocument _document;
    [SerializeField] private StyleSheet _styleSheet;

    private void Start()
    {
        // Generate UI
        StartCoroutine(Generate());
    }

    private IEnumerator Generate()
    {
        yield return null; // fix race condition

        // Create Root
        var root = _document.rootVisualElement;
        root.Clear();
        root.styleSheets.Add(_styleSheet);
        root.AddToClassList("root");
        
        // Create button
        var loadAudioButton = Create<Button>("button");
        loadAudioButton.text = "Load Audio";
        loadAudioButton.clicked += BrowseAudio;
        root.Add(loadAudioButton);

        _document.sortingOrder = 1;
    }
    
    private void BrowseAudio()
    {
        // https://github.com/yasirkula/UnitySimpleFileBrowser#example-code
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio", MP3_EXT, WAV_EXT));
        FileBrowser.SetDefaultFilter(MP3_EXT);
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    private IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null,
            "Load Audio", "Load");

        if (FileBrowser.Success)
        {
            string result = FileBrowser.Result[0];
            Debug.Log($"AudioManager: loaded file: {result}");
            LaunchManager.Instance.LoadAudio(result);
            
            // Disable UI
            _document.enabled = false;
        }
        else
        {
            // cancel
        }
    }

    private VisualElement Create(params string[] classNames)
    {
        return Create<VisualElement>(classNames);
    }

    private T Create<T>(params string[] classNames) where T : VisualElement, new()
    {
        var element = new T();
        foreach (var className in classNames)
        {
            element.AddToClassList(className);
        }

        return element;
    }
}