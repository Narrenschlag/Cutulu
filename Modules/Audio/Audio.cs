using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Audio System for static, 3D and 2D audio players.
    /// </summary>
    public partial class Audio : Node
    {
        private static Dictionary<byte, Node> GroupParents;
        private static Index<byte, Node> Index;
        private static Audio Singleton;

        public override void _EnterTree()
        {
            Singleton = this;
        }

        /// <summary>
        /// Plays an audio stream in 3D space
        /// <br/>Stream is the sound file
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// <br/>Remove Percent removes the player after (1.0 = 100%) of it's length. Set to 0.0 for loops.
        /// </summary>
        public static AudioStreamPlayer Play(AudioStream stream, byte audioGroup = 0, float removePercent = 1.0f)
        {
            // Create player and parent to group parent
            AudioStreamPlayer player = new();

            // Prepare values
            PreparePlay(player, ref stream, ref audioGroup, ref removePercent);

            // Assign remaining values
            player.Stream = stream;
            player.Play();

            // Return result
            return player;
        }

        /// <summary>
        /// Plays an audio stream in 3D space
        /// <br/>Stream is the sound file
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// <br/>Global Position is the position assigned to the stream player
        /// <br/>Remove Percent removes the player after (1.0 = 100%) of it's length. Set to 0.0 for loops.
        /// </summary>
        public static AudioStreamPlayer3D Play(AudioStream stream, Vector3 globalPosition, byte audioGroup = 0, float removePercent = 1.0f)
        {
            // Create player and parent to group parent
            AudioStreamPlayer3D player = new();

            // Prepare values
            PreparePlay(player, ref stream, ref audioGroup, ref removePercent);

            // Assign remaining values
            player.GlobalPosition = globalPosition;
            player.Stream = stream;
            player.Play();

            // Return result
            return player;
        }

        /// <summary>
        /// Plays an audio stream in 2D space
        /// <br/>Stream is the sound file
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// <br/>Global Position is the position assigned to the stream player
        /// <br/>Remove Percent removes the player after (1.0 = 100%) of it's length. Set to 0.0 for loops.
        /// </summary>
        public static AudioStreamPlayer2D Play(AudioStream stream, Vector2 globalPosition, byte audioGroup = 0, float removePercent = 1.0f)
        {
            // Create player and parent to group parent
            AudioStreamPlayer2D player = new();

            // Prepare values
            PreparePlay(player, ref stream, ref audioGroup, ref removePercent);

            // Assign remaining values
            player.GlobalPosition = globalPosition;
            player.Stream = stream;
            player.Play();

            // Return result
            return player;
        }

        /// <summary>
        /// Prepares incomming streams and their nodes for less code
        /// </summary>
        private static void PreparePlay(Node playerNode, ref AudioStream stream, ref byte audioGroup, ref float removePercent)
        {
            // Setup group parent
            if ((GroupParents ??= new()).ContainsKey(audioGroup) == false)
            {
                GroupParents.Add(audioGroup, new());
                Singleton.AddChild(GroupParents[audioGroup]);
                GroupParents[audioGroup].Name = $"Group [{audioGroup}]";
            }

            // Set parent
            GroupParents[audioGroup].AddChild(playerNode);

            // Remove after time
            if (removePercent > 0)
            {
                playerNode.Destroy((float)stream.GetLength() * removePercent);
            }

            // Register to index
            (Index ??= new()).Add(audioGroup, playerNode);
        }

        /// <summary>
        /// Removes all audio players of given group
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// </summary>
        public static void Remove(byte audioGroup = 0)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                // Remove group parent
                parent.Destroy();

                // Clear index entry
                (Index ??= new()).Remove(audioGroup);
            }
        }

        /// <summary>
        /// Stops all audio players of given group
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// </summary>
        public static void Stop(byte audioGroup = 0)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                // Static
                foreach (AudioStreamPlayer player in parent.GetNodesInChildren<AudioStreamPlayer>())
                {
                    player.Stop();
                }

                // 3D
                foreach (AudioStreamPlayer3D player in parent.GetNodesInChildren<AudioStreamPlayer3D>())
                {
                    player.Stop();
                }

                // 2D
                foreach (AudioStreamPlayer2D player in parent.GetNodesInChildren<AudioStreamPlayer2D>())
                {
                    player.Stop();
                }
            }
        }

        /// <summary>
        /// Pauses all audio players of given group
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// </summary>
        public static void Pause(byte audioGroup = 0)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                // Static
                foreach (AudioStreamPlayer player in parent.GetNodesInChildren<AudioStreamPlayer>())
                {
                    player.StreamPaused = true;
                }

                // 3D
                foreach (AudioStreamPlayer3D player in parent.GetNodesInChildren<AudioStreamPlayer3D>())
                {
                    player.StreamPaused = true;
                }

                // 2D
                foreach (AudioStreamPlayer2D player in parent.GetNodesInChildren<AudioStreamPlayer2D>())
                {
                    player.StreamPaused = true;
                }
            }
        }

        /// <summary>
        /// Continues all audio players of given group
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// </summary>
        public static void UnPause(byte audioGroup = 0)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                // Static
                foreach (AudioStreamPlayer player in parent.GetNodesInChildren<AudioStreamPlayer>())
                {
                    player.StreamPaused = false;
                }

                // 3D
                foreach (AudioStreamPlayer3D player in parent.GetNodesInChildren<AudioStreamPlayer3D>())
                {
                    player.StreamPaused = false;
                }

                // 2D
                foreach (AudioStreamPlayer2D player in parent.GetNodesInChildren<AudioStreamPlayer2D>())
                {
                    player.StreamPaused = false;
                }
            }
        }

        /// <summary>
        /// Plays all audio players of given group
        /// <br/>AudioGroup sets the group of the audio for easy access of all group members
        /// <br/>FromPosition sets the offset of the tracks
        /// </summary>
        public static void Play(byte audioGroup = 0, float fromPosition = 0)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                // Static
                foreach (AudioStreamPlayer player in parent.GetNodesInChildren<AudioStreamPlayer>())
                {
                    player.Play();
                }

                // 3D
                foreach (AudioStreamPlayer3D player in parent.GetNodesInChildren<AudioStreamPlayer3D>())
                {
                    player.Play();
                }

                // 2D
                foreach (AudioStreamPlayer2D player in parent.GetNodesInChildren<AudioStreamPlayer2D>())
                {
                    player.Play();
                }
            }
        }
    }
}