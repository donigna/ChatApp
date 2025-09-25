using System;
namespace ChatShared
{
    public enum MessageType
    {
        Connect,
        Disconnect,
        Broadcast,
        Private,
        UserList,
        System
    }

    public class ChatMessage
    {
        public MessageType Type { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Text { get; set; }
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public string[] Users { get; set; } = [];
    }
}

