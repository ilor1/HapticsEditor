// <copyright file="ButtplugUnity.cs" company="Nonpolynomial Labs LLC">
// Buttplug Unity Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Buttplug;
using System.Threading;
using System.ComponentModel;

namespace ButtplugUnity
{
    public class ButtplugUnityHelper
    {
        // The server process, assuming we've been asked to bring one up. Should be
        // null if not running.
        private static Process serverProcess = null;

        // Event handler for outputting things printed to stdout by the process
        private static event EventHandler<string> ProcessLogReceived;

        // Used internally to wait for process bringup.
        private static TaskCompletionSource<bool> serverBringupTask = null;

        // If true, output debug messages.
        private static bool outputDebugMessages = false;

        // Outputs a debug message to the console if outputDebugMessages is true.
        private static void MaybeDebugLog(string msg)
        {
            if (ButtplugUnityHelper.outputDebugMessages)
            {
                UnityEngine.Debug.Log(msg);
            }
        }

        // Given a set of Client/Server options, creates a client that either
        // connects to a server process also started by us, or else to an external
        // server like Intiface Desktop.
        //
        // Throws if server is already running, or if server process fails to start
        // up, or if client fails to connect for some reason.
        public static async Task StartProcessAndCreateClient(ButtplugUnityClient client, ButtplugUnityOptions options)
        {
            if (options.OutputDebugMessages)
            {
                outputDebugMessages = true;
            }

            ButtplugUnityHelper.MaybeDebugLog($"Using connection type {options.ConnectorType}.");

            ButtplugUnityHelper.MaybeDebugLog("Bringing up Buttplug Server/Client");
            // If the server is already up, we can't start it again, but we also can't
            // hand back a client. Throw.
            if (ButtplugUnityHelper.serverProcess != null)
            {
                ButtplugUnityHelper.MaybeDebugLog("Server already running, throwing");
                throw new InvalidOperationException("Server already running.");
            }

            var websocketPort = options.WebsocketPort;

            // We want to start the CLI process without a window, and capture output
            // from stdout at the moment just to see if it's alive. Crude, but it
            // does the job of making sure we don't try to connect before the
            // process spins up. This will change to using the Intiface Protobuf
            // system in the future.
            if (options.ConnectorType == ButtplugUnityConnectorType.WebsocketServerProcess)
            {
                // If we aren't given a port to use, generate a random one.
                if (websocketPort == 0)
                {
                    var rand = new System.Random();
                    websocketPort = (ushort)rand.Next(10000, 60000);
                }
                ButtplugUnityHelper.MaybeDebugLog($"Setting websocket port to {websocketPort}");

                var processPath = Path.Combine(Application.streamingAssetsPath, "Buttplug", "IntifaceCLI.exe");
                var arguments = $"--wsinsecureport {websocketPort} --pingtime {options.ServerPingTime}";
                if (options.AllowRawMessages)
                {
                    arguments += " --allowraw";
                }
                try
                {
                    // Create a new task that will resolve when the server comes up.
                    ButtplugUnityHelper.serverBringupTask = new TaskCompletionSource<bool>();

                    var serverProcess = new Process();
                    serverProcess.StartInfo.FileName = processPath;
                    // Don't open a window on starting, and make sure stdout is redirected
                    // so we can catch it for bringup status.
                    serverProcess.StartInfo.CreateNoWindow = true;
                    serverProcess.StartInfo.RedirectStandardOutput = true;
                    serverProcess.StartInfo.UseShellExecute = false;
                    serverProcess.StartInfo.Arguments = arguments;

                    ButtplugUnityHelper.MaybeDebugLog($"Starting task with arguments: {serverProcess.StartInfo.Arguments}");
                    serverProcess.Exited += ButtplugUnityHelper.OnServerExit;
                    serverProcess.OutputDataReceived += ButtplugUnityHelper.OnServerStart;

                    ButtplugUnityHelper.serverProcess = serverProcess;
                    serverProcess.Start();
                    serverProcess.BeginOutputReadLine();
                    ButtplugUnityHelper.MaybeDebugLog("Waiting for task output");
                    // Wait to get something from the process
                    await ButtplugUnityHelper.serverBringupTask.Task;
                    ButtplugUnityHelper.MaybeDebugLog("Task output received, continuing");
                    // Reset our bringup task to null now that the process is either up or dead.
                    ButtplugUnityHelper.serverBringupTask = null;
                    if (ButtplugUnityHelper.serverProcess == null)
                    {
                        ButtplugUnityHelper.MaybeDebugLog("Process died before bringup finished.");
                        throw new ApplicationException("ButtplugUnityHelper: Intiface process exited or crashed while coming up.");
                    }
                }
                catch (Win32Exception processException)
                {
                    ButtplugUnityHelper.MaybeDebugLog("Got process exception. If this is IL2CPP, this is expected and Ok. Printing exception and retrying using kernel32 P/Invoke methods.");
                    ButtplugUnityHelper.MaybeDebugLog(processException.ToString());
                    // This might be a IL2CPP issue, in which case, try to launch the process using those bindings.
                    //
                    // The option here is to hide the window, so we'll flip the context from our option.
                    StartExternalProcess.Start(processPath + " " + arguments, ".", !options.OpenIL2CPPConsoleWindow);
                }
            }

            // For some reason, in Unity 2018/2019 IL2CPP, awaiting our connect call
            // causes copies internally that end up dropping our sorter, meaning we'll
            // never get our connection confirmation back. This work everywhere in
            // Mono, and is fixed in IL2CPP in Unity 2020.
            //
            // Why am I doing this work for free.
            //
            // Logic tests for "202", meaning this should work back through 2018/2019,
            // but will futureproof us until Unity 2030 (or until they change their
            // major versioning scheme again).

            ButtplugUnityHelper.MaybeDebugLog("Connecting client");
            if (options.ConnectorType == ButtplugUnityConnectorType.ExternalWebsocketServer || options.ConnectorType == ButtplugUnityConnectorType.WebsocketServerProcess)
            {
                var connector_options = new ButtplugWebsocketConnectorOptions(new Uri($"ws://{options.WebsocketAddress}:{websocketPort}/buttplug"));
                if (Application.unityVersion.Contains("202"))
                {
                    await client.ConnectAsync(connector_options);
                }
                else
                {
                    client.ConnectAsync(connector_options);
                    await Task.Delay(3000);
                }
            }
            else
            {
                var connector_options = new ButtplugEmbeddedConnectorOptions();
                connector_options.AllowRawMessages = options.AllowRawMessages;
                connector_options.MaxPingTime = options.ServerPingTime;
                if (Application.unityVersion.Contains("202"))
                {
                    await client.ConnectAsync(connector_options);
                }
                else
                {
                    client.ConnectAsync(connector_options);
                    await Task.Delay(3000);
                }
            }
            ButtplugUnityHelper.MaybeDebugLog("Connected client");
        }

        private static void OnServerExit(object sender, EventArgs e)
        {
            ButtplugUnityHelper.MaybeDebugLog("Server process exited.");
            // If our process exited before outputing anything (crashed, etc...), fire
            // the task so StartProcess will clear out.
            if (ButtplugUnityHelper.serverBringupTask != null)
            {
                ButtplugUnityHelper.serverBringupTask.SetResult(true);
            }
            // Otherwise, we should just clean up, while letting shutdown know that
            // the process doesn't need to be killed.
            ButtplugUnityHelper.StopServer(true);
        }

        private static void OnServerStart(object sender, DataReceivedEventArgs e)
        {
            ButtplugUnityHelper.MaybeDebugLog("Server process started, got stdout line.");
            // This should only be called when the server first starts. Once that
            // happens, it should just pass off to the actual logging event handler.
            ButtplugUnityHelper.serverBringupTask.SetResult(true);
            ButtplugUnityHelper.serverProcess.OutputDataReceived -= ButtplugUnityHelper.OnServerStart;
            ButtplugUnityHelper.serverProcess.OutputDataReceived += ButtplugUnityHelper.OnServerLogMessage;
            ButtplugUnityHelper.OnServerLogMessage(sender, e);
        }

        private static void OnServerLogMessage(object sender, DataReceivedEventArgs e)
        {
            ButtplugUnityHelper.MaybeDebugLog($"Intiface Process Output: {e.Data}");
            ButtplugUnityHelper.ProcessLogReceived?.Invoke(null, e.Data);
        }

        // Assuming a server process is running, stops it, detaches event handlers,
        // and sets it to null.
        public static void StopServer(bool exited = false)
        {
            if (ButtplugUnityHelper.serverProcess == null)
            {
                return;
            }
            // Remove the exit event handler before killing the process, otherwise the
            // event handler will call StopServer and we'll end up in an infinite
            // event loop (heh).
            ButtplugUnityHelper.serverProcess.Exited -= ButtplugUnityHelper.OnServerExit;
            ButtplugUnityHelper.serverProcess.OutputDataReceived -= ButtplugUnityHelper.OnServerLogMessage;
            if (!exited && !ButtplugUnityHelper.serverProcess.HasExited)
            {
                ButtplugUnityHelper.serverProcess.Kill();
            }
            ButtplugUnityHelper.serverProcess = null;
        }
    }
}