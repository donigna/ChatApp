// Client/MainWindow.cs - Versi Perbaikan
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ChatShared;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatClient;

public partial class MainWindow : Window
{
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _isConnected = false;
    private string _username = "";

    public MainWindow()
    {
        InitializeComponent();
        this.Closing += OnWindowClosing;
    }

    private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        var ip = IpTextBox.Text;
        var portStr = PortTextBox.Text;
        var user = UsernameTextBox.Text;

        if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(portStr) || string.IsNullOrWhiteSpace(user))
        {
            AddMessageToChat("[SYSTEM]: IP, Port, dan Username tidak boleh kosong.", Brushes.DarkOrange);
            return;
        }

        if (!int.TryParse(portStr, out var port))
        {
            AddMessageToChat("[SYSTEM]: Port harus berupa angka.", Brushes.DarkOrange);
            return;
        }

        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);

            _isConnected = true;
            _username = user;
            var stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            var connectMessage = new ChatMessage { Type = MessageType.Connect, From = _username };
            await _writer.WriteLineAsync(JsonSerializer.Serialize(connectMessage));

            ToggleControls(true);
            AddMessageToChat("[SYSTEM]: Tersambung ke server.", Brushes.Green);

            _ = Task.Run(ListenForMessagesAsync);
        }
        catch (Exception ex)
        {
            AddMessageToChat($"[SYSTEM]: Gagal terhubung: {ex.Message}", Brushes.Red);
            CleanupConnection();
        }
    }

    private async Task ListenForMessagesAsync()
    {
        try
        {
            while (_isConnected && _reader != null)
            {
                var jsonMessage = await _reader.ReadLineAsync();
                if (jsonMessage == null) break;

                var message = JsonSerializer.Deserialize<ChatMessage>(jsonMessage);
                if (message == null) continue;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    switch (message.Type)
                    {
                        case MessageType.Broadcast:
                            var sender = message.From == _username ? "You" : message.From;
                            var color = message.From == _username ? Brushes.GreenYellow : Brushes.GreenYellow;
                            AddMessageToChat($"[{sender}]: {message.Text}", color);
                            break;
                        case MessageType.Private:
                            AddMessageToChat($"[PM dari {message.From}]: {message.Text}", Brushes.BlueViolet);
                            break;
                        case MessageType.System:
                            AddMessageToChat($"[SYSTEM]: {message.Text}", Brushes.SlateGray);
                            break;
                        case MessageType.UserList:
                            UpdateUserList(message.Users);
                            break;
                        default:
                            AddMessageToChat($"[DEBUG]: Pesan tidak dikenali: {jsonMessage}", Brushes.DarkRed);
                            break;
                    }
                });
            }
        }
        catch (IOException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => AddMessageToChat("[SYSTEM]: Koneksi ke server terputus.", Brushes.Red));
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => AddMessageToChat($"[SYSTEM]: Terjadi error: {ex.Message}", Brushes.Red));
        }
        finally
        {
            CleanupConnection();
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageTextBox.Text) || !_isConnected || _writer == null) return;

        try
        {
            var rawMessage = MessageTextBox.Text;
            ChatMessage chatMessage;

            if (rawMessage.StartsWith("/w "))
            {
                var parts = rawMessage.Split(new[] { ' ' }, 3);
                if (parts.Length == 3)
                {
                    chatMessage = new ChatMessage { Type = MessageType.Private, To = parts[1], Text = parts[2] };
                }
                else
                {
                    AddMessageToChat("[SYSTEM]: Format PM salah. Gunakan: /w <username> <pesan>", Brushes.DarkOrange);
                    return;
                }
            }
            else
            {
                chatMessage = new ChatMessage { Type = MessageType.Broadcast, Text = rawMessage };
            }

            await _writer.WriteLineAsync(JsonSerializer.Serialize(chatMessage));
            MessageTextBox.Text = string.Empty;
        }
        catch (Exception ex)
        {
            AddMessageToChat($"[SYSTEM]: Gagal mengirim pesan: {ex.Message}", Brushes.Red);
        }
    }

    private void UpdateUserList(string[] users)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            UserList.ItemsSource = users;
        });
    }

    private void DisconnectButton_Click(object? sender, RoutedEventArgs e) => CleanupConnection();
    private void OnWindowClosing(object? sender, CancelEventArgs e) => CleanupConnection();

    private void CleanupConnection()
    {
        if (!_isConnected) return;
        _isConnected = false;
        UserList.ItemsSource = null;

        try
        {
            _writer?.Close();
            _reader?.Close();
            _client?.Close();
        }
        catch { }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ToggleControls(false);
            AddMessageToChat("[SYSTEM]: Koneksi ditutup.", Brushes.Gray);
        });
    }

    private void AddMessageToChat(string text, IBrush? color = null)
    {
        var messageBlock = new TextBlock
        {
            Text = $"[{DateTime.Now:HH:mm:ss}] {text}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = color ?? Brushes.Black,
        };
        ChatPanel.Children.Add(messageBlock);

        ChatScrollViewer.ScrollToEnd();
    }

    private void ToggleControls(bool connected)
    {
        IpTextBox.IsEnabled = !connected;
        PortTextBox.IsEnabled = !connected;
        UsernameTextBox.IsEnabled = !connected;
        ConnectButton.IsEnabled = !connected;

        DisconnectButton.IsEnabled = connected;
        MessageTextBox.IsEnabled = connected;
        SendButton.IsEnabled = connected;
    }

    private async void SendButton_Click(object? sender, RoutedEventArgs e) => await SendMessageAsync();
    private async void MessageTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SendMessageAsync();
        }
    }
}