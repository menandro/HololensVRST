using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

#if !UNITY_EDITOR
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
#endif

public class CameraMainCapture
{
#if !UNITY_EDITOR
    public MediaFrameSourceGroup depthGroup = null; //contains depth and positional cameras
    public MediaFrameSourceGroup webcamGroup = null;

    public MediaFrameSourceInfo depthFarInfo = null;
    public MediaFrameSourceInfo depthNearInfo = null;
    public MediaFrameSourceInfo infraredFarInfo = null;
    public MediaFrameSourceInfo infraredNearInfo = null;
    public MediaFrameSourceInfo leftSideInfo = null;
    public MediaFrameSourceInfo leftFrontInfo = null;
    public MediaFrameSourceInfo rightSideInfo = null;
    public MediaFrameSourceInfo rightFrontInfo = null;
    public MediaFrameSourceInfo webcamInfo = null;

    public MediaFrameReader webcamFrameReader;
    public MediaCapture webcamCapture;
    public SoftwareBitmap webcamBackBuffer;
    public byte[] webcamBytes;// = new byte[896 * 504 * 4];

    public MediaFrameReader depthFarFrameReader;
    public MediaCapture depthFarCapture;
    public SoftwareBitmap depthFarBackBuffer;
    public byte[] depthFarBytes;// = new byte[448 * 450 * 2];

    public MediaFrameReader depthNearFrameReader;
    public MediaCapture depthNearCapture;
    public SoftwareBitmap depthNearBackBuffer;
    public byte[] depthNearBytes;// = new byte[448 * 450 * 2];

    public MediaFrameReader leftSideFrameReader;
    public MediaCapture leftSideCapture;
    public SoftwareBitmap leftSideBackBuffer;
    public byte[] leftSideBytes;// = new byte[448 * 450 * 2];
#endif

    public bool isReadyToSend = false;
    public bool dataSending = false;

    public async void StartDataStream()
    {
#if UNITY_EDITOR
        //DebugToServer.Log.Send("Camera test is only for Hololens.");
#endif

#if !UNITY_EDITOR
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        GetStreamingGroupsAndInfos(frameSourceGroups);
        await StartWebcamCapture();
        //await StartDepthFarCapture();
        //await StartDepthNearCapture();
        //await StartFourCamerasCapture();
        isReadyToSend = true;
#endif
    }


#if !UNITY_EDITOR
    // Find all streaming cameras and sensors
    //public async Task<IReadOnlyList<MediaFrameSourceGroup>> GetMediaFrameSourceGroup()
    //{
    //    var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
    //    return frameSourceGroups;
    //}

    // Assign the groups and infos to their appropriate names
    private void GetStreamingGroupsAndInfos(IReadOnlyList<MediaFrameSourceGroup> frameSourceGroups)
    {
        /// Group and Info structure for Hololens V1
        /// Assuming all feeds are working
        /// Group 0:
        ///     Info 0: Depth VideoRecord
        ///     Info 1: Infrared VideoRecord
        ///     Info 2: Depth VideoRecord
        ///     Info 3: Infrared VideoRecord
        ///     Info 4: Color VideoRecord
        ///     Info 5: Color VideoRecord
        ///     Info 6: Color VideoRecord
        ///     Info 7: Color VideoRecord
        /// Group 0:
        ///     Info 0: Color VideoPreview
        ///     Info 1: Color VideoRecord
        ///     Info 2: 5 Photo
        if (frameSourceGroups.Count >= 2)
        {
            depthGroup = frameSourceGroups[0];
            webcamGroup = frameSourceGroups[1];
        }
        else
        {
            DebugToServer.Log.Send("SG: " + frameSourceGroups.Count.ToString());
            return;
        }

        if (depthGroup.SourceInfos.Count >= 8)
        {
            depthNearInfo = depthGroup.SourceInfos[0];
            infraredFarInfo = depthGroup.SourceInfos[1];
            depthFarInfo = depthGroup.SourceInfos[2];
            infraredNearInfo = depthGroup.SourceInfos[3];
            leftFrontInfo = depthGroup.SourceInfos[4];
            leftSideInfo = depthGroup.SourceInfos[5];
            rightFrontInfo = depthGroup.SourceInfos[6];
            rightSideInfo = depthGroup.SourceInfos[7];
        }
        else
        {
            DebugToServer.Log.Send("Sensor group missing infos.");
            //DebugToServer.Log.Send("Sensor group missing infos.");
            return;
        }

        if (webcamGroup.SourceInfos.Count >= 2)
        {
            webcamInfo = webcamGroup.SourceInfos[0];
        }
        else
        {
            //DebugToServer.debugClient.SendDebug("Webcam group missing infos.");
            return;
        }
    }

