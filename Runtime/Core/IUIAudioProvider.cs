namespace GamePlay
{
    /// <summary>
    /// UI音频播放接口，使用者需要实现此接口来提供音频播放功能
    /// </summary>
    public interface IUIAudioProvider
    {
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundName">音效名称</param>
        void PlaySound(string soundName);
    }
}