using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Duongtddse172132_CloudApp1
{
    public partial class MainWindow : Window
    {
        private HubConnection connection;

        public MainWindow()
        {
            InitializeComponent();

            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5258/chathub")
                .Build();

            SendMessageButton.IsEnabled = false;

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AddMessageToChatHistory(user, message);
                });
            });

            StartConnection();
        }

        private async void StartConnection()
        {
            try
            {
                await connection.StartAsync();
                SendMessageButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageTextBox.Text))
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        string user = "User1";
                        string message = MessageTextBox.Text;

                        await connection.InvokeAsync("SendMessage", user, message);
                        //AddMessageToChatHistory(user, message);
                        MessageTextBox.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error sending message: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Connection is not active. Please wait...");
                }
            }
        }

        private void AddMessageToChatHistory(string sender, string message)
        {
            TextBlock messageTextBlock = new TextBlock
            {
                Text = $"{message}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };

            Border messageBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Background = (sender == "User1") ? Brushes.LightBlue : Brushes.LightGray,
                Margin = new Thickness(10, 5, 10, 5),
                Child = messageTextBlock,
                HorizontalAlignment = (sender == "User1") ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            BlockUIContainer blockUIContainer = new BlockUIContainer(messageBorder);

            ChatHistoryRichTextBox.Document.Blocks.Add(blockUIContainer);
            ChatHistoryRichTextBox.ScrollToEnd();
        }


        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, e);
            }
        }
    }
}
