namespace Cutulu.Network;

using System.Threading.Tasks;
using Cutulu.Network.Sockets;
using Core;

public class PingHandler(byte key) : ConnectionHandler(key)
{
    public override async Task<(bool Status, object Data)> Validate(IConnectionWrapper _wrapper, TcpSocket socket)
    {
        if (_wrapper is IPingWrapper wrapper)
        {
            var buffer = wrapper.GetPingBuffer();

            if (buffer.NotEmpty())
            {
                await socket.SendAsync(buffer.Length.Encode(), buffer);
                Debug.LogError($"Ping response sent to {socket.Socket.RemoteEndPoint}. Closing connection.");
            }
        }

        return (false, null);
    }
}