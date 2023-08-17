# DSystem
For DSystem to work, you need to install the MainInjector component on the scene.
# Overview
## Classes
### DBehaviour
The Awake function has been replaced by the Initialize function.
Example:
``` C#
public class ExampleComponent : DBehaviour
{
    protected override void Initialize()
    {
       //Executing like Awake
    }
}
```
To inject dependencies of disabled components or GameObject, use the <b>DInstantiate()</b> function and the <b>DisableInitialize</b> attribute.
## Interfaces
To dispose of the singleton, use the <b>IDisposable</b> system interface.
### IInitializable
Interface for singleton initialization. At the time of calling the initialization function, all dependencies will be resolved.
Example:
``` C#
[AutoRegistry]
public class ExampleSingleton : DBehaviour, IInitializable
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
public class ExampleSingleton : DBehaviour, IUpdatable
{
    public void Update()
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
Works only in singletons and DBehaviour classes.
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
}
```
### DisableInitialize
Initializes the DBehaviour component even when the GameObject or script is disabled.
Example:
``` C#
[DisableInitialize]
public class ExampleComponent : DBehaviour
{
    protected override void Initialize()
    {
       //Executing on disabled component or GameObject
    }
}
```
