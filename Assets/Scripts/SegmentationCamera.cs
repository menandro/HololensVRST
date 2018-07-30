using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// This script sends first two 4-byte int corresponding
/// to height and width of the detected camera.
/// The succeeding stream is a height*width*4 bytes of image
/// data.
/// 
/// Create a tcp server with ip_addr and port that:
/// 1. Fetch int as height
/// 2. Fetch int as width
/// 3. While true:
///     Fetch height*width*4 bytes
/// </summary>

public class SegmentationCamera : MonoBehaviour {
    WebCamTexture webcamTexture;

    // Semantic Segmentation
    bool isSegmentRequesting = false;
    SocketClient segmentationClient;
    private string ip_addr = "192.168.1.125";
    private string port = "13010";
    bool isTryConnecting = false;
    bool isTrySendingImage = false;
    int imageHeight;
    int imageWidth;
    int imageTotalByteSize;
    GCHandle handle;
    IntPtr colorPtr;
    Color32[] colors;
    byte[] colorBytes;

    // Use this for initialization
    void Start () {
        webcamTexture = new WebCamTexture(896, 504);
        webcamTexture.Play();

        imageHeight = webcamTexture.height;
        imageWidth = webcamTexture.width;
        imageTotalByteSize = imageHeight * imageWidth * 4;
        DebugToServer.Log.Send(imageHeight.ToString() + " " + imageWidth.ToString());

        colors = new Color32[webcamTexture.width * webcamTexture.height];
        colorBytes = new byte[imageTotalByteSize];

        segmentationClient = new SocketClient();
        Task<bool> result = Task.Factory.StartNew(() => TryConnect());

        handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
        colorPtr = handle.AddrOfPinnedObject();
    }

    private bool TryConnect()
    {
        isTryConnecting = true;
        var res = Task.Run(async () => await segmentationClient.Connect(ip_addr, port)).Result; // Blocking

        // Send info
        segmentationClient.SendInt(imageHeight);
        segmentationClient.SendInt(imageWidth);
        isTryConnecting = false;
        return true;
    }

    // Update is called once per frame
    void Update () {
        webcamTexture.GetPixels32(colors);

        if ((!isTryConnecting) && (segmentationClient._connected) && (!isTrySendingImage)){
            // Retreive segmentation frame
            isTrySendingImage = true;
            Task<bool> result = Task.Factory.StartNew(() => TrySendImage(colors));
        }
    }

    private bool TrySendImage(Color32[] colors)
    {
        isTrySendingImage = true;

        Marshal.Copy(colorPtr, colorBytes, 0, imageTotalByteSize);
        
        segmentationClient.SendBytes(imageHeight * imageWidth * 4, colorBytes);
        isTrySendingImage = false;
        return true;
    }

    void OnApplicationQuit()
    {
        handle.Free();
        segmentationClient.Disconnect();
    }
}
