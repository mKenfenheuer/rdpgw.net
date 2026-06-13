namespace RDPGW.Protocol;

/// <summary>
/// HRESULT-style error codes used by the RDG (MS-TSGU) protocol in handshake,
/// tunnel, tunnel-auth and channel responses. Values are the well-known
/// E_PROXY_* codes used by the Windows RD Gateway.
/// </summary>
public static class HTTP_ERROR_CODE
{
    /// <summary>Operation succeeded (S_OK).</summary>
    public const uint S_OK = 0x00000000;

    /// <summary>An unexpected internal error occurred (E_PROXY_INTERNALERROR).</summary>
    public const uint E_PROXY_INTERNALERROR = 0x800759D8;

    /// <summary>Resource access denied by the Resource Authorization Policy (E_PROXY_RAP_ACCESSDENIED).</summary>
    public const uint E_PROXY_RAP_ACCESSDENIED = 0x800759DA;

    /// <summary>Access denied by the Network Access Protection policy (E_PROXY_NAP_ACCESSDENIED).</summary>
    public const uint E_PROXY_NAP_ACCESSDENIED = 0x800759DB;

    /// <summary>The RDG server could not connect to the requested target server (E_PROXY_TS_CONNECTFAILED).</summary>
    public const uint E_PROXY_TS_CONNECTFAILED = 0x800759DD;

    /// <summary>The capabilities offered by the client are not supported by the server (E_PROXY_CAPABILITYMISMATCH).</summary>
    public const uint E_PROXY_CAPABILITYMISMATCH = 0x800759E9;
}