    //********************************************************
    //                  MAIN CAMERA CAPTURE
    //********************************************************
    public async Task<string> StartWebcamCapture()
    {
        //webcamBytes = new byte[896 * 504 * 4];
        webcamBytes = new byte[1280 * 720 * 4];
        webcamCapture = new MediaCapture();
        try
        {
            await webcamCapture.InitializeAsync(
                new MediaCaptureInitializationSettings()
                {
                    SourceGroup = webcamGroup,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                }
            );
        }
        catch (Exception ex)
        {
            return null;
        }

        var colorFrameSource = webcamCapture.FrameSources[webcamInfo.Id];
        //var preferredFormat = colorFrameSource.SupportedFormats.Where(format =>
        //{
        //    return format.VideoFormat.Width == 896
        //    && format.VideoFormat.Height == 504 &&
        //    format.FrameRate.Denominator == 1001 &&
        //    format.FrameRate.Numerator == 30000;

        //}).FirstOrDefault();

        //if (preferredFormat == null)
        //{
        //    preferredFormat = colorFrameSource.SupportedFormats.FirstOrDefault();
        //}
        var preferredFormat = colorFrameSource.SupportedFormats.FirstOrDefault();
        await colorFrameSource.SetFormatAsync(preferredFormat);

        webcamFrameReader = await webcamCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Argb32);
        webcamFrameReader.FrameArrived += WebcamFrameReader_FrameArrived;
        webcamFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
        await webcamFrameReader.StartAsync();
        
