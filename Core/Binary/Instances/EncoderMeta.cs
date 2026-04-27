namespace Cutulu.Core;

public struct EncoderMeta(string paramName, long length)
{
    [Encodable] public string ParamName = paramName;
    [Encodable] public UNumber64 Length = length;
}