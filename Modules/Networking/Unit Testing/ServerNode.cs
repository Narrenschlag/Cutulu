namespace Cutulu.UnitTest.Network
{
    public partial class ServerNode : Receiver
    {
        public override void Receive(ref NetworkPackage package, params object[] values)
        {
            ServerConnection<Receiver> client = values[0] as ServerConnection<Receiver>;
            if (package.TryBuffer(out string text)) { }

            $"{client.UUID}-{package.Method}: [k:{package.Key}, {package.Content.Length}b] {(text.NotEmpty() ? text : "")}".Log();
        }
    }
}