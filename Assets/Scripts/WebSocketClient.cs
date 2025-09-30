using UnityEngine;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance;
    private WebSocket ws;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ws = new WebSocket("ws://192.168.0.203:8765");
        ws.Connect();
    }

    public async void SendData(string data)
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.SendText(data);
        }
    }

    void Update()
    {
        if (ws != null)
        {
            ws.DispatchMessageQueue();
        }
    }

    async void OnApplicationQuit()
    {
        await ws.Close();
    }
}
