namespace ServiceWire
{
    internal sealed class ParameterTypes
    {
        internal const byte Unknown     = 0x00;
        internal const byte Bool        = 0x01;
        internal const byte Byte        = 0x02;
        internal const byte SByte       = 0x03;
        internal const byte Char        = 0x04;
        internal const byte Decimal     = 0x05;
        internal const byte Double      = 0x06;
        internal const byte Float       = 0x07;
        internal const byte Int         = 0x08;
        internal const byte UInt        = 0x09;
        internal const byte Long        = 0x0A;
        internal const byte ULong       = 0x0B;
        internal const byte Short       = 0x0C;
        internal const byte UShort      = 0x0D;
        internal const byte String      = 0x0E;
        internal const byte ByteArray   = 0x0F;
        internal const byte CharArray   = 0x10;
        internal const byte Null        = 0x11;
        internal const byte Type        = 0x12;
        internal const byte Guid        = 0x13;
        internal const byte DateTime    = 0x14;
        //v2 frames only: DateTime.ToBinary() as int64 (preserves Kind semantics
        //across machines like the v1 round-trip "o" string, without parsing)
        internal const byte DateTime2   = 0x15;

        internal const byte CompressedByteArray     = 0x20;
        internal const byte CompressedCharArray     = 0x21;
        internal const byte CompressedString        = 0x22;
        internal const byte CompressedUnknown       = 0x23;

        internal const byte ArrayBool        = 0x41;
        internal const byte ArraySByte       = 0x43;
        internal const byte ArrayDecimal     = 0x45;
        internal const byte ArrayDouble      = 0x46;
        internal const byte ArrayFloat       = 0x47;
        internal const byte ArrayInt         = 0x48;
        internal const byte ArrayUInt        = 0x49;
        internal const byte ArrayLong        = 0x4A;
        internal const byte ArrayULong       = 0x4B;
        internal const byte ArrayShort       = 0x4C;
        internal const byte ArrayUShort      = 0x4D;
        internal const byte ArrayString      = 0x4E;
        internal const byte ArrayType        = 0x52;
        internal const byte ArrayGuid        = 0x53;
        internal const byte ArrayDateTime    = 0x54;
        //v2 frames only: int32 length + int64 DateTime.ToBinary() per element
        internal const byte ArrayDateTime2   = 0x55;
    }
}