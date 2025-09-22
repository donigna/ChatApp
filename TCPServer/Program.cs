using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static TcpListener server;
    static List<TcpClient> clients = new List<TcpClient>();
    static object lockObj = new object();

    static void Main(string[] args)
    {
        int port = 5000;
        server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine("Server started on port " + port);

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            lock (lockObj) clients.Add(client);

            Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

            Thread clientThread = new Thread(HandleClient);
            clientThread.IsBackground = true;
            clientThread.Start(client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        try
        {
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received from {client.Client.RemoteEndPoint}: {message}");

                
                Broadcast(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            lock (lockObj) clients.Remove(client);
            Console.WriteLine("Client disconnected: " + client.Client.RemoteEndPoint);
            client.Close();
        }
    }

    static void Broadcast(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch
                {
                    
                }
            }
        }
    }
}
