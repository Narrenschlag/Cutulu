namespace Cutulu.Network;

using System.Threading.Tasks;
using Cutulu.Network.Sockets;
using Core;

public class PingHandler(byte key) : ConnectionHandler(key)
{
    public override async Task<(bool Status, object Data)> Validate(HostManager.Wrapper wrapper, TcpSocket socket)
    {
        if (wrapper.Manager.PingBuffer.NotEmpty())
        {
            var buffer = wrapper.Manager.PingBuffer;

            await socket.SendAsync(buffer.Length.Encode(), buffer);
            Debug.LogError($"Ping response sent to {socket.Socket.RemoteEndPoint}. Closing connection.");
        }

        return (false, null);
    }
}