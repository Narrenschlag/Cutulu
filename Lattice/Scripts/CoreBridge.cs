namespace Cutulu.Lattice
{
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
    }
}