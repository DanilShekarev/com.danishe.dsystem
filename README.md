# DSystem
For DSystem to work, you need to install the MainInjector component on the scene.
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
Interface for the update function. It works the same way as Update in Monobehaviour.
Example:
``` C#
[AutoRegistry]
public class ExampleSingleton : IUpdatable
{
    public void Update()
    {
        
    }
}
```
### IDisableInitialize
Allows initializing a component on a disabled GameObject. Overrides IInitialize.
Example:
``` C#
public class ExampleComponent : MonoBehaviour, IDisableInitialize
{
    public void Initialize()
    {
        
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
For components, you need to add the [Inject] attribute
Examples:
``` C#
[AutoRegistry]
public class ExampleSingleton
{
    [Inject] private Example example;
}

[Inject]
public class ExampleComponent : MonoBehaviour
{
    [Inject] private Example example;
}
```
