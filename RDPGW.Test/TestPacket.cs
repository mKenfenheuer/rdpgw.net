using Newtonsoft.Json;

namespace RDPGW.Test;

public class TestPacket
{
    [JsonProperty("packet")]
    public string TypeName { get; set; } = string.Empty;
    [JsonProperty("data")]
    public string DataHex { get; set; } = string.Empty;
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
    [JsonProperty("expect")]
    public string Expected { get; set; } = string.Empty;

    [JsonIgnore]
    public byte[] Data => Convert.FromHexString(DataHex.Replace("0x", ""));
}
