using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class OBSWebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private string obsPassword = ""; // Add your OBS WebSocket password if needed

    async void Start()
    {
        ws = new WebSocket("ws://localhost:4455");

        ws.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("OBS Response: " + message);
        };

        await ws.Connect();

        // Authenticate if password is set
        if (!string.IsNullOrEmpty(obsPassword))
        {
            string authMessage = "{\"op\": 1, \"d\": {\"rpcVersion\": 1}}";
            await ws.SendText(authMessage);
        }

        // Start Virtual Camera
        string startVirtualCam = "{\"op\":6, \"d\": { \"requestType\": \"StartVirtualCam\", \"requestId\": \"start_vcam\"}}";
        await ws.SendText(startVirtualCam);

        Debug.Log("Sent request to start OBS Virtual Camera.");
    }

    private async void OnApplicationQuit()
    {
        await ws.Close();
    }
}