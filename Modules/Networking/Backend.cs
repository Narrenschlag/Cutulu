using System;

namespace Cutulu
{
    /// <summary> 
    /// Define your methods here. Tcp and Udp are already contained
    /// </summary>
    public enum Method : byte
    {
        Tcp, Udp
    }

    /// <summary> 
    /// Structure of Network Package
    /// </summary>
    public struct NetworkPackage
    {
        public byte[] Content { get; private set; }
        public Method Method { get; private set; }
        public short Key { get; private set; }

        public static NetworkPackage Create<T>(short key, T value, Method method) => new(key, value.Buffer(), method);
        public NetworkPackage(short key, byte[] content, Method method)
        {
            Content = content ?? Array.Empty<byte>();
            Method = method;
            Key = key;
        }

        public readonly bool TryBuffer<T>(out T value) => Content.TryBuffer(out value);
        public readonly int ByteSize() => Content.Length;
    }
}