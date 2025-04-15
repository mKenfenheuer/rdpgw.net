namespace RDPGW.Protocol;

/// <summary>
/// Flags controlling redirection settings for an HTTP tunnel.
/// </summary>
public enum HTTP_TUNNEL_REDIR_FLAGS : uint
{
    /// <summary>
    /// Enables all redirection types.
    /// </summary>
    HTTP_TUNNEL_REDIR_ENABLE_ALL = 0x80000000,

    /// <summary>
    /// Disables all redirection types.
    /// </summary>
    HTTP_TUNNEL_REDIR_DISABLE_ALL = 0x40000000,

    /// <summary>
    /// Disables drive redirection.
    /// </summary>
    HTTP_TUNNEL_REDIR_DISABLE_DRIVE = 0x01,

    /// <summary>
    /// Disables printer redirection.
    /// </summary>
    HTTP_TUNNEL_REDIR_DISABLE_PRINTER = 0x02,

    /// <summary>
    /// Disables port redirection.
    /// </summary>
    HTTP_TUNNEL_REDIR_DISABLE_PORT = 0x04,

    /// <summary>
    /// Disables clipboard redirection.
    /// </summary>
    HTTP_TUNNEL_REDIR_DISABLE_CLIPBOARD = 0x08,

    /// <summary>
    /// Disables Plug and Play device redirection.
    /// </summary>
    HTTP_TUNNEL_REDIR_DISABLE_PNP = 0x10,
}
