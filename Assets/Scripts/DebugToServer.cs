using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class DebugToServer : MonoBehaviour
{
    private string ip_addr = "192.168.1.120";
    private string port = "12345";

    public static SocketClient Log = new SocketClient();

    // Use this for initialization
    void Start()
    {
        TryConnect();
        //var res = Task.Run(async () => await Log.Connect(ip_addr, port)).Result;
        //Log.Send("Debugger connected.");
    }

    public async void TryConnect()
    {
        await Log.Connect(ip_addr, port);
    }

    void OnApplicationQuit()
    {
        Log.Disconnect();
    }
}
