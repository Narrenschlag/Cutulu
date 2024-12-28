namespace Cutulu.Core.Audio
{
    using Godot;

    public static partial class DMod
    {
        public static AssetLibrary AssetLibrary { get; set; }

        /// <summary>
        /// Plays sound effect as child of given object. Node type defines the type of audio player.
        /// <br/>Overwrite can be used for GlobalPosition overwrite.
        /// </summary>
        public static Node Play(Node parent, DModule module, string bus = "Master", object overwrite = default)
        => Play(parent, module.GetInstance(), bus, overwrite);

        /// <summary>
        /// Plays sound effect as child of given object. Node type defines the type of audio player.
        /// <br/>Overwrite can be used for GlobalPosition overwrite.
        /// </summary>
        public static Node Play(Node parent, DModInstance instance, string bus = "Master", object overwrite = default)
        {
            if (instance.Stream.IsNull()) return null;
            if (parent.IsNull()) return null;

            var loop = false;

            switch (instance.Stream)
            {
                case AudioStreamOggVorbis ogg:
                    loop = ogg.Loop;
                    break;

                case AudioStreamMP3 mp3:
                    loop = mp3.Loop;
                    break;

                default:
                    break;
            }

            Node n = null;

            switch (parent)
            {
                case Node3D node:
                    var player3D = new AudioStreamPlayer3D()
                    {
                        PitchScale = instance.Pitch,
                        VolumeDb = instance.Volume,
                        Stream = instance.Stream,

                        Bus = bus,
                    };

                    parent.AddChild(n = player3D);

                    player3D.GlobalPosition = overwrite is Vector3 v3 ? v3 : node.GlobalPosition;
                    player3D.Play();
                    break;

                case Node2D node:
                    var player2D = new AudioStreamPlayer2D()
                    {
                        PitchScale = instance.Pitch,
                        VolumeDb = instance.Volume,
                        Stream = instance.Stream,

                        Bus = bus,
                    };

                    parent.AddChild(n = player2D);

                    player2D.GlobalPosition = overwrite is Vector2 v2 ? v2 : node.GlobalPosition;
                    player2D.Play();
                    break;

                default:
                    var player = new AudioStreamPlayer()
                    {
                        PitchScale = instance.Pitch,
                        VolumeDb = instance.Volume,
                        Stream = instance.Stream,

                        Bus = bus,
                    };

                    parent.AddChild(n = player);
                    player.Play();
                    break;
            }

            if (n.IsNull()) return null;

            // Destroy after lifetime
            if (loop == false) n.Destroy((float)instance.Stream.GetLength());

            return n;
        }
    }
}