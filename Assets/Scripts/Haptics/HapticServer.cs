// Starts up a Buttplug Server, creates a client, connects to it, and has that
// client run a device scan. All output goes to the Unity Debug log.
//
// This is just a generic behavior, so you can attach it to any active object in
// your scene and it'll run on scene load.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using UnityEngine;
using UnityEngine.UIElements;


public class HapticServer : MonoBehaviour
{
    public static HapticServer Instance;
    public List<ButtplugClientDevice> Devices { get; } = new List<ButtplugClientDevice>();

    [Tooltip("Scaling for the Haptics intensity"), Range(0, 1f)]
    public float IntensityScale = 1f;

    [Tooltip("Haptics intensity (after scaling)"), SerializeField]
    private float _intensity = 0;

    private ButtplugClient _client;
    private float _timeSinceLastUpdate = 0.2f;

    private VisualElement _bluetoothIcon;
    [SerializeField] private Texture2D _bluetoothConnectedSprite;
    [SerializeField] private Texture2D _bluetoothDisconnectedSprite;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    private async void Start()
    {
        _client = new ButtplugClient("Test Client");
        Log("Trying to create client");

        // Set up client event handlers before we connect.
        _client.DeviceAdded += AddDevice;
        _client.DeviceRemoved += RemoveDevice;
        _client.ScanningFinished += ScanFinished;

        // Creating a Websocket Connector is as easy as using the right
        // options object.
        var connector = new ButtplugWebsocketConnector(
            new Uri("ws://localhost:12345/buttplug"));
        await _client.ConnectAsync(connector);

        await _client.StartScanningAsync();
    }

    private async void OnDestroy()
    {
        Devices.Clear();

        // On object shutdown, disconnect the client and just kill the server
        // process. Server process shutdown will be cleaner in future builds.
        if (_client != null)
        {
            _client.DeviceAdded -= AddDevice;
            _client.DeviceRemoved -= RemoveDevice;
            _client.ScanningFinished -= ScanFinished;
            await _client.DisconnectAsync();
            _client.Dispose();
            //_client = null;
        }

        Log("I am destroyed now");
    }

    public void SetIntensity(float intensity)
    {
        _intensity = intensity;

        // if (intensity == 0)
        // {
        //     _intensity = 0;
        //     return;
        // }
        //
        // // 0 to 1 float value
        // float t = (float)intensity * 0.01f;
        // float v = Mathf.Ceil(Mathf.Lerp(0, IntensityScale, t) * 20f) / 20f;
        //
        // // Scale the value
        // _intensity = v;
    }

    private void OnValidate()
    {
        // Make IntensityScale match the 20 steps of Lovense. (0.00, 0.05, 0.10, ...) 
        IntensityScale = Mathf.Round(IntensityScale * 20f) / 20f;
    }

    private void FixedUpdate()
    {
        // Send VibrateCmd to the device at given intervals at current intensity
        _timeSinceLastUpdate += Time.deltaTime;
        if (_timeSinceLastUpdate > 0.2f)
        {
            foreach (ButtplugClientDevice device in Devices)
            {
                device.VibrateAsync(_intensity);
            }

            _timeSinceLastUpdate = 0;
        }
    }

    private void AddDevice(object sender, DeviceAddedEventArgs e)
    {
        Log($"Device {e.Device.Name} Connected!");
        Devices.Add(e.Device);

        ToggleBluetoothIcon(Devices.Count > 0);
    }

    private void RemoveDevice(object sender, DeviceRemovedEventArgs e)
    {
        Log($"Device {e.Device.Name} Removed!");
        Devices.Remove(e.Device);

        ToggleBluetoothIcon(Devices.Count > 0);
    }

    private void ScanFinished(object sender, EventArgs e)
    {
        Log("Device scanning is finished!");
    }

    private void Log(object text)
    {
        Debug.Log("<color=red>Buttplug:</color> " + text, this);
    }

    private void ToggleBluetoothIcon(bool value)
    {
        if (_bluetoothIcon == null)  UIDocumentReferenceManagerSystem.TryGetVisualElementByName("bt-icon", out _bluetoothIcon);

        if (value)
        {
            _bluetoothIcon.style.display = DisplayStyle.Flex;
        }
        else
        {
            _bluetoothIcon.style.display = DisplayStyle.None;
        }

        _bluetoothIcon.tooltip = Devices.Count + " connected toys.";
    }
}