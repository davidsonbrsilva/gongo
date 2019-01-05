namespace Gongo.Library
{
    /// <summary>
    /// Um enumerador para os tipos de resposta do servidor.
    /// </summary>
    public enum Status
    {
        Ok,
        ExistingUsername,
        InvalidUsername,
        ReservedUsername,
        NotSpecifiedRequest
    }
}
