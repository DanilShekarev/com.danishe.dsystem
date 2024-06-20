# DSystem
For DSystem to work, you need to install the MainInjector component on scene.
# Overview
## Interfaces
To dispose of the singleton, use the <b>IDisposable</b> system interface.
### IInitializable
Interface for singleton initialization. At the time of calling the initialization function, all dependencies will be resolved.
Example:
``` C#
[AutoRegistry]
public class ExampleSingleton : IInitializable
{
    public void Initialize()
    {
        
    }
}
```
### IUpdatable
Interface for the update function. It works the same way as Update in MonoBehaviour.
Example:
``` C#
[AutoRegistry]
public class ExampleSingleton : IUpdatable
{
    public void Update()
    {
        //Called every frame
    }
}
```
## Attributes
### AutoRegistry
Registers the class as a singleton. Example:
``` C#
[AutoRegistry]
public class ExampleSingleton
{

}
```
### Inject
Sets references to variables and fields.
Can inject child components
Examples:
``` C#
[AutoRegistry]
public class ExampleSingleton
{
    [Inject] private Example example;
}

public class ExampleComponent : DBehaviour
{
    [Inject] private Example example;
    [Inject] private Collider[] colliders;
}
```
InjectParams IncludeInactive inject disabled component like GetComponentInChildren<Example>(true).
Can be used with arrays.
Example:
``` C#
public class ExampleComponent : DBehaviour
{
    [Inject(InjectParams.IncludeInactive)]
    private Example example;
    
    [Inject(InjectParams.IncludeInactive)]
    private Example[] example;
}
```
InjectParams UseGlobal inject singleton instance.
Usually used for inject singleton component.
Example:
``` C#
public class ExampleComponent : DBehaviour
{
    [Inject(InjectParams.UseGlobal)]
    private Example example;
}

[Singleton/DynamicSingleton]
public class Example : DBehaviour {}
```
Can add onInject event. Event call when inject instance.
``` C#
public class ExampleComponent : DBehaviour
{
    [Inject(nameof(OnInjectExample))] private Example example;

    private void OnInjectExample()
    {
        //Called when inject example.
    }
}
```
### DisableInitialize
This script will be initialized when loading scene. Example:
``` C#
[DisableInitialize]
public class ExampleComponent : DBehaviour
{
    protected override void OnInitialize()
    {
        //Called when on loaded scene
    }
}
```
## Event system
### Global events
For event provide use interface. Example:
``` C#
[Listener]
public interface IExampleListener
{
    public void SomeEvent();
}
```
To send an event, you need to call the InvokeListeners method. Example:
``` C#
MainInjector.Instance.InvokeListeners<IExampleListener>(l =>
{
    l.SomeEvent();
});

//Alternative invokation.
foreach (var listener in MainInjector.Instance.ForeachListeners<IExampleListener>())
{
    listener.SomeEvent();
}
```
To subscribe to an event, it is enough to inherit the interface only for DBehaviour objects. 
For other implementations, you must manually subscribe via the RegistryListener method.
For unsubscribe use RemoveListener method.
Example:
``` C#
[AutoRegistry]
public class ExampleSingleton : IInitializable, IExampleListener
{
    public void Initialize()
    {
        //Manual subscription
        MainInjector.Instance.RegistryListener<IExampleListener>(this);
    }

    public void SomeEvent()
    {
        
    }
}

public class ExampleComponent : DBehaviour, IExampleListener
{
    public void SomeEvent()
    {
        
    }
}
```
## Local events
For event provide use interface. UseGlobal false mark interface local.
The Up value is true, which indicates that events will be sent up the hierarchy. Example:
``` C#
[Listener(useGlobal: false)]
public interface IExampleListener
{
    public void SomeEvent();
}

[Listener(useGlobal: false, up: true)]
public interface IExampleUpListener
{
    public void SomeEvent();
}
```
To send an event, you need to call the InvokeListeners method.
You also need to add the RegistryListeners attribute to register listeners.
Example:
``` C#
[RegistryListeners(typeof(IExampleListener))]
public class ExampleParent : DBehaviour
{
    private void ExampleMethod()
    {
        InvokeListeners<IExampleListener>(l => l.SomeEvent());
    }
}
```
To subscribe to an event, it is enough to inherit the interface only for DBehaviour objects. 
``` C#
public class ExampleChild : DBehaviour, IExampleListener
{
    public void SomeEvent()
    {
        
    }
}
```
To sign outside the hierarchy, you must use the SubscribeTo method.
To unsubscribe, you must call Action with the returned SubscribeTo method.
Example:
``` C#
public class ExampleComponent : DBehaviour, IExampleListener
{
    [SerializeField] private DBehaviour other;
    
    private Action _unsubscribeAction;
    
    protected override void OnInitialize()
    {
        _unsubscribeAction = SubscribeTo<IExampleListener>(other);
    }
    
    protected override void OnDispose()
    {
        _unsubscribeAction.Invoke();
    }

    public void SomeEvent()
    {
        
    }
}
```
### Listener catcher
Listener Catcher intercepts events and changes them.
Example:
``` C#
[AutoRegistry]
public class ExampleSingleton : IInitializable, IDisposable
{
    private ExampleCatcher _exampleCatcher;
    
    public void Initialize()
    {
        _exampleCatcher = new ExampleCatcher();
        MainInjector.Instance.RegistryCatcher(_exampleCatcher);
    }

    public void ExampleMethod()
    {
        MainInjector.Instance.InvokeListeners<IExampleListener>(l => l.SomeEvent(10));
    }

    public void Dispose()
    {
        MainInjector.Instance.RemoveCatcher(_exampleCatcher);
    }
}

[Listener]
public interface IExampleListener
{
    public void SomeEvent(int amount);
}

public class ExampleCatcher : ListenerCatcher<IExampleListener>, IExampleListener
{
    public void SomeEvent(int amount)
    {
        Listener.SomeEvent(amount*2);
    }
}

[AutoRegistry]
public class ExampleListener : IInitializable, IExampleListener
{
    public void Initialize()
    {
        MainInjector.Instance.RegistryListener<IExampleListener>(this);
    }
    
    public void SomeEvent(int amount)
    {
        
    }
}
```