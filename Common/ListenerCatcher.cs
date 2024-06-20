namespace DSystem
{
    public class ListenerCatcher<T> where T : class
    {
        public T Listener { get; internal set; }
    }
}