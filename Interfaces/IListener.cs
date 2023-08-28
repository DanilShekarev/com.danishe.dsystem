using DSystem.Attributes;

namespace DSystem.Interfaces
{
    [ListenerInterface]
    public interface IListener<T, in T1> where T : class where T1 : struct 
    {
        void OnValueChanged(T1 val);
    }
}