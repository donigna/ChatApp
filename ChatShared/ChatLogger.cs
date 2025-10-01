using System;
using System.Collections.Generic;

namespace ChatShared
{
    public static class ChatLogger
    {
        // List untuk menampung riwayat chat
        private static readonly List<string> _history = new();

        
        // Menyimpan pesan ke riwayat
        
        public static void Log(string message)
        {
            _history.Add(message);
        }

        
        // Mengambil seluruh riwayat chat
        
        public static IEnumerable<string> GetHistory()
        {
            // Return salinan agar aman dari modifikasi luar
            return _history.ToArray();
        }

        
        // Menghapus seluruh riwayat (dipanggil saat server restart)
        
        public static void Clear()
        {
            _history.Clear();
        }
    }
}