        return null;
    }

    //Webcam Frame Arrived handler function
    private void WebcamFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var webcamFrameReference = sender.TryAcquireLatestFrame();
        var videoMediaFrame = webcamFrameReference?.VideoMediaFrame;
        var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

        if (softwareBitmap != null)
        {
            // Swap the processed frame to _backBuffer and dispose of the unused image.
            // Done because videoMediaFrame.SoftwareBitmap is not automatically disposed.
            softwareBitmap = Interlocked.Exchange(ref webcamBackBuffer, softwareBitmap);
            softwareBitmap?.Dispose();
            webcamBackBuffer.CopyToBuffer(webcamBytes.AsBuffer()); //data written to buffer even while sending
        }
        webcamFrameReference.Dispose();
    }

    //********************************************************
    //                  DEPTH FAR CAPTURE
    //********************************************************
    public async Task<string> StartDepthFarCapture()
    {
        depthFarBytes = new byte[450 * 448 * 2];
        depthFarCapture = new MediaCapture();
        try
        {
            await depthFarCapture.InitializeAsync(
                new MediaCaptureInitializationSettings()
                {
                    SourceGroup = depthGroup,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                }
            );
        }
        catch (Exception ex)
        {
            return null;
        }

        var depthFrameSource = depthFarCapture.FrameSources[depthFarInfo.Id];
        var preferredFormat = depthFrameSource.SupportedFormats.FirstOrDefault();
        await depthFrameSource.SetFormatAsync(preferredFormat);

        depthFarFrameReader = await depthFarCapture.CreateFrameReaderAsync(depthFrameSource, MediaEncodingSubtypes.D16);
        depthFarFrameReader.FrameArrived += DepthFarFrameReader_FrameArrived;
        await depthFarFrameReader.StartAsync();
        return null;
    }

    //Depth Far Frame Arrived handler function
    private void DepthFarFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var depthFarFrameReference = sender.TryAcquireLatestFrame();
        var videoMediaFrame = depthFarFrameReference?.VideoMediaFrame;
        var softwareBitmap = videoMediaFrame?.SoftwareBitmap;
        
        if (softwareBitmap != null)
        {
            // Swap the processed frame to _backBuffer and dispose of the unused image.
            // Done because videoMediaFrame.SoftwareBitmap is not automatically disposed.
            softwareBitmap = Interlocked.Exchange(ref depthFarBackBuffer, softwareBitmap);
            softwareBitmap?.Dispose();
            depthFarBackBuffer.CopyToBuffer(depthFarBytes.AsBuffer()); //data written to buffer even while sending
        }
        depthFarFrameReference.Dispose();
    }


    //********************************************************
    //                  DEPTH NEAR CAPTURE
    //********************************************************
    public async Task<string> StartDepthNearCapture()
    {
        depthNearBytes = new byte[450 * 448 * 2];
        depthNearCapture = new MediaCapture();
        try
        {
            await depthNearCapture.InitializeAsync(
                new MediaCaptureInitializationSettings()
                {
                    SourceGroup = depthGroup,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                }
            );
        }
        catch (Exception ex)
        {
            return null;
        }

        var depthFrameSource = depthNearCapture.FrameSources[depthNearInfo.Id];
        var preferredFormat = depthFrameSource.SupportedFormats.FirstOrDefault();
        await depthFrameSource.SetFormatAsync(preferredFormat);

        depthNearFrameReader = await depthNearCapture.CreateFrameReaderAsync(depthFrameSource, MediaEncodingSubtypes.D16);
        depthNearFrameReader.FrameArrived += DepthNearFrameReader_FrameArrived;
        await depthNearFrameReader.StartAsync();
        return null;
    }

    //Depth Near Frame Arrived handler function
    private void DepthNearFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var depthNearFrameReference = sender.TryAcquireLatestFrame();
        var videoMediaFrame = depthNearFrameReference?.VideoMediaFrame;
        var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

        if (softwareBitmap != null)
        {
            // Swap the processed frame to _backBuffer and dispose of the unused image.
            // Done because videoMediaFrame.SoftwareBitmap is not automatically disposed.
            softwareBitmap = Interlocked.Exchange(ref depthNearBackBuffer, softwareBitmap);
            softwareBitmap?.Dispose();
            depthNearBackBuffer.CopyToBuffer(depthNearBytes.AsBuffer()); //data written to buffer even while sending
        }
        depthNearFrameReference.Dispose();
    }


    //********************************************************
    //   LEFTSIDE, LEFTFRONT, RIGHTSIDE, RIGHTFRONT CAPTURE
    //********************************************************
    public async Task<string> StartFourCamerasCapture()
    {
        leftSideBytes = new byte[480 * 160 * 4];
        leftSideCapture = new MediaCapture();
        try
        {
            await leftSideCapture.InitializeAsync(
                new MediaCaptureInitializationSettings()
                {
                    SourceGroup = depthGroup,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                }
            );
        }
        catch (Exception ex)
        {
            return null;
        }

        var colorFrameSource = depthNearCapture.FrameSources[leftSideInfo.Id];
        var preferredFormat = colorFrameSource.SupportedFormats.FirstOrDefault();
        await colorFrameSource.SetFormatAsync(preferredFormat);

        leftSideFrameReader = await depthNearCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Argb32);
        leftSideFrameReader.FrameArrived += leftSideFrameReader_FrameArrived;
        await leftSideFrameReader.StartAsync();
        return null;
    }

    //Depth Near Frame Arrived handler function
    private void leftSideFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var frameReference = sender.TryAcquireLatestFrame();
        var videoMediaFrame = frameReference?.VideoMediaFrame;
        var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

        if (softwareBitmap != null)
        {
            // Swap the processed frame to _backBuffer and dispose of the unused image.
            // Done because videoMediaFrame.SoftwareBitmap is not automatically disposed.
            softwareBitmap = Interlocked.Exchange(ref leftSideBackBuffer, softwareBitmap);
            softwareBitmap?.Dispose();
            leftSideBackBuffer.CopyToBuffer(leftSideBytes.AsBuffer()); //data written to buffer even while sending
        }
        frameReference.Dispose();
    }

#endif

    // Update is called once per frame
    //    void Update()
    //    {
    //#if !UNITY_EDITOR
    //        if (isReadyToSend)
    //        {
    //            var task = Task.Run(async () =>
    //            {
    //                if (dataSending)
    //                {
    //                    return;
    //                }
    //                dataSending = true;

    //                //int a = (int)SocketClient.Header.WEBCAM;
    //                FrameRateDisp.text = SocketClient.header[SocketClient.HeaderID.WEBCAM] + webcamBytes.Length.ToString();
    //                await MDataAsync(SocketClient.HeaderID.WEBCAM, webcamBytes.Length, webcamBytes);
    //                //await MDataAsync(SocketClient.HeaderID.DEPTHFAR, depthFarBytes.Length, depthFarBytes);
    //                //await MDataAsync(SocketClient.HeaderID.DEPTHNEAR, depthNearBytes.Length, depthNearBytes);
    //                //await MDataAsync(SocketClient.HeaderID.LEFTSIDE, leftSideBytes.Length, leftSideBytes);
    //                dataSending = false;
    //            });
    //        }
    //#endif
    //    }
}