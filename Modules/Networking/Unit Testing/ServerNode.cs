namespace Cutulu.UnitTest.Network
{
    public partial class ServerNode : Receiver
    {
        public override void Receive(byte key, byte[] bytes, Method method, params object[] values)
        {
            ServerConnection<Receiver> client = values[0] as ServerConnection<Receiver>;
            $"{client.UUID}-{method}:  [{key}]".Log();
        }
    }
}