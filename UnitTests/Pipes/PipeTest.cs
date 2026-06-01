#if GODOT4_0_OR_GREATER
namespace Cutulu.Core.UnitTest;

using System.Threading.Tasks;
using Network;
using Godot;

public partial class PipeTest : Node
{
    [Export] private string PipeName = "TestPipe";

    private const string TestMessage = "Hello World!";

    public override void _Ready()
    {
        _ = StartHostPipe();
        _ = StartClientPipe();
    }

    private async Task StartHostPipe()
    {
        var socket = await NamedPipe.StartHost(PipeName, _ReceivePipe, default);

        socket.Send(1, TestMessage);
    }

    private async Task StartClientPipe()
    {
        // Pipe to matchmaking
        if (PipeName.NotEmpty())
        {
            NamedPipe.Socket socket = await NamedPipe.Connect(PipeName, _ReceivePipe);

            if (socket?.IsConnected != true)
            {
                Crash("ClientPipe could not connect after retries");
                return;
            }
        }
    }

    private void _ReceivePipe(NamedPipe.Socket socket, UNumber32 key, LocalDecoder decoder)
    {
        switch ((uint)key)
        {
            case 1 when decoder.TryDecode(out string str) && str == TestMessage:
                Debug.Log($"Client received message '{str}'. Sending back message. (1/2)");
                socket.Send(2, TestMessage);
                return;

            case 2 when decoder.TryDecode(out string str) && str == TestMessage:
                Debug.Log($"Host received message '{str}'. Invoking success. (2/2)");
                Success();
                return;

            default:
                Crash($"Received unknown message key {key} or wrong message.");
                return;
        }
    }

    private void Crash(string message)
    {
        Debug.LogError($"[color=red]NamedPipe Test [b]failed[/b].[/color] {message}");
        Application.Quit();
    }

    private void Success()
    {
        Debug.LogR($"[color=green]NamedPipe Test [b]successful[/b].[/color]");
        Application.Quit();
    }
}
#endif