using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Threading.Tasks;

public class SocketClient
{
    Stream streamOut;
    Stream streamIn;
    StreamWriter writer;
    StreamReader reader;
    public bool _connected = false;
    private bool _canConnect = true;

    public SocketClient() { }

#if UNITY_EDITOR
    public System.Net.Sockets.TcpClient connectSocket;

    public async System.Threading.Tasks.Task<string> Connect(string ip_addr, string port)
    {
        connectSocket = new System.Net.Sockets.TcpClient();
        try
        {
            await connectSocket.ConnectAsync(ip_addr, Int32.Parse(port));
        }
        catch (Exception ex)
        {
            _canConnect = false;
            Debug.Log(ex);
            throw new Exception("Server not found at: " + ip_addr);
        }
        streamOut = connectSocket.GetStream();
        writer = new StreamWriter(streamOut);
        reader = new StreamReader(streamOut);
        _connected = true;
        return null;
    }

    public void Disconnect()
    {
        connectSocket.Close();
    }
#endif

#if !UNITY_EDITOR
    public Windows.Networking.Sockets.StreamSocket connectSocket;

    public async System.Threading.Tasks.Task<string> Connect(string ip_addr, string port)
    {
        connectSocket = new Windows.Networking.Sockets.StreamSocket();
        Windows.Networking.HostName serverHost = new Windows.Networking.HostName(ip_addr);
        try {
            await connectSocket.ConnectAsync(serverHost, port);
        }
        catch {
            _canConnect = false;
            _connected = false;
            throw new Exception("Server not found.");
        }
        streamOut = connectSocket.OutputStream.AsStreamForWrite();
        streamIn = connectSocket.InputStream.AsStreamForRead();
        writer = new StreamWriter(streamOut);
        reader = new StreamReader(streamIn);
        _connected = true;
        return null;
    }

    public void Disconnect()
    {
        connectSocket.Dispose();
    }
#endif
    
    private async System.Threading.Tasks.Task<string> SendMessageSizeAsync(Int32 value)
    {
        char[] chars = new char[4];
        chars[0] = (char)(value & 0xFF);
        chars[1] = (char)((value >> 8) & 0xFF);
        chars[2] = (char)((value >> 16) & 0xFF);
        chars[3] = (char)((value >> 24) & 0xFF);
        await writer.WriteAsync(chars, 0, 4);
        await writer.FlushAsync();
        return null;
    }

    private void SendMessageSize(int value)
    {
        char[] chars = new char[4];
        chars[0] = (char)(value & 0xFF);
        chars[1] = (char)(value >> 8 & 0xFF);
        chars[2] = (char)(value >> 16 & 0xFF);
        chars[3] = (char)(value >> 24 & 0xFF);
        writer.Write(chars, 0, 4);
        writer.Flush();
    }

    public void SendInt(int value)
    {
        byte[] byteArray = BitConverter.GetBytes(value);
        streamOut.Write(byteArray, 0, 4);
        streamOut.Flush();
    }

    public async System.Threading.Tasks.Task<string> SendAsync(string request)
    {
        if (_connected)
        {
            await writer.WriteAsync("<debug>");
            await writer.FlushAsync();
            await SendMessageSizeAsync(request.Length);
            await writer.WriteAsync(request);
            await writer.FlushAsync();
        }
        return null;
    }

    public void Send(string request)
    {
        if (_connected)
        {
            writer.Write("<debug>");
            writer.Flush();
            SendMessageSize(request.Length);
            writer.Write(request);
            writer.Flush();
        }
    }

    public void SendBytes(byte[] bytes)
    {
        if (_connected)
        {
            streamOut.Write(bytes, 0, bytes.Length);
            streamOut.Flush();
        }
    }

    public void SendBytes(int size, byte[] bytes)
    {
        if (_connected)
        {
            streamOut.Write(bytes, 0, size);
            streamOut.Flush();
        }
    }

    public void SendWebcamImage(int size, byte[] bytes)
    {
        if (_connected)
        {
            writer.Write("<webcam>");
            writer.Flush();
            SendMessageSize(size);
            streamOut.Write(bytes, 0, size);
            streamOut.Flush();
        }
    }

    public async System.Threading.Tasks.Task<string> SendWebcamImageAsync(int size, byte[] bytes)
    {
        if (_connected)
        {
            await writer.WriteAsync("<webcam>");
            await writer.FlushAsync();
            await SendMessageSizeAsync(size);
            await streamOut.WriteAsync(bytes, 0, size);
            await streamOut.FlushAsync();
        }
        return null;
    }

    public void Receive(int size, ref byte[] data)
    {
        //Read all bytes
        int totalrecvsize = 0;
        int recvsize = 0;
        int remainsize = size;
        using (MemoryStream ms = new MemoryStream())
        {
            byte[] buffer = new byte[size];
            while (totalrecvsize < size)
            {
                remainsize = size - totalrecvsize;
#if UNITY_EDITOR
                recvsize = streamOut.Read(buffer, 0, remainsize);
#endif
#if !UNITY_EDITOR
                recvsize = streamIn.Read(buffer, 0, remainsize);
#endif
                ms.Write(buffer, 0, recvsize);
                totalrecvsize += recvsize;
            }
            data = ms.ToArray();
        }
    }

    public async Task<byte[]> ReceiveAsync(int size)
    {
        //Read all bytes
        byte[] data = new byte[size];
        int totalrecvsize = 0;
        int recvsize = 0;
        int remainsize = size;
        using (MemoryStream ms = new MemoryStream())
        {
            byte[] buffer = new byte[size];
            while (totalrecvsize < size)
            {
                remainsize = size - totalrecvsize;
#if UNITY_EDITOR
                //var task = streamOut.ReadAsync(buffer, 0, remainsize);
                recvsize = await Task.Run(() => streamOut.ReadAsync(buffer, 0, remainsize));
#endif
#if !UNITY_EDITOR
                recvsize = streamIn.Read(buffer, 0, remainsize);
#endif
                ms.Write(buffer, 0, recvsize);
                totalrecvsize += recvsize;
            }
            data = ms.ToArray();
        }
        return data;
    }
}