using DSystem.Utils.Tree;

namespace DSystem.Debugging
{
    public class InjectorDebugger : IInjectorDebugger
    {
        public readonly Tree<object> Tree = new ();
        
        public void StartInjection(object instance)
        {
            Tree.Push(instance);
        }

        public void EndInjection(object instance)
        {
            Tree.Back();
        }
    }
}