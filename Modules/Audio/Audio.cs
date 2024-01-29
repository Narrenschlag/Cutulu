using Cutulu;
using Godot;

public partial class Audio : Node
{
    public static Audio Node;
    public override void _EnterTree()
    {
        Node = this;
    }

    public static AudioStreamPlayer3D Play(string localPath, Vector3 globalPosition, float lifeTime = 0) => Node.IsNull() ? null : localPath.TryLoadResource(out AudioStream stream) ? Play(stream, globalPosition, lifeTime) : null;
    public static AudioStreamPlayer3D Play(AudioStream stream, Vector3 globalPosition, float lifeTime = 0) => Node.IsNull() ? null : Node.PlayLocal(stream, globalPosition, lifeTime);
    public AudioStreamPlayer3D PlayLocal(AudioStream stream, Vector3 globalPosition, float lifeTime = 0)
    {
        AudioStreamPlayer3D player = new();
        AddChild(player);

        player.GlobalPosition = globalPosition;

        if (lifeTime > 0)
            player.Destroy(lifeTime);

        player.Stream = stream;
        player.Play(0);
        return player;
    }

    public static AudioStreamPlayer Play(string localPath, float lifeTime = 0) => Node.IsNull() ? null : localPath.TryLoadResource(out AudioStream stream) ? Play(stream, lifeTime) : null;
    public static AudioStreamPlayer Play(AudioStream stream, float lifeTime = 0) => Node.IsNull() ? null : Node.PlayLocal(stream, lifeTime);
    public AudioStreamPlayer PlayLocal(AudioStream stream, float lifeTime = 0)
    {
        AudioStreamPlayer player = new();
        AddChild(player);

        if (lifeTime > 0)
            player.Destroy(lifeTime);

        player.Stream = stream;
        player.Play(0);
        return player;
    }
}