namespace Gongo.Library
{
    /// <summary>
    /// Classe de requisição do cliente.
    /// </summary>
    public sealed class Request
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Message { get; set; }
        System.DateTime DateTime { get; set; }
        public string NewUsername { get; set; }
        public string Receiver { get; set; }
        public Action Action { get; set; }

        public Request()
        {
            From = null;
            To = null;
            Message = null;
            DateTime = System.DateTime.Now;
            NewUsername = null;
            Receiver = null;
            Action = Action.None;
        }

        public Request(string from, string to, string message, string newUsername, string receiver, Action action)
        {
            From = from;
            To = to;
            Message = message;
            DateTime = System.DateTime.Now;
            NewUsername = newUsername;
            Receiver = receiver;
            Action = action;
        }

        public void Clear()
        {
            From = null;
            To = null;
            Message = null;
            DateTime = System.DateTime.Now;
            NewUsername = null;
            Receiver = null;
            Action = Action.None;
        }
    }
}
