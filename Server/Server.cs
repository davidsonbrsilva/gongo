using Gongo.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Gongo.Server
{
    /// <summary>
    /// Classe de servidor do Gongo Server.
    /// </summary>
    public sealed class Server
    {
        private static readonly Server instance = new Server();
        private bool isRunning;
        private JavaScriptSerializer serializer;
        private Dictionary<TcpClient, string> connections;
        private Dictionary<TcpClient, Thread> channels;
        private TcpListener listener;
        private Thread listenerThread;

        public static Server Instance { get { return instance; } }

        private Server()
        {
            isRunning = false;
        }

        #region External
        /// <summary>
        /// Escuta novas solicitações de conexão.
        /// </summary>
        private void ListenConnections()
        {
            listener.Start();

            while (isRunning)
            {
                Notify("Aguardando novas conexões...");
                TcpClient client = listener.AcceptTcpClient();
                Notify("Conexão estabelecida.");

                // O nome de usuário é atribuido como o host e a porta para a comunicação aceita.
                string username = "@" + client.Client.RemoteEndPoint.ToString();

                // Mapeia o cliente.
                connections.Add(client, username);

                // Cria e inicia uma nova thread para escutar a comunicação.
                Thread listenerThread = new Thread(ListenMessages);
                listenerThread.Start(client);

                channels.Add(client, listenerThread);
            }

            listener.Stop();
            Stop();
        }

        /// <summary>
        /// Exibe uma mensagem no console do servidor.
        /// </summary>
        private void Notify(string message)
        {
            Console.WriteLine("[{0}]: {1}", DateTime.Now.ToString("hh:mm:ss"), message);
        }

        /// <summary>
        /// Interrompe o servidor caso esteja em funcionamento e prepara nova comunicação para o host e port especificados.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void Fresh(string host, int port)
        {
            if (isRunning)
            {
                Stop();
            }

            serializer = new JavaScriptSerializer();
            connections = new Dictionary<TcpClient, string>();
            channels = new Dictionary<TcpClient, Thread>();
            listener = DefineTcpListener(host, port);
        }

        /// <summary>
        /// Inicia o servidor.
        /// </summary>
        public void Start()
        {
            Notify("Iniciando servidor...");
            listenerThread = new Thread(ListenConnections);
            listenerThread.Start();
            isRunning = true;
            Notify("Servidor iniciado.");
        }

        /// <summary>
        /// Interrompe o servidor.
        /// </summary>
        public void Stop()
        {
            Notify("Desligando servidor...");

            Notify("\tInterrompendo a escuta por novas conexões...");
            if (listenerThread.ThreadState == ThreadState.Running)
            {
                listenerThread.Abort();
            }
            Notify("Concluído.");

            Notify("\tInterrompendo canais...");
            foreach (KeyValuePair<TcpClient, Thread> keyValuePair in channels)
            {
                if (keyValuePair.Value.ThreadState == ThreadState.Running)
                {
                    keyValuePair.Value.Abort();
                }

                if (keyValuePair.Key.Connected)
                {
                    keyValuePair.Key.Close();
                }
            }

            connections.Clear();
            channels.Clear();
            Notify("Até logo!");

            isRunning = false;
        }
        #endregion

        #region Communication
        /// <summary>
        /// Escuta mensagens do cliente especificado.
        /// </summary>
        /// <param name="client">O cliente (TcpClient esperado).</param>
        private void ListenMessages(object client)
        {
            TcpClient cli = client as TcpClient;
            Request request;
            Response response;
            string receivedData;
            string dataToSend;

            // Notifica todos os usuários sobre a entrada do cliente na conversa.
            response = new Response(request: new Request(connections[cli], "@gongoserver", null, null, null, Library.Action.Login), returnMessage: "Você agora participa da conversa.", actionMessage: connections[cli] + " entrou na conversa.");
            dataToSend = serializer.Serialize(response);
            SendToAll(dataToSend);

            // Limpa o objeto de resposta para reutilização.
            response.Clear();

            // Permanece no laço enquanto a conexão com o cliente existir.
            while (cli.Connected && (receivedData = ReceiveFrom(cli)) != null)
            {
                request = serializer.Deserialize<Request>(receivedData);

                response = Process(request, cli);
                dataToSend = serializer.Serialize(response);

                // Se há algum tipo de resposta a ser dada para os destinatários do emissor da mensagem.
                if (response.ActionMessage != null)
                {
                    SendToAllExcept(cli, dataToSend);
                }

                // Envia a resposta do servidor para o cliente da requisição.
                SendTo(cli, dataToSend);

                // Limpa os objetos de requisição e resposta para reutilização.
                request.Clear();
                response.Clear();
            }

            // Notifica todos os usuários sobre a saída do cliente.
            response.ActionMessage = connections[cli] + " saiu.";
            response.Status = Status.Ok;
            dataToSend = serializer.Serialize(response);
            SendToAllExcept(cli, dataToSend);

            connections.Remove(cli);
            channels.Remove(cli);
            cli.Close();
        }

        /// <summary>
        /// Aguarda nova mensagem do cliente especificado.
        /// </summary>
        /// <param name="client">O cliente.</param>
        /// <returns></returns>
        private string ReceiveFrom(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            return reader.ReadLine();
        }

        /// <summary>
        /// Envia mensagem para o cliente especificado.
        /// </summary>
        /// <param name="client">O cliente.</param>
        /// <param name="data">A mensagem a ser enviada.</param>
        private void SendTo(TcpClient client, string data)
        {
            StreamWriter writer = new StreamWriter(client.GetStream());
            writer.WriteLine(data);
            writer.Flush();
        }

        /// <summary>
        /// Envia mensagem para todos os clientes conectados, exceto para o cliente especificado.
        /// </summary>
        /// <param name="client">O cliente que não receberá a mensagem.</param>
        /// <param name="data">A mensagem a ser enviada.</param>
        private void SendToAllExcept(TcpClient client, string data)
        {
            foreach (TcpClient activeClient in connections.Keys)
            {
                if (activeClient != client)
                {
                    SendTo(activeClient, data);
                }
            }
        }

        /// <summary>
        /// Envia mensagem para todos os clientes conectados.
        /// </summary>
        /// <param name="data">A mensagem a ser enviada.</param>
        private void SendToAll(string data)
        {
            foreach (TcpClient activeClient in connections.Keys)
            {
                SendTo(activeClient, data);
            }
        }
        #endregion

        #region Processing
        /// <summary>
        /// Processa uma requisição e retorna um objeto de resposta.
        /// </summary>
        /// <param name="request">A requisição.</param>
        /// <returns></returns>
        private Response Process(Request request, TcpClient client)
        {
            Response response = new Response(request);

            if (request.Action == Library.Action.AlterUsername)
            {
                // Adiciona o símbolo identificador @ no começo do nome de usuário caso o usuário não o tenha especificado.
                if (!request.NewUsername.StartsWith("@"))
                {
                    request.NewUsername = "@" + request.NewUsername;
                }

                // Verifica se o nome de usuário informado na requisição é válido.
                if (!UsernameIsValid(request.NewUsername))
                {
                    response.ReturnMessage = "Esse nome de usuário é inválido.\n Dica: nomes de usuário podem conter letras, números e pontos (.).";
                    response.Status = Status.InvalidUsername;
                    return response;
                }

                // Verifica se o nome de usuário já está sendo usado.
                if (UsernameAlreadyExists(request.NewUsername))
                {
                    response.ReturnMessage = "Esse nome de usuário já está sendo usado por outra pessoa. Por favor, tente outro.";
                    response.Status = Status.ExistingUsername;
                    return response;
                }

                // Verfica se o nome de usuário é reservado pelo sistema.
                if (UsernameIsReservedBySystem(request.NewUsername))
                {
                    response.ReturnMessage = "Esse nome é reservado pelo sistema. Por favor, tente outro.";
                    response.Status = Status.ReservedUsername;
                    return response;
                }

                connections[client] = request.NewUsername;
                response.ReturnMessage = "Seu nome agora é " + request.NewUsername + ".";
                response.ActionMessage = request.From + " alterou o nome de usuário para " + request.NewUsername + ".";
                response.Status = Status.Ok;
                return response;
            }
            else if (request.Action == Library.Action.SendMessage)
            {
                response.ReturnMessage = "Sua mensagem foi enviada.";
                response.ActionMessage = request.Message;
                response.Status = Status.Ok;
                return response;
            }
            else
            {
                response.ReturnMessage = "A mensagem foi recebida pelo servidor, mas nenhuma ação foi especificada na requisição.";
                response.Status = Status.NotSpecifiedRequest;
                return response;
            }
        }

        /// <summary>
        /// Verifica se o nome de usuário especificado por parâmetro é válido.
        /// </summary>
        /// <param name="username">O nome de usuário.</param>
        /// <returns></returns>
        private bool UsernameIsValid(string username)
        {
            return Regex.IsMatch(username, @"^@\w+(\.\w+)*$");
        }

        /// <summary>
        /// Verifica se o nome de usuário especificado já está sendo utilizado.
        /// </summary>
        /// <param name="username">O nome de usuário.</param>
        /// <returns></returns>
        private bool UsernameAlreadyExists(string username)
        {
            if (connections.ContainsValue(username))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica se o nome de usuário especificado é reservado pelo sistema.
        /// </summary>
        /// <param name="username">O nome de usuário.</param>
        /// <returns></returns>
        private bool UsernameIsReservedBySystem(string username)
        {
            string[] reservedNames = new string[] { "@all", "@admin" };

            foreach (string name in reservedNames)
            {
                if (name.Equals(username))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Auxiliar Methods
        private TcpListener DefineTcpListener(string host, int port)
        {
            IPAddress address;

            if (host == "any" || host == "" || host == null)
            {
                address = IPAddress.Any;
                Notify("O servidor recebe conexões de qualquer endereço.");
            }
            else
            {
                address = IPAddress.Parse(host);
                Notify("O servidor recebe conexões de " + address.ToString());
            }

            return new TcpListener(address, port);
        }
        #endregion
    }
}
