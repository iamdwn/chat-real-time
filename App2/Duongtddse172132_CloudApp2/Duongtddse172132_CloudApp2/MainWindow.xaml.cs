using Firebase.Auth;
using Firebase.Storage;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Duongtddse172132_CloudApp2
{
    public partial class MainWindow : Window
    {
        private HubConnection connection;
        private List<ChatMessage> chatHistory;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSignalR();
            InitializeFirebase();
        }

        public void InitializeSignalR()
        {
            chatHistory = new List<ChatMessage>();

            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5258/chathub")
                .Build();

            SendMessageButton.IsEnabled = false;

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AddMessageToChatHistory(user, "User2", message);
                });
            });

            StartConnection();
        }

        public void InitializeFirebase()
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("D:\\Docm\\Dev\\.learning-process\\C#\\PRN221_B3W\\chat-real-time\\App1\\Duongtddse172132_CloudApp1\\Duongtddse172132_CloudApp1\\serviceAccount.json"),
                ProjectId = "fir-chathistory",
            });

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
                        string user = "User2";
                        string message = MessageTextBox.Text;

                        await connection.InvokeAsync("SendMessage", user, message);
                        //AddMessageToChatHistory(user, "Receiver", message);
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

        private void AddMessageToChatHistory(string sender, string receiver, string message)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                Sender = sender,
                Receiver = receiver,
                Message = message,
                Timestamp = DateTime.Now
            };

            chatHistory.Add(chatMessage);

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
                Background = (sender == "User2") ? Brushes.LightBlue : Brushes.LightGray,
                Margin = new Thickness(10, 5, 10, 5),
                Child = messageTextBlock,
                HorizontalAlignment = (sender == "User2") ? HorizontalAlignment.Right : HorizontalAlignment.Left
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

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveChatHistory();
        }

        private async void SaveChatHistory()
        {
            try
            {
                var localFilePath = Path.Combine(Directory.GetCurrentDirectory(), "chatHistory.json");
                var json = JsonConvert.SerializeObject(chatHistory, Formatting.Indented);
                File.WriteAllText(localFilePath, json);
                await UploadFileToFirebase(localFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save or upload chat history: {ex.Message}");
            }
        }

        public class ChatMessage
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public async Task<string> GetFirebaseAuthToken(string email, string password)
        {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyC8ufMTi4s2D9g17q5jX0C4PIi-ahelFGQ"));
            var auth = await authProvider.SignInWithEmailAndPasswordAsync(email, password);
            return await auth.GetFreshAuthAsync().ContinueWith(task => task.Result.FirebaseToken);
        }

        private async Task UploadFileToFirebase(string filePath)
        {
            try
            {
                var storage = new FirebaseStorage(
                    "fir-chathistory.appspot.com",
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = async () => await GetFirebaseAuthToken("nextintern.corp@gmail.com", "swdnextinterniumaycauratnhiu:D"),
                        ThrowOnCancel = true
                    });

                using (var fileStream = File.OpenRead(filePath))
                {
                    await storage
                        .Child("chatHistory")
                        .Child("chatHistory.json")
                        .PutAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
            }
        }
    }
}
