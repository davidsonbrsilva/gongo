using System;

namespace Gongo.Library
{
    /// <summary>
    /// Classe de resposta do servidor.
    /// </summary>
    public class Response
    {
        public Request Request { get; set; }
        public string ReturnMessage { get; set; }
        public string ActionMessage { get; set; }
        public Status Status { get; set; }
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Cria uma nova instância da classe Response.
        /// </summary>
        public Response()
        {
            Request = null;
            ReturnMessage = null;
            ActionMessage = null;
            Status = Status.Ok;
            DateTime = DateTime.Now;
        }

        /// <summary>
        /// Cria uma nova instância da classe Response.
        /// </summary>
        /// <param name="request">A requisição recebida pelo servidor.</param>
        /// <param name="message">A mensagem de resposta do servidor conforme a ação solicitada na requisição.</param>
        /// <param name="dateTime">A data e hora de envio da resposta.</param>
        public Response(Request request = null, string returnMessage = null, string actionMessage = null, Status status = Status.Ok)
        {
            Request = request;
            ReturnMessage = returnMessage;
            ActionMessage = actionMessage;
            Status = status;
            DateTime = DateTime.Now;
        }

        public void Clear()
        {
            Request = null;
            ReturnMessage = null;
            ActionMessage = null;
            Status = Status.Ok;
            DateTime = DateTime.Now;
        }
    }
}
