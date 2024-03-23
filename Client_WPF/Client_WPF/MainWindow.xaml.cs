using System.Windows;
using System.Windows.Input;
using System.IO;
using ChatClient;


namespace Client_WPF
{
    public partial class MainWindow : Window
    {
        private static bool isConnected { get; set; }
        private static string? Tb_MsgDefaultText { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            isConnected = false;
            Tb_MsgDefaultText = "Enter your message...";
            message_Tb.Text = Tb_MsgDefaultText;
        }

        private async void con_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                switch (name_Tb.Text)
                {
                    case "Username":
                        usernameRand();
                        break;
                    default:
                        break;
                }
                Client.Connect(name_Tb.Text);
                isConnected = true;
                con_Button.Content = "Disconnect";
                message_Tb.IsEnabled = true;
                name_Tb.IsEnabled = false;
                await addToChat();
            }
            else
            {
                Client.Disconnect();
                isConnected = false;
                con_Button.Content = "Connect";
                message_Tb.IsEnabled = false;
                name_Tb.IsEnabled = true;
            }
        }

        private async Task addToChat()
        {
            while (isConnected)
            {
                string toChatBox_str = await Client.MessageReceiverHandler();
                chatBox_Lb.Items.Add(toChatBox_str);
                chatBox_Lb.ScrollIntoView(chatBox_Lb.Items[chatBox_Lb.Items.Count - 1]);
            }
        }

        private void message_Tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Client.MessageSenderHandle(message_Tb.Text);
                message_Tb.Text = string.Empty;
            }
        }

        private void save_Button_Click(object sender, RoutedEventArgs e)
        {
            DateTime currentDateTime = DateTime.Now;
            string formattedDateTime = currentDateTime.ToString("dd MMMM yyyy HH.mm.ss");
            string path = formattedDateTime + ".txt";
            string newstr = string.Empty;
            foreach (var item in chatBox_Lb.Items)
            {
                newstr += item.ToString() + "\n";
            }

            File.AppendAllText(path, newstr);
        }

        private void usernameRand()
        {
            Random rnd = new Random();
            name_Tb.Text = "Username" + rnd.Next(0, 100000);
        }

        private void message_Tb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (message_Tb.IsEnabled)
            {
                case true:
                    if (message_Tb.Text == Tb_MsgDefaultText)
                    {
                        message_Tb.Text = string.Empty;
                    }
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            switch(isConnected)
            {
                case true:
                    Client.Disconnect();
                    break;
                default:
                    break;
            }
        }
    }
}