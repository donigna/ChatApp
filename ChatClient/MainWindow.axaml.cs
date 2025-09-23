using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
public class ChatMessage
{
    public string[]? users { get; set; }
    public string type { get; set; } = "msg";
    public string from { get; set; } = "";
    public string? to { get; set; }
    public string text { get; set; } = "";
    public long ts { get; set; }
}

namespace ChatClient
{


    public partial class MainWindow : Window
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private ObservableCollection<string> _users = new();
        private string _username = "";

        public MainWindow()
        {
            InitializeComponent();

            UserList.ItemsSource = _users;

            ConnectButton.Click += ConnectButton_Click;
            DisconnectButton.Click += DisconnectButton_Click;
            SendButton.Click += SendButton_Click;
            // try
            // {
            //     client = new TcpClient("127.0.0.1", 5000);
            //     stream = client.GetStream();
            //     AppendMessage("Connected to server.");


            //     listenerThread = new Thread(ListenForMessages);
            //     listenerThread.IsBackground = true;
            //     listenerThread.Start();
            // }
            // catch (Exception ex)
            // {
            //     AppendMessage("Error connecting to server: " + ex.Message);
            // }

            // SendButton.Click += OnSend;
        }

        private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string[] parts = (ServerInput.Text ?? "127.0.0.1:5000").Split(":");
                string host = parts[0];
                int port = int.Parse(parts[1]);

                _username = UsernameInput.Text ?? "Anonymous";

                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();

                _ = ListenAsync();

                var joinMsg = new ChatMessage { type = "join", from = _username };
                await SendAsync(joinMsg);

                AppendMessage($"Connected as {_username}");

                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AppendMessage($"Error: {ex.Message}");
            }
        }

        private void DisconnectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_client != null && _client.Connected)
            {
                _stream?.Close();
                _client.Close();
            }

            AppendMessage("Disconnected.");
            _users.Clear();

            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
        }

        private async void SendButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_stream == null || !_client!.Connected) return;

            var msg = new ChatMessage
            {
                type = "msg",
                from = _username,
                text = ChatInput.Text ?? ""
            };

            await SendAsync(msg);

            ChatInput.Text = "";
        }

        private async Task ListenAsync()
        {
            if (_stream == null) return;

            byte[] buffer = new byte[4096];

            try
            {
                while (_client != null && _client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var msg = JsonSerializer.Deserialize<ChatMessage>(json);

                    if (msg != null)
                    {
                        switch (msg.type)
                        {
                            case "msg":
                                AppendMessage($"[{msg.from}] {msg.text}");
                                break;
                            case "sys":
                                if (msg.from != _username)
                                    AppendMessage($"* {msg.text}");
                                break;
                            case "userlist":
                                if (msg.users != null)
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        _users.Clear();
                                        foreach (var u in msg.users)
                                        {
                                            _users.Add(u);
                                        }
                                    });
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"Connection lost: {ex.Message}");
            }
        }

        private async Task SendAsync(ChatMessage msg)
        {
            if (_stream == null) return;

            string json = JsonSerializer.Serialize(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        private void AppendMessage(string message)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = $"{DateTime.Now:HH:mm} {message}",
                Margin = new Avalonia.Thickness(2)
            });
        }
    }
}
