namespace Cutulu.UnitTest.Network
{
	public partial class ServerNode : Destination
	{
		public override void __receive(byte key, BufferType type, byte[] bytes, Method method, params object[] values)
		{
			ServerConnection<Destination> client = values[0] as ServerConnection<Destination>;
			$"{client.UUID}> {method}-{key}".Log();
		}
	}
}