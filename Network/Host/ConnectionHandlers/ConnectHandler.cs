namespace Cutulu.Network;

using System.Threading.Tasks;
using Cutulu.Network.Sockets;
using System.Net;
using Core;

public class ConnectHandler(byte key) : ConnectionHandler(key)
{
    public override async Task<(bool Status, object Data)> Validate(HostManager.Wrapper wrapper, TcpSocket socket)
    {
        var packet = await socket.Receive(4);
        Debug.Log($"Received connection type [{packet.Success}].");

        if (packet.Success == false) return (false, null);

        await socket.SendAsync(true.Encode());

        var connection = new Connection(
            wrapper.NextUID(),
            wrapper.Manager,
            socket,
            new(((IPEndPoint)socket.Socket.RemoteEndPoint).Address, packet.Buffer.Decode<int>())
        );

        // Check if the client is still connected
        try
        {
            await socket.SendAsync(connection.UserId.Encode());
        }
        catch
        {
            Debug.LogError($"Socket closed session remotely. Aboarting onboarding process.");
            return (false, null);
        }

        // Remove already connected connections with same address
        if (wrapper.Manager.ConnectionsByUdp.TryGetValue(connection.EndPoint, out var existingConnection))
            wrapper.InvokeDisconnect(existingConnection.Socket);

        // Stop connection if max client limit has been reached
        if (wrapper.Manager.MaxClients > 0 && wrapper.Manager.Connections.Count >= wrapper.Manager.MaxClients)
        {
            Debug.LogError($"Maximum client capacity has been reached. Cancelling connection.");
            return (false, null);
        }

        wrapper.Manager.ConnectionsByUdp[connection.EndPoint] = connection;
        wrapper.Manager.Connections[socket] = connection;

        await socket.ClearBuffer();
        return (true, connection);
    }

    public override async Task Handle(HostManager.Wrapper wrapper, TcpSocket socket, object data)
    {
        if (data.IsNull() || data is not Connection connection) return;
        wrapper.InvokeConnect(connection);

        while (active())
        {
            var packet = await connection.Socket.Receive(4);
            if (packet.Success == false) continue;
            if (active() == false) continue;

            packet = await connection.Socket.Receive(packet.Buffer.Decode<int>());
            if (active() == false) continue;

            if (packet.Success) connection.ReceiveBuffer(packet.Buffer);
        }

        bool active() => connection != null && connection.Socket != null && connection.Socket.IsConnected;

        Debug.LogR($"[color=indianred][i]Connection has been closed remotely. [color=darkgray](Connection:{connection.NotNull()}, Socket:{connection.Socket.NotNull()} [{connection?.Socket?.IsConnected.ToString() ?? "<null>"}])");

        // Close connection
        if (connection.Kick() == false) socket?.Close();
    }
}