namespace GamePlay
{
    /// <summary>
    /// UI日志接口，使用者可以实现此接口来自定义日志输出
    /// </summary>
    public interface IUILogger
    {
        /// <summary>
        /// 普通日志
        /// </summary>
        void Log(string message);
        
        /// <summary>
        /// 警告日志
        /// </summary>
        void LogWarning(string message);
        
        /// <summary>
        /// 错误日志
        /// </summary>
        void LogError(string message);
    }
    
    /// <summary>
    /// 默认使用 Unity Debug 的日志实现
    /// </summary>
    public class DefaultUILogger : IUILogger
    {
        public void Log(string message)
        {
            UnityEngine.Debug.Log($"[UIModule] {message}");
        }

        public void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"[UIModule] {message}");
        }

        public void LogError(string message)
        {
            UnityEngine.Debug.LogError($"[UIModule] {message}");
        }
    }
}

