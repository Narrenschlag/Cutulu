namespace Cutulu.UnitTest.Network
{
    public partial class ServerNode : Receiver
    {
        public override void Receive(ref NetworkPackage package, params object[] values)
        {
            ServerConnection<Receiver> client = values[0] as ServerConnection<Receiver>;
            var text = "";

            if (package.Content.TryBuffer(out text)) { }
            else if (package.Content.TryBuffer(out short s)) text = s.ToString();

            $"{client.UUID}-{package.Method}: {(text ?? "<null>")} [k:{package.Key}] {package.Content.Length}b".Log();
        }
    }
}