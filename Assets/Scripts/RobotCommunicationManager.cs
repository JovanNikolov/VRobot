using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class RobotCommunicationManager : MonoBehaviour
{
    [Header("Network Settings")]
    public string raspberryPiIP = "192.168.0.210";
    public int port = 6000;

    [Header("Performance Settings")]
    public float sendInterval = 0.05f; // 20Hz update rate

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool enableNetworking = true;

    private float[] servoValues = new float[8];
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private float lastSendTime = 0f;

    private static RobotCommunicationManager instance;
    public static RobotCommunicationManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<RobotCommunicationManager>();
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        InitializeNetwork();
    }

    void InitializeNetwork()
    {
        if (enableNetworking)
        {
            try
            {
                udpClient = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(raspberryPiIP), port);

                byte[] test = Encoding.ASCII.GetBytes("90,150,90,90,90,150,90,90");
                udpClient.Send(test, test.Length, remoteEndPoint);

                if (enableDebugLogs)
                    Debug.Log("Communication Manager: Test UDP sent successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError("UDP initialization error: " + ex.Message);
                enableNetworking = false;
            }
        }
    }

    void Update()
    {
        if (Time.time - lastSendTime >= sendInterval)
        {
            SendAllServoData();
            lastSendTime = Time.time;
        }
    }

    public void UpdateArmServos(float shoulderPitch, float shoulderRoll, float shoulderYaw,
                                 float elbow, float wrist, float grip)
    {
        servoValues[0] = shoulderYaw;
        servoValues[1] = shoulderPitch;
        servoValues[2] = shoulderRoll;
        servoValues[3] = elbow;
        servoValues[4] = wrist;
        servoValues[5] = grip;
    }

    public void UpdateHeadServos(float headPan, float headTilt)
    {
        servoValues[6] = headPan;
        servoValues[7] = headTilt;
    }

    void SendAllServoData()
    {
        // Format: shoulderYaw,shoulderPitch,shoulderRoll,elbow,wrist,grip,headPan,headTilt
        string message = string.Join(",", servoValues);

        if (enableNetworking && udpClient != null)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                udpClient.Send(data, data.Length, remoteEndPoint);

                if (enableDebugLogs)
                    Debug.Log("Sent: " + message);
            }
            catch (Exception ex)
            {
                Debug.LogError("UDP send error: " + ex.Message);
            }
        }
        else if (enableDebugLogs)
        {
            Debug.Log("Would send: " + message);
        }
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient.Dispose();
        }
    }
}