namespace Cutulu.Network;

using System.Threading.Tasks;
using Cutulu.Network.Sockets;

public abstract class ConnectionHandler(byte key)
{
    public byte Key { get; init; } = key;

    public virtual async Task<(bool Status, object Data)> Validate(HostManager.Wrapper wrapper, TcpSocket socket) => (false, null);

    public virtual async Task Handle(HostManager.Wrapper wrapper, TcpSocket socket, object data) { }
}