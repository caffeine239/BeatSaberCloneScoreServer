using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class NetworkClass
{
    public volatile bool listenSocket = true;
    TcpListener listener;
    NetworkSession session;

    public bool Start(string host, int port)
    {
        try
        {
            listener = new TcpListener(IPAddress.Parse(host), port);
            listener.Start();

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public void AcceptConnectionThread()
    {
        new Thread(AcceptConnection).Start();
    }

    async void AcceptConnection()
    {
        while (listenSocket)
        {
            Thread.Sleep(1);
            if (listener.Pending())
            {
                session = new NetworkSession();
                session.AuthSocket = listener.AcceptSocket();

                Thread NewThread = new Thread(session.InitAuth);
                NewThread.Start();
            }
        }
    }
}