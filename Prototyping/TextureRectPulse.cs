#if GODOT4_0_OR_GREATER
namespace Cutulu.Prototyping;

using Cutulu.Core;
using Godot;

public partial class TextureRectPulse : TextureRect
{
    [Export] private float PulseSpeed = 0.5f;
    [Export] private float PulseMin = 0.0f;
    [Export] private float PulseMax = 1.0f;

    private float PingPong { get; set; }

    public override void _Ready()
    {
        PingPong = Random.Value;
    }

    public override void _Process(double delta)
    {
        PingPong += (float)delta * PulseSpeed;

        SelfModulate = new Color(
            SelfModulate.R,
            SelfModulate.G,
            SelfModulate.B,
            Mathf.Lerp(PulseMin, PulseMax, Mathf.PingPong(PingPong, 1.0f))
        );
    }
}
#endif