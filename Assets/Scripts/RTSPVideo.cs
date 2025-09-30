using System.Diagnostics;
using System.IO;
using UnityEngine;

public class VideoStreamToTexture : MonoBehaviour
{
    //Direct stream through RTSP protocol, TODO
    Process ffmpegProcess;
    Texture2D texture;
    byte[] frameBuffer;
    byte[] fullFrameBuffer;
    int textureWidth = 1280;  // Set to match your video stream resolution
    int textureHeight = 720;  // Set to match your video stream resolution
    int expectedFrameSize;

    int accumulatedDataSize = 0;  // Tracks the size of accumulated data

    void Start()
    {
        StartFFmpeg();
        texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        frameBuffer = new byte[32768]; // Buffer for partial data chunks (adjust size as needed)
        fullFrameBuffer = new byte[textureWidth * textureHeight * 3]; // Full frame buffer
        expectedFrameSize = fullFrameBuffer.Length; // 1280x720x3 bytes for RGB24 format
    }

    void Update()
    {
        if (ffmpegProcess != null && ffmpegProcess.StandardOutput.BaseStream.CanRead)
        {
            int bytesRead = ffmpegProcess.StandardOutput.BaseStream.Read(frameBuffer, 0, frameBuffer.Length);

            if (bytesRead > 0)
            {
                // Accumulate partial data in the full frame buffer
                System.Array.Copy(frameBuffer, 0, fullFrameBuffer, accumulatedDataSize, bytesRead);
                accumulatedDataSize += bytesRead;

                // Check if we've accumulated enough data for a full frame
                if (accumulatedDataSize >= expectedFrameSize)
                {
                    // We have a complete frame, so apply it to the texture
                    try
                    {
                        texture.LoadRawTextureData(fullFrameBuffer);
                        texture.Apply();
                        GetComponent<Renderer>().material.mainTexture = texture;

                        // Reset the buffer for the next frame
                        accumulatedDataSize = 0;
                    }
                    catch
                    {
                        UnityEngine.Debug.LogWarning("Failed to load texture frame.");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Accumulating frame data: {accumulatedDataSize} bytes.");
                }
            }
        }
    }

    void StartFFmpeg()
    {
        ffmpegProcess = new Process();
        ffmpegProcess.StartInfo.FileName = "ffmpeg"; // Ensure FFmpeg is installed
        ffmpegProcess.StartInfo.Arguments = "-rtsp_transport udp -i rtsp://192.168.0.203:8554/unicast -f rawvideo -pix_fmt rgb24 -vsync 2 -flush_packets 0 -";  // Adjust FFmpeg arguments
        ffmpegProcess.StartInfo.RedirectStandardOutput = true;
        ffmpegProcess.StartInfo.UseShellExecute = false;
        ffmpegProcess.StartInfo.CreateNoWindow = true;
        ffmpegProcess.Start();
    }

    void OnApplicationQuit()
    {
        ffmpegProcess?.Kill();
    }
}

//STAR
// using System.Diagnostics;
// using UnityEngine;
// using System.IO;

// public class RTSPStream : MonoBehaviour
// {
//     Process ffmpegProcess;
//     Texture2D texture;
//     byte[] frameBuffer;

//     void Start()
//     {
//         StartFFmpeg();
//         texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
//     }

//     void Update()
//     {
//         if (ffmpegProcess != null && ffmpegProcess.StandardOutput.BaseStream.CanRead)
//         {
//             ffmpegProcess.StandardOutput.BaseStream.Read(frameBuffer, 0, frameBuffer.Length);
//             texture.LoadRawTextureData(frameBuffer);
//             texture.Apply();
//             GetComponent<Renderer>().material.mainTexture = texture;
//         }
//     }
// //rtsp://192.168.0.203:8554/unicast
//     void StartFFmpeg()
//     {
//         ffmpegProcess = new Process();
//         ffmpegProcess.StartInfo.FileName = "ffmpeg";
//         ffmpegProcess.StartInfo.Arguments = "-rtsp_transport tcp -i rtsp://192.168.0.203:8554/unicast -pix_fmt rgb24 -f rawvideo -";
//         ffmpegProcess.StartInfo.RedirectStandardOutput = true;
//         ffmpegProcess.StartInfo.UseShellExecute = false;
//         ffmpegProcess.StartInfo.CreateNoWindow = true;
//         ffmpegProcess.Start();

//         frameBuffer = new byte[1280 * 720 * 3]; // Adjust for resolution and color format
//     }

//     void OnApplicationQuit()
//     {
//         ffmpegProcess?.Kill();
//     }
// }