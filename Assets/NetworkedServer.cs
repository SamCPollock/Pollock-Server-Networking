using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    LinkedList<PlayerAccount> playerAccounts;


    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

        playerAccounts = new LinkedList<PlayerAccount>();
    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');
        int signifier = int.Parse(csv[0]);

        if (signifier == ClientToServerSignifiers.CreateAccountAttempt)
        {
            string name = csv[1];
            string pass  = csv[2];

            bool userNameAlreadyInUse = false;

            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == name)
                {
                    userNameAlreadyInUse = true; 
                    break;
                }
            }

            if (!userNameAlreadyInUse)
            {
                PlayerAccount pa = new PlayerAccount(name, pass);
                playerAccounts.AddLast(pa);
                SendMessageToClient(ServerToClientSignifiers.CreateAccountSuccess + "", id);
            }
            else // Checking if signifier was something else.
            {
                // Out of bounds! 
                SendMessageToClient(ServerToClientSignifiers.CreateAccountFailure + "", id);

            }

        }
        else if (signifier == ClientToServerSignifiers.LoginAttempt)
        {
            string name = csv[1];
            string pass = csv[2];

            bool userNameFound = false;

            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == name)
                {
                    userNameFound = true;
                    if (pa.password == pass)
                    {
                        SendMessageToClient(ServerToClientSignifiers.LoginSuccess + "", id);
                    }
                    else
                    {
                        SendMessageToClient(ServerToClientSignifiers.LoginFailure + "", id);
                    }
                }

            }

            if (!userNameFound)
            {
                SendMessageToClient(ServerToClientSignifiers.LoginFailure + "", id);
            }

        }

    }

}


public class PlayerAccount
{
    public string name;
    public string password; 

    public PlayerAccount(string Name, string Password)
    {
        name = Name;
        password = Password;
    }
}


public static class ServerToClientSignifiers
{
    public const int CreateAccountSuccess = 1;
    public const int LoginSuccess = 2;

    public const int CreateAccountFailure = 3;
    public const int LoginFailure = 4;

}

public static class ClientToServerSignifiers
{
    public const int CreateAccountAttempt = 1;
    public const int LoginAttempt = 2;

}
