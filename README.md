# DSystem

DSystem is a lightweight framework designed to simplify dependency management and event-driven architectures in Unity projects. Here's a guide to understanding its components and how to use them.

---

## Installation

To use DSystem, you need to install the `MainInjector` component in your Unity scene. This serves as the core of the system, managing dependencies and events.

---

## Key Features

- **Dependency Injection**: Easily inject dependencies into your components and classes.
- **Event System**: Manage global and local events with flexible subscription and interception.
- **Lifecycle Management**: Automate initialization, updates, and disposal of components.

---

## Interfaces

### 1. `IInitializable`

For singleton initialization. Dependencies are resolved before the `Initialize` method is called.

```csharp
[AutoRegistry]
public class ExampleSingleton : IInitializable
{
    public void Initialize()
    {
        // Initialization logic
    }
}
```

### 2. `IUpdatable`

Provides a frame-based update function similar to `MonoBehaviour`'s `Update`.

```csharp
[AutoRegistry]
public class ExampleSingleton : IUpdatable
{
    public void Update()
    {
        // Called every frame
    }
}
```

### 3. `IDisposable`

Disposes of the singleton when no longer needed.

---

## Attributes

### 1. `AutoRegistry`

Registers a class as a singleton.

```csharp
[AutoRegistry]
public class ExampleSingleton {}
```

### 2. `Inject`

Injects dependencies into fields.

```csharp
[AutoRegistry]
public class ExampleSingleton
{
    [Inject] private Example example;
    [Inject] private Collider[] colliders;
}
```

Options:

- `InjectParams.IncludeInactive`: Injects disabled components.
- `InjectParams.UseGlobal`: Injects singleton instances.

### 3. `DisableInitialize`

Defers initialization until the scene is loaded.

```csharp
[DisableInitialize]
public class ExampleComponent : DBehaviour
{
    protected override void OnInitialize()
    {
        // Called after the scene is loaded
    }
}
```

---

## Event System

### 1. Global Events

Define events with interfaces and manage them globally.

```csharp
[Listener]
public interface IExampleListener
{
    void SomeEvent();
}

// Sending events
GetDAction<IExampleListener>().Invoke(l => l.SomeEvent());

// Subscribing to events
public class ExampleComponent : DBehaviour, IExampleListener
{
    public void SomeEvent() {}
}
```

### 2. Local Events

Manage events within a specific hierarchy.

```csharp
[Listener(useGlobal: false)]
public interface IExampleListener
{
    void SomeEvent();
}

[RegistryListeners(typeof(IExampleListener))]
public class ExampleParent : DBehaviour
{
    private void ExampleMethod()
    {
        GetDAction<IExampleListener>().Invoke(l => l.SomeEvent());
    }
}
```

### 3. Event Handlers

Intercept and modify event data.

```csharp
public class ExampleHandler : EventHandler<IExampleListener>, IExampleListener
{
    public void SomeEvent(int amount)
    {
        Listener.SomeEvent(amount * 2); // Modify event data
    }
}
```

---

## DBehaviour

`DBehaviour` is a wrapper for Unity's `MonoBehaviour`. It automates event subscription and lifecycle management.

**Note**: Use `Awake`, `OnDestroy`, `OnDisable`, and `OnEnable` only by overriding them.

```csharp
public class ExampleComponent : DBehaviour
{
    protected override void OnInitialize()
    {
        // Called after the scene is injected
    }

    protected override void OnDisable()
    {
        // Cleanup logic
    }
}
```

---

## DAction

`DAction` facilitates event management between classes and interfaces.

```csharp
public class ExampleClass
{
    public readonly DAction<IExampleInterface> ExampleAction = new();

    private void ExampleMethod()
    {
        ExampleAction.Invoke(l => l.SomeEvent());
    }
}
```

---

## License

This project is licensed under the [MIT License](LICENSE).

---

Thank you for using DSystem! If you have any questions or feedback, feel free to reach out.

