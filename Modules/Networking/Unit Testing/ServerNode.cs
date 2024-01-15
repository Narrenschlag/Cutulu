namespace Cutulu.UnitTest.Network
{
    public partial class ServerNode : Destination
    {
        public override void Receive(byte key, byte[] bytes, Method method, params object[] values)
        {
            ServerConnection<Destination> client = values[0] as ServerConnection<Destination>;
            $"{client.UUID}-{method}:  [{key}]".Log();
        }
    }
}