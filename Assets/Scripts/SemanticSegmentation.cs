using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;

#if !UNITY_EDITOR
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
#endif

public class SemanticSegmentation : MonoBehaviour {
    public string ip_addr = "157.82.140.103";
    public string port = "12345";

    private CameraMainCapture webcam;
    private SocketClient segmentationServer;

    //private byte[] webcamBytesSmall;

    // Use this for initialization
    void Start () {
        segmentationServer = new SocketClient();
        var res = Task.Run(async () => await segmentationServer.Connect(ip_addr, port)).Result;
        webcam = new CameraMainCapture();
        webcam.StartDataStream();
        //webcamBytesSmall = new byte[320 * 180 * 4];
    }
	
	// Update is called once per frame
	void Update () {
        UpdateSemanticSegmentation();
    }

    //Loads a new frame with semantic label
    void UpdateSemanticSegmentation()
    {
#if !UNITY_EDITOR
        if (webcam.isReadyToSend)
        {
            var task = Task.Run(async () =>
            {
                if (webcam.dataSending)
                {
                    return;
                }
                webcam.dataSending = true;

                await segmentationServer.SendWebcamImageAsync(webcam.webcamBytes.Length, webcam.webcamBytes);
                //DebugToServer.Log.Send("Resizing");
                //webcamBytesSmall = ResizeImage(webcam.webcamBytes, 320, 180).Result;
                //DebugToServer.Log.Send(webcamBytesSmall.Length.ToString());
                //await segmentationServer.SendWebcamImageAsync(webcamBytesSmall.Length, webcamBytesSmall);
                webcam.dataSending = false;
            });
        }
#endif
    }

#if !UNITY_EDITOR
    //public async Task<byte[]> ResizeImage(byte[] imageData, int reqWidth, int reqHeight)
    //{

    //    var memStream = new MemoryStream(imageData);

    //    IRandomAccessStream imageStream = memStream.AsRandomAccessStream();
    //    var decoder = await BitmapDecoder.CreateAsync(imageStream);
    //    DebugToServer.Log.Send(decoder.PixelWidth.ToString());
    //    if (decoder.PixelHeight > reqHeight || decoder.PixelWidth > reqWidth)
    //    {
    //        using (imageStream)
    //        {
    //            var resizedStream = new InMemoryRandomAccessStream();

    //            BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
    //            double widthRatio = (double)reqWidth / decoder.PixelWidth;
    //            double heightRatio = (double)reqHeight / decoder.PixelHeight;

    //            double scaleRatio = Math.Min(widthRatio, heightRatio);

    //            if (reqWidth == 0)
    //                scaleRatio = heightRatio;

    //            if (reqHeight == 0)
    //                scaleRatio = widthRatio;

    //            uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
    //            uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

    //            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

    //            encoder.BitmapTransform.ScaledHeight = aspectHeight;
    //            encoder.BitmapTransform.ScaledWidth = aspectWidth;

    //            await encoder.FlushAsync();
    //            resizedStream.Seek(0);
    //            var outBuffer = new byte[resizedStream.Size];
    //            await resizedStream.ReadAsync(outBuffer.AsBuffer(), (uint)resizedStream.Size, InputStreamOptions.None);
    //            return outBuffer;
    //        }
    //    }
    //    return imageData;
    //}
#endif


}
