namespace ServiceWire
{
    /// <summary>
    /// The parameter-block encoding in use. V1 is the classic unframed format every
    /// release understands; V2 (7.0+) is negotiated via ProtocolCapabilities.WireV2
    /// and adds binary DateTime encodings inside framed messages.
    /// </summary>
    internal enum WireVersion
    {
        V1 = 1,
        V2 = 2,
    }
}
