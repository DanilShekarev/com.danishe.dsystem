namespace DSystem
{
    public class EventHandler<T> where T : class
    {
        public virtual int GetPriority() => 0;
        public T Listener { get; internal set; }
    }
}