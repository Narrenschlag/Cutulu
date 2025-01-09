namespace Cutulu.Lattice
{
    using Core;

    public static class CoreBridge
    {
        public static void Log(string message)
        {
            Debug.Log($"[Cutulu.Lattice]: {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[Cutulu.Lattice]: {message}");
        }
    }
}