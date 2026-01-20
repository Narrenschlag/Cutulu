#if GODOT4_0_OR_GREATER
namespace Cutulu.Communication;

using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class VoipOutput : AudioStreamPlayer
{
    [Export] private bool LogConsole = false;
    [Export] public int MixRate = 48000;
    [Export] public float BufferLength = 0.2f;

    public AudioStreamGeneratorPlayback Playback;
    private readonly List<short> ReceiveBuffer = [];

    public override void _Ready()
    {
        PlaybackType = AudioServer.PlaybackType.Stream;
        Stream = new AudioStreamGenerator()
        {
            BufferLength = BufferLength,
            MixRate = MixRate,
        };

        Play();
        Playback = GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public override void _Process(double delta)
    {
        if (ReceiveBuffer.Count < 1 || Playback == null) return;

        int length = Mathf.Min(Playback.GetFramesAvailable(), ReceiveBuffer.Count);
        float value;

        if (LogConsole) Cutulu.Core.Debug.Log($"Processing {length} frames.");

        for (int i = 0; i < length; i++)
        {
            value = ReceiveBuffer[i] / 10000.0f;

            Playback.PushFrame(new Vector2(value, value));
        }

        ReceiveBuffer.RemoveRange(0, length);
    }

    public void AppendBuffer(MicData data)
    {
        ReceiveBuffer.AddRange(data.Data);

        if (LogConsole) Cutulu.Core.Debug.Log($">>Added {data.Data.Length * 2 + 4} bytes to the buffer.");
    }
}
#endif