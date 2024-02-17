using System.Collections.Generic;
using System.Threading.Tasks;
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
        public static Audio Singleton;

        public override void _EnterTree()
        {
            Singleton = this;
        }

        #region Get             ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static List<T> Get<T>(byte audioGroup) where T : Node
        {
            if (Singleton.IsNull() || GroupParents == null || GroupParents.TryGetValue(audioGroup, out Node parent) == false) return null;

            List<T> list = parent.GetNodesInChildren<T>();
            return list.Count < 1 ? null : list;
        }
        #endregion

        #region Play            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) == false || parent.IsNull())
            {
                parent = new();
                Singleton.AddChild(parent);
                parent.Name = $"Group [{audioGroup}]";

                GroupParents.Set(audioGroup, parent);
            }

            // Set parent
            parent.AddChild(playerNode);

            // Remove after time
            if (removePercent > 0)
            {
                playerNode.Destroy((float)stream.GetLength() * removePercent);
            }

            // Register to index
            (Index ??= new()).Add(audioGroup, playerNode);
        }
        #endregion

        #region Other Utility   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
                    player.Play(fromPosition);
                }

                // 3D
                foreach (AudioStreamPlayer3D player in parent.GetNodesInChildren<AudioStreamPlayer3D>())
                {
                    player.Play(fromPosition);
                }

                // 2D
                foreach (AudioStreamPlayer2D player in parent.GetNodesInChildren<AudioStreamPlayer2D>())
                {
                    player.Play(fromPosition);
                }
            }
        }
        #endregion

        #region Fade In/Out     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fades in audio player volume.
        /// </summary>
        public static void FadeIn(byte audioGroup, float duration, float volumeDb, byte fadeResolution = 50)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                foreach (Node player in parent.GetChildren())
                {
                    FadeIn(player, duration, volumeDb, fadeResolution);
                }
            }
        }

        /// <summary>
        /// Fades in audio player volume.
        /// </summary>
        public static async void FadeIn(Node player, float duration, float volumeDb, byte fadeResolution = 50)
        {
            lock (player)
            {
                SetVolume(player, -40);
            }

            int step = Mathf.RoundToInt(duration / fadeResolution * 1000);
            for (byte i = 0; i < fadeResolution; i++)
            {
                if (player.IsNull())
                {
                    return;
                }

                lock (player)
                {
                    SetVolume(player, Mathf.Lerp(GetVolume(player), volumeDb, (float)i / fadeResolution));
                }

                await Task.Delay(step);
            }
        }

        /// <summary>
        /// Fades out audio player group volume.
        /// </summary>
        public static async void FadeOut(byte audioGroup, float duration, bool removeOnEnd = true, byte fadeResolution = 50)
        {
            // Validate group
            if ((GroupParents ??= new()).TryGetValue(audioGroup, out Node parent) && parent.NotNull())
            {
                foreach (Node player in parent.GetChildren())
                {
                    FadeOut(player, duration, removeOnEnd, fadeResolution);
                }
            }

            // Remove group on end if has no children
            if (removeOnEnd)
            {
                await Task.Delay(Mathf.RoundToInt(1000 * duration) + 1);

                if ((GroupParents ??= new()).TryGetValue(audioGroup, out parent) && parent.NotNull() && parent.GetChildCount() < 1)
                {
                    Remove(audioGroup);
                }
            }
        }

        /// <summary>
        /// Fades out audio player volume.
        /// </summary>
        public static async void FadeOut(Node player, float duration, bool removeOnEnd = true, byte fadeResolution = 50)
        {
            int step = Mathf.RoundToInt(duration / fadeResolution * 1000);
            float volume = 40f / fadeResolution;

            for (byte i = 0; i < fadeResolution; i++)
            {
                lock (player)
                {
                    if (player.IsNull())
                    {
                        return;
                    }

                    ModifyVolume(player, -volume);
                }

                await Task.Delay(step);
            }

            if (removeOnEnd)
            {
                lock (player)
                {
                    if (player.IsNull())
                    {
                        return;
                    }

                    player.Destroy();
                }
            }
        }
        #endregion

        #region Volume          ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Modifies audio player's volumeDb
        /// </summary>
        private static void ModifyVolume(Node node, float volumeDb)
        {
            if (node is AudioStreamPlayer)
            {
                (node as AudioStreamPlayer).VolumeDb += volumeDb;
            }

            else if (node is AudioStreamPlayer3D)
            {
                (node as AudioStreamPlayer3D).VolumeDb += volumeDb;
            }

            else if (node is AudioStreamPlayer2D)
            {
                (node as AudioStreamPlayer2D).VolumeDb += volumeDb;
            }
        }

        /// <summary>
        /// Sets audio player's volumeDb
        /// </summary>
        private static void SetVolume(Node node, float volumeDb)
        {
            if (node is AudioStreamPlayer)
            {
                (node as AudioStreamPlayer).VolumeDb = volumeDb;
            }

            else if (node is AudioStreamPlayer3D)
            {
                (node as AudioStreamPlayer3D).VolumeDb = volumeDb;
            }

            else if (node is AudioStreamPlayer2D)
            {
                (node as AudioStreamPlayer2D).VolumeDb = volumeDb;
            }
        }

        /// <summary>
        /// Get audio player's volumeDb
        /// </summary>
        private static float GetVolume(Node node)
        {
            if (node is AudioStreamPlayer)
            {
                return (node as AudioStreamPlayer).VolumeDb;
            }

            else if (node is AudioStreamPlayer3D)
            {
                return (node as AudioStreamPlayer3D).VolumeDb;
            }

            else if (node is AudioStreamPlayer2D)
            {
                return (node as AudioStreamPlayer2D).VolumeDb;
            }

            return default;
        }
        #endregion
    }
}