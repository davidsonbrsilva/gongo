using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Web.Script.Serialization;
using Gongo.Library;
using System.Net;

namespace Gongo.Client
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class GongoWindow : Window
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread listenerThread;
        private string username;
        private bool autoScroll;

        public TcpClient Client { get { return client; } }

        public GongoWindow()
        {
            autoScroll = true;
            InitializeComponent();
        }

        #region Methods
        private void Connect(string host, int port)
        {
            client = new TcpClient(host, port);
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream());
            username = "@" + client.Client.LocalEndPoint.ToString();
        }

        private void Disconnect()
        {
            if (listenerThread != null)
            {
                if (listenerThread.ThreadState == ThreadState.Running)
                {
                    listenerThread.Abort();
                }
            }

            reader?.Close();
            writer?.Close();
            client?.Close();
        }

        private void ChangeUsername()
        {
            if (writer != null)
            {
                if (MessageTextBlock.Text != "")
                {
                    string newUsername = MessageTextBlock.Text;
                    newUsername = newUsername.ToLower();

                    if (!newUsername.StartsWith("@"))
                    {
                        newUsername = "@" + newUsername;
                    }

                    Request request = new Request(username, "@gongoserver", null, newUsername, "@all", Library.Action.AlterUsername);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string serialized = serializer.Serialize(request);

                    SendMessageToServer(serialized);
                }
            }
            else
            {
                Notify("Houve um problema de conexão com o servidor.");
                Disconnect();
            }
        }

        private void SendMessage()
        {
            if (writer != null)
            {
                if (MessageTextBlock.Text != "")
                {
                    Request request = new Request(username, "@gongoserver", MessageTextBlock.Text, null, "@all", Library.Action.SendMessage);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string serialized = serializer.Serialize(request);

                    SendMessageToServer(serialized);
                    MessageTextBlock.Clear();
                }
            }
            else
            {
                Notify("Não foi possível enviar a sua mensagem '" + MessageTextBlock.Text + "'");
                Disconnect();
            }
        }

        /// <summary>
        /// Envia uma mensagem para o servidor.
        /// </summary>
        /// <param name="message"></param>
        private void SendMessageToServer(string message)
        {
            if (writer != null)
            {
                writer.WriteLine(message);
                writer.Flush();
            }
        }

        /// <summary>
        /// Escreve a mensagem enviada no content.
        /// </summary>
        /// <param name="message"></param>
        private delegate void WriteSendedMessageDelegate(string message);
        private void WriteSendedMessage(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new WriteSendedMessageDelegate(WriteSendedMessage), message);
                return;
            }
            else
            {
                string time = string.Format("{0}: ", DateTime.Now.ToString("HH:mm"));

                Paragraph paragraph = new Paragraph();

                Run senderRun = new Run(time);
                Run messageRun = new Run(message);

                senderRun.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC6, 0x9A, 0xCC));

                paragraph.Inlines.Add(senderRun);
                paragraph.Inlines.Add(messageRun);

                MessageContent.Document.Blocks.Add(paragraph);
            }
        }

        /// <summary>
        /// Escreve a mensagem recebida no content.
        /// </summary>
        /// <param name="sender">O emissor.</param>
        /// <param name="message">A mensagem.</param>
        private delegate void WriteReceivedMessageDelegate(string sender, string message);
        private void WriteReceivedMessage(string sender, string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new WriteReceivedMessageDelegate(WriteReceivedMessage), sender, message);
                return;
            }
            else
            {
                string dateAndSender = string.Format("{0}{1}: ", DateTime.Now.ToString("HH:mm"), " " + sender);

                Paragraph paragraph = new Paragraph();

                Run senderRun = new Run(dateAndSender);
                Run messageRun = new Run(message);

                senderRun.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x9B, 0xBC, 0x80));

                paragraph.Inlines.Add(senderRun);
                paragraph.Inlines.Add(messageRun);

                MessageContent.Document.Blocks.Add(paragraph);
            }
        }

        /// <summary>
        /// Notifica uma mensagem do sistema.
        /// </summary>
        /// <param name="message"></param>
        private delegate void NotifyDelegate(string message);
        private void Notify(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new NotifyDelegate(Notify), message);
                return;
            }
            else
            {
                Paragraph paragraph = new Paragraph(new Run(message))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(10, 5, 10, 5),
                    Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x47, 0x21, 0x49)),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xC6, 0x9A, 0xCC)),
                };

                MessageContent.Document.Blocks.Add(paragraph);
            }
        }

        private void ChangeDefaultButtonToSend()
        {
            UsernameTextBox.Text = username;
            SendButton.Content = "Enviar";
            CancelButtonBorder.Visibility = Visibility.Collapsed;
            MessageTextBlock.Text = "";
            MessageTextBlock.MaxLength = int.MaxValue;
            MessageLabel.Content = "Digite sua mensagem...";
        }

        private void ChangeDefaultButtonToConfirm()
        {
            SendButton.Content = "Confirmar";
            CancelButtonBorder.Visibility = Visibility.Visible;
            MessageTextBlock.Text = "";
            MessageTextBlock.MaxLength = 14;
            MessageLabel.Content = "Novo nome...";
        }

        private void Listen(object cli)
        {
            GongoWindow gongoWindow = cli as GongoWindow;
            TcpClient client = gongoWindow.Client;
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            using (StreamReader reader = new StreamReader(client.GetStream()))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    Response response = serializer.Deserialize<Response>(line);
                    Process(response);
                }
            }
        }

        private void Process(Response response)
        {
            // Se a requisição partiu de um cliente.
            if (response.Request != null)
            {
                if (response.Request.Action == Library.Action.Login)
                {
                    if (response.Status == Status.Ok)
                    {
                        if (response.Request.From.Equals(username))
                        {
                            Notify(response.ReturnMessage);
                        }
                        else
                        {
                            Notify(response.ActionMessage);
                        }
                    }
                }
                else if (response.Request.Action == Library.Action.SendMessage)
                {
                    if (response.Status == Status.Ok)
                    {
                        if (response.Request.From == username)
                        {
                            WriteSendedMessage(response.Request.Message);
                        }
                        else
                        {
                            WriteReceivedMessage(response.Request.From, response.Request.Message);
                        }
                    }
                }
                else if (response.Request.Action == Library.Action.AlterUsername)
                {
                    if (response.Status == Status.Ok)
                    {
                        if (response.Request.From == username)
                        {
                            username = response.Request.NewUsername;
                            Notify(response.ReturnMessage);

                            // Atualiza a UI.
                            Dispatcher.BeginInvoke
                            (
                                System.Windows.Threading.DispatcherPriority.Normal, (System.Action)
                                (
                                    () => ChangeDefaultButtonToSend()
                                )
                            );
                        }
                        else
                        {
                            Notify(response.ActionMessage);
                        }
                    }
                    else
                    {
                        Notify(response.ReturnMessage);
                    }
                }
            }
            // Se a requisição partiu diretamente do servidor.
            else
            {
                Notify(response.ActionMessage);
            }
        }

        private string GetLocalIPAddress() =>
            Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
        #endregion

        #region Events
        private void MessageTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MessageTextBlock.Text.Count() != 0)
            {
                if (!(MessageTextBlock.Background is SolidColorBrush))
                {
                    MessageTextBlock.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                }
            }
            else if (MessageTextBlock.Text.Count() == 0)
            {
                if (MessageTextBlock.Background is SolidColorBrush)
                {
                    MessageTextBlock.Background = null;
                }
            }
        }

        private void AlterUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeDefaultButtonToConfirm();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeDefaultButtonToSend();
        }

        private void MessageTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((string)SendButton.Content == "Enviar")
                {
                    SendMessage();
                }
                else if ((string)SendButton.Content == "Confirmar")
                {
                    ChangeUsername();
                }
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)SendButton.Content == "Enviar")
            {
                SendMessage();
            }
            else if ((string)SendButton.Content == "Confirmar")
            {
                ChangeUsername();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string settingsFile = Directory.GetCurrentDirectory() + "/ClientSettings.json";

            try
            {
                var settings = new Settings() { Host = GetLocalIPAddress(), Port = 22777 };

                if (File.Exists(settingsFile))
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string text = File.ReadAllText(settingsFile);
                    var json = serializer.Deserialize<Settings>(text);

                    if (json != null)
                    {
                        settings.Host = json.Host ?? settings.Host;
                        settings.Port = json.Port ?? settings.Port;
                    }
                }

                if (settings.Host == null)
                    throw new Exception("Parece que você não está conectado à internet.");

                Connect(settings.Host, settings.Port.Value);

                listenerThread = new Thread(Listen)
                {
                    Name = "ListenerThread",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                };

                listenerThread.Start(this);

                UsernameTextBox.Text = username;
            }
            catch (FileNotFoundException ex)
            {
                Notify(ex.Message);
            }
            catch (SocketException ex)
            {
                Notify(ex.Message);
            }
            catch (Exception ex)
            {
                Notify(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
        }
        #endregion

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                if (ScrollViewer.VerticalOffset == ScrollViewer.ScrollableHeight)
                {
                    autoScroll = true;
                }
                else
                {
                    autoScroll = false;
                }
            }

            if (autoScroll && e.ExtentHeightChange != 0)
            {
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
            }
        }
    }
}
