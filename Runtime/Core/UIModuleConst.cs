namespace GamePlay
{
    public enum UIStatus
    {
        Sleeping,               // 休眠
        Opening,                // 打开中
        Showing,                // 展示中
        Hiding,                 // 隐藏中
        Disposing,              // 销毁中
    }
    
    public enum UIOperateState
    {
        None,
        PrepareCreate,
        Created,
        PrepareOpen,
        Opened,
        PrepareClose,
        Closed,
        PrepareDispose,
        Disposed
    }
}