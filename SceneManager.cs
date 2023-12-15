using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public partial class SceneManager : Node3D
    {
        public static int MaxSceneMemory = 8;

        public static List<int> BuildIndexHistory = new List<int>();
        protected static SceneManager Singleton;

        public override void _EnterTree() => Singleton = this;

        public static int getCurrentIndex() => BuildIndexHistory.IsEmpty() ? -1 : BuildIndexHistory[BuildIndexHistory.Count - 1];
        public static PackedScene ReloadScene() => Scene(getCurrentIndex());

        public static PackedScene Scene(int buildIndex) => Singleton.IsNull() ? null : Singleton.scene(buildIndex);
        public virtual PackedScene scene(int buildIndex) => default;

        public static Node Load(int sceneIndex, bool replaceAll = true) => Singleton.load(sceneIndex, replaceAll);
        public static Node Load(PackedScene scene, bool replaceAll = true) => Singleton.load(scene, replaceAll);

        protected virtual Node load(int sceneIndex, bool replaceAll = true)
        {
            // Memorize index
            if (sceneIndex >= 0 && getCurrentIndex() != sceneIndex)
            {
                BuildIndexHistory.Add(sceneIndex);

                if (BuildIndexHistory.Count > 8)
                    BuildIndexHistory.RemoveAt(0);
            }

            return Load(scene(sceneIndex), replaceAll);
        }

        protected virtual Node load(PackedScene scene, bool replaceAll = true)
        {
            if (replaceAll) this.Clear();

            return scene.IsNull() ? null :
                scene.Instantiate<Node>(Singleton);
        }
    }
}