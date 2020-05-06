namespace Unity.Animation
{
    public static class Debug
    {
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
