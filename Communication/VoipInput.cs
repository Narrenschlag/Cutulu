#if GODOT4_0_OR_GREATER
namespace Cutulu.Communication;

using Cutulu.Core;
using Godot;

/// <summary>
/// Documentation: Create two audio buses: MuteBus>>Master, Record>>MuteBus.
/// Then add capture effect to Record bus.
/// </summary>

[GlobalClass]
public partial class VoipInput : AudioStreamPlayer
{
    [Export] private bool LogConsole = false;
    [Export] private VoipOutput DebugOutput;
    [Export] public float InputThreshold = 0.005f;

    private AudioEffectCapture Capture;
    private int Index;

    public Notification<MicData> AudioBroadcastEvent;

    public override void _Ready()
    {
        Stream = new AudioStreamMicrophone();
        Play();

        Index = AudioServer.GetBusIndex("Record");
        Capture = AudioServer.GetBusEffect(Index, 0) as AudioEffectCapture;
    }

    public override void _Process(double delta)
    {
        if (Capture.IsNull()) return;

        Vector2[] stereoData = Capture.GetBuffer(Capture.GetFramesAvailable());

        if (stereoData.Length < 1) return;

        short[] data = new short[stereoData.Length];
        float maxAmplitude = 0.0f;
        float value;

        for (int i = 0; i < stereoData.Length; i++)
        {
            value = (stereoData[i].X + stereoData[i].Y) * 0.5f; // Average the left and right channels
            maxAmplitude = Mathf.Max(maxAmplitude, value);
            data[i] = (short)(value * 10000.0f);
        }

        if (maxAmplitude < InputThreshold) return;

        if (AudioBroadcastEvent?.HasAnyListeners() ?? false)
            AudioBroadcastEvent.Invoke(new MicData { MaxAmplitude = maxAmplitude, Data = data });

        if (DebugOutput.NotNull()) DebugOutput.AppendBuffer(new MicData { MaxAmplitude = maxAmplitude, Data = data });
        if (LogConsole) Debug.Log($"Added {data.Length * 2 + 4} bytes to the buffer.");
    }
}
#endif