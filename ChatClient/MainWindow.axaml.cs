using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatClient;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        UserList.Items.Add("Ana");
        UserList.Items.Add("Dina");
        UserList.Items.Add("Udin");

        SendButton.Click += OnSend;
    }

    private void OnSend(object? sender, RoutedEventArgs e)
    {
        var text = ChatInput.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        string user = "You";
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
            message = $"{user}: {text}";
        }

        ChatPanel.Children.Add(new TextBlock
        {
            Text = $"{DateTime.Now:HH:mm} {message}"
        });

        ChatInput.Text = string.Empty;
    }
}