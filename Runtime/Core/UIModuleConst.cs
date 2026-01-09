namespace GamePlay
{
    public enum UIStatus
    {
        None,
        Creating,
        Created,
        Showing,
        Showed,
        Hiding,
        Hided,
        Disposed,
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