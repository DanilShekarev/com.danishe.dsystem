namespace DSystem.Debugging
{
    public interface IInjectorDebugger
    {
        public void StartInjection(object instance);
        public void EndInjection(object instance);
    }
}