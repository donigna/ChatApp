using ChatShared;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class Server
{
    private static readonly ConcurrentDictionary<string, StreamWriter> s_clients = new();
    private static readonly ConcurrentDictionary<string, TcpClient> s_clientConnections = new();


    public static async Task Main(string[] args)
    {
        var server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        Console.WriteLine("Server dimulai di port 8888. Menunggu koneksi...");

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        string username = string.Empty;

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        try
        {
            // --- Proses Koneksi Awal ---
            var jsonConnectMessage = await reader.ReadLineAsync();
            if (jsonConnectMessage == null) return;

            var connectMessage = JsonSerializer.Deserialize<ChatMessage>(jsonConnectMessage);
            if (connectMessage is not { Type: MessageType.Connect, From: not null })
            {
                return;
            }

            if (!s_clients.TryAdd(connectMessage.From, writer))
            {
                var nameTakenMsg = new ChatMessage { Type = MessageType.System, Text = "Username sudah digunakan." };
                await writer.WriteLineAsync(JsonSerializer.Serialize(nameTakenMsg));
                return;
            }
            username = connectMessage.From;
            s_clientConnections[username] = client;

            Console.WriteLine($"[Koneksi] {username} terhubung.");
            await BroadcastUserListAsync();

            var joinNotification = new ChatMessage { Type = MessageType.System, Text = $"{username} telah bergabung." };
            await BroadcastMessageAsync(joinNotification);

            while (true)
            {
                var jsonMessage = await reader.ReadLineAsync();
                if (jsonMessage == null) break;

                var message = JsonSerializer.Deserialize<ChatMessage>(jsonMessage);
                if (message == null) continue;

                message.From = username;

                switch (message.Type)
                {
                    case MessageType.Broadcast:
                        Console.WriteLine($"[Broadcast] {username}: {message.Text}");
                        await BroadcastMessageAsync(message);
                        break;
                    case MessageType.Private:
                        Console.WriteLine($"[Private] Dari {username} ke {message.To}: {message.Text}");
                        await SendPrivateMessageAsync(message);
                        break;
                }
            }
        }
        catch (IOException)
        {
            Console.WriteLine($"[Info] Koneksi dengan {username} ditutup paksa.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Terjadi error pada client {username}: {ex.Message}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(username) && s_clients.TryRemove(username, out _))
            {
                Console.WriteLine($"[Koneksi] {username} terputus.");
                var leaveNotification = new ChatMessage { Type = MessageType.System, Text = $"{username} telah keluar." };
                await BroadcastMessageAsync(leaveNotification);
                await BroadcastUserListAsync();
            }
            client.Close();
        }
    }

    private static async Task BroadcastMessageAsync(ChatMessage message)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        foreach (var writer in s_clients.Values)
        {
            try
            {
                await writer.WriteLineAsync(jsonMessage);
            }
            catch
            {
            }
        }
    }

    private static async Task BroadcastUserListAsync()
    {
        var userListMsg = new ChatMessage
        {
            Type = MessageType.UserList,
            Users = s_clients.Keys.ToArray()
        };

        var json = JsonSerializer.Serialize(userListMsg);
        foreach (var writer in s_clients.Values)
        {
            try
            {
                await writer.WriteLineAsync(json);
            }
            catch
            {
            }
        }
    }


    private static async Task SendPrivateMessageAsync(ChatMessage message)
    {
        if (message.To != null && s_clients.TryGetValue(message.To, out var targetWriter))
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            await targetWriter.WriteLineAsync(jsonMessage);

            if (message.From != null && s_clients.TryGetValue(message.From, out var sourceWriter))
            {
                var confirmationMsg = new ChatMessage { Type = MessageType.System, Text = $"Pesan Anda ke {message.To} telah terkirim." };
                await sourceWriter.WriteLineAsync(JsonSerializer.Serialize(confirmationMsg));
            }
        }
        else
        {
            if (message.From != null && s_clients.TryGetValue(message.From, out var sourceWriter))
            {
                var errorMsg = new ChatMessage { Type = MessageType.System, Text = $"User '{message.To}' tidak ditemukan atau sedang offline." };
                await sourceWriter.WriteLineAsync(JsonSerializer.Serialize(errorMsg));
            }
        }
    }
}