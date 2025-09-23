using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ChatClient;

public partial class MainWindow : Window
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread listenerThread;

    public MainWindow()
    {
        InitializeComponent();

        
        try
        {
            client = new TcpClient("127.0.0.1", 5000);
            stream = client.GetStream();
            AppendMessage("Connected to server.");

            
            listenerThread = new Thread(ListenForMessages);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
        catch (Exception ex)
        {
            AppendMessage("Error connecting to server: " + ex.Message);
        }

        UserList.Items.Add("Ana");
        UserList.Items.Add("Dina");
        UserList.Items.Add("Udin");

        SendButton.Click += OnSend;
    }

    private void OnSend(object? sender, RoutedEventArgs e)
    {
        var text = ChatInput.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        string message;

        if (text.StartsWith("/W"))
        {
            var parts = text.Split(' ', 3);
            if (parts.Length >= 3)
            {
                string targetUser = parts[1];
                string pmText = parts[2];
                message = $"[PM to {targetUser}] {pmText}";
            }
            else
            {
                message = "Format salah. Gunakan: /w <user> <pesan>";
            }
        }
        else
        {
            message = $"You: {text}";
        }

        SendMessageToServer(message);
        ChatInput.Text = string.Empty;
    }

    private void SendMessageToServer(string message)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }

    private void ListenForMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        try
        {
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Dispatcher.UIThread.Post(() =>
                {
                    AppendMessage(message);
                });
            }
        }
        catch
        {
            Dispatcher.UIThread.Post(() =>
            {
                AppendMessage("Disconnected from server.");
            });
        }
    }

    private void AppendMessage(string message)
    {
        ChatPanel.Children.Add(new TextBlock
        {
            Text = $"{DateTime.Now:HH:mm} {message}"
        });
    }
}
