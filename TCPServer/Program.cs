using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ChatMessage
{
    public IEnumerable<string>? users { get; set; } // khusus untuk userlist
    public string type { get; set; } = "msg";
    public string from { get; set; } = "";
    public string? to { get; set; }
    public string text { get; set; } = "";
    public long ts { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

class Program
{
    static TcpListener? _listener;
    static readonly ConcurrentDictionary<TcpClient, string> _clients = new();

    static async Task Main(string[] args)
    {
        int port = 5000;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Console.WriteLine($"Server started on port {port}...");

        while (true)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[4096];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // client disconnect

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var msg = JsonSerializer.Deserialize<ChatMessage>(json);

                if (msg != null)
                {
                    if (!_clients.ContainsKey(client) && msg.type == "join")
                    {
                        _clients[client] = msg.from;
                        Console.WriteLine($"[{msg.from}] joined.");
                        await BroadcastSystemMessage($"{msg.from} joined.");
                        await BroadcastUserList();
                    }

                    if (msg.type == "msg")
                    {
                        // Console.WriteLine($"[{msg.from}] {msg.text}");
                        await BroadcastAsync(msg);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (_clients.TryRemove(client, out string? username))
            {
                Console.WriteLine($"[{username}] left.");
                await BroadcastSystemMessage($"{username} left.");
                await BroadcastUserList();
            }

            client.Close();
        }
    }

    private static async Task BroadcastAsync(ChatMessage msg)
    {
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.UTF8.GetBytes(json);

        foreach (var kv in _clients)
        {
            try
            {
                var stream = kv.Key.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch
            {
                // ignore error, client mungkin disconnect
            }
        }
    }

    private static async Task BroadcastSystemMessage(string text)
    {
        var sysMsg = new ChatMessage
        {
            type = "sys",
            from = "server",
            text = text,
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await BroadcastAsync(sysMsg);
    }

    private static async Task BroadcastUserList()
    {
        var listMsg = new ChatMessage
        {
            type = "userlist",
            from = "server",
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            users = _clients.Values
        };
        await BroadcastAsync(listMsg);
    }
}
