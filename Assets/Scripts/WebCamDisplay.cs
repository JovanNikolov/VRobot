using UnityEngine;

public class WebCamDisplay : MonoBehaviour
{
    //Virtual WebCam stream using OBS, works, need to be changed to direct stream from RTSP
    private WebCamTexture webcamTexture;

    void Start()
    {
        // Get a list of available webcams
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            // Use the first available camera
            string cameraName = devices[2].name;
            webcamTexture = new WebCamTexture(cameraName);

            // Apply the webcam texture to the object's material
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.mainTexture = webcamTexture;

            // Start the webcam feed
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("No webcam found!");
        }
    }

    void OnDestroy()
    {
        // Stop the webcam when the object is destroyed
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}




// using System;
// using System.Diagnostics;
// using System.Text;
// using System.Threading.Tasks;
// using NativeWebSocket;
// using UnityEngine;

// public class OBSWebCamDisplay : MonoBehaviour
// {
//     private WebSocket ws;
//     private string obsAddress = "ws://localhost:4455";
//     private bool isConnected = false;
//     private WebCamTexture webcamTexture;

//     async void Start()
//     {
//         // Ensure OBS is running before connecting
//         if (!IsOBSRunning())
//         {
//             StartOBS();
//             await Task.Delay(5000); // Wait 5 seconds for OBS to start
//         }

//         // Connect to OBS WebSocket and start the virtual camera
//         await ConnectToOBS();
//     }

//     bool IsOBSRunning()
//     {
//         Process[] processes = Process.GetProcessesByName("obs64");
//         return processes.Length > 0;
//     }
// void StartOBS()
// {
//     string batFilePath = Application.dataPath + "/Scripts/start_obs.bat"; // Adjust path if needed

//     try
//     {
//         ProcessStartInfo startInfo = new ProcessStartInfo
//         {
//             FileName = batFilePath,
//             WorkingDirectory = Application.dataPath + "/Scripts",
//             UseShellExecute = true,
//             CreateNoWindow = true
//         };

//         Process.Start(startInfo);
//         UnityEngine.Debug.Log("Starting OBS via batch file...");
//     }
//     catch (Exception e)
//     {
//         UnityEngine.Debug.LogError("Failed to start OBS: " + e.Message);
//     }
// }


//     async Task ConnectToOBS()
//     {
//         int maxRetries = 5;
//         int retryDelay = 3000; // 3 seconds

//         for (int attempt = 1; attempt <= maxRetries; attempt++)
//         {
//             try
//             {
//                 ws = new WebSocket(obsAddress);

//                 ws.OnOpen += () =>
//                 {
//                     isConnected = true;
//                     UnityEngine.Debug.Log("Connected to OBS WebSocket.");
//                     StartVirtualCamera();
//                 };

//                 ws.OnMessage += (bytes) =>
//                 {
//                     string message = Encoding.UTF8.GetString(bytes);
//                     UnityEngine.Debug.Log("OBS Response: " + message);
//                 };

//                 ws.OnError += (error) =>
//                 {
//                     UnityEngine.Debug.LogError("WebSocket Error: " + error);
//                 };

//                 ws.OnClose += (code) =>
//                 {
//                     isConnected = false;
//                     UnityEngine.Debug.LogWarning("OBS WebSocket closed. Code: " + code);
//                 };

//                 await ws.Connect();

//                 if (isConnected) break;
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogWarning($"OBS connection attempt {attempt} failed: {ex.Message}");
//                 await Task.Delay(retryDelay);
//             }
//         }

//         if (!isConnected)
//         {
//             UnityEngine.Debug.LogError("Failed to connect to OBS after multiple attempts.");
//         }
//     }

//     async void StartVirtualCamera()
//     {
//         if (!isConnected) return;

//         string startVirtualCam = "{\"op\":6, \"d\": { \"requestType\": \"StartVirtualCam\", \"requestId\": \"start_vcam\"}}";
//         await ws.SendText(startVirtualCam);
//         UnityEngine.Debug.Log("Requested OBS to start Virtual Camera.");

//         // Wait a few seconds to allow OBS Virtual Camera to start
//         await Task.Delay(3000);

//         // Now start the webcam
//         StartWebCam();
//     }

//     void StartWebCam()
//     {
//         WebCamDevice[] devices = WebCamTexture.devices;

//         if (devices.Length > 0)
//         {
//             // Look for OBS Virtual Camera
//             string cameraName = "OBS Virtual Camera"; // Default name for OBS Virtual Cam

//             foreach (var device in devices)
//             {
//                 if (device.name.Contains("OBS"))
//                 {
//                     cameraName = device.name;
//                     break;
//                 }
//             }

//             webcamTexture = new WebCamTexture(cameraName);
//             Renderer renderer = GetComponent<Renderer>();
//             renderer.material.mainTexture = webcamTexture;
//             webcamTexture.Play();

//             UnityEngine.Debug.Log("Started webcam: " + cameraName);
//         }
//         else
//         {
//             UnityEngine.Debug.LogError("No webcam found!");
//         }
//     }

//     private async void OnApplicationQuit()
//     {
//         if (ws != null) await ws.Close();
//         if (webcamTexture != null && webcamTexture.isPlaying) webcamTexture.Stop();
//     }
// }