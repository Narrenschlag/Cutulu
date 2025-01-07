namespace Cutulu.Lattice
{
    using Godot;
    using Core;

    public static class CoreBridge
    {
        public static void Log(string message)
        {
            Debug.Log($"[CoreBridge]: {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[CoreBridge]: {message}");
        }

        public static Node GetNode(string path)
        {
            return GD.Load<Node>(path);
        }

        // Add access to custom systems here (e.g., game state, assets, or utilities).
    }
}