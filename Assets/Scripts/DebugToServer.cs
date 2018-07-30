using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class DebugToServer : MonoBehaviour
{
    private string ip_addr = "192.168.1.125";
    private string port = "12345";

    public static SocketClient Log = new SocketClient();

    // Use this for initialization
    void Start()
    {
        var res = Task.Run(async () => await Log.Connect(ip_addr, port)).Result;
        Log.Send("Debugger connected.");
    }

    void OnApplicationQuit()
    {
        Log.Disconnect();
    }
}
