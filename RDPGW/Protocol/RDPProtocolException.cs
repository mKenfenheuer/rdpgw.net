[Serializable]
internal class RDPProtocolException : Exception
{
    public RDPProtocolException()
    {
    }

    public RDPProtocolException(string? message) : base(message)
    {
    }

    public RDPProtocolException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
