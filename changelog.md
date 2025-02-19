﻿# Changelog

## [2.5.3] - 2025-02-19

### Added
- Support DSystemUtils.


## [2.5.2] - 2025-02-18

### Fixed

- **DAction**: Fix exception log.

## [2.5.1] - 2024-12-11

### Fixed
- **DBehavior**: Now when disable unsubscribe from local events.

## [2.5.0] - 2024-12-11

### Removed
- **MainInjector**: The following methods have been removed:
    - `RegistryCatcher<T>`
    - `RemoveCatcher<T>`
    - `InvokeListenersEvent`

### Deprecated
- **MainInjector**: The following methods are now deprecated:
    - `RegistryListener<T>`
    - `RemoveListener<T>`
    - `RegistryListenerCatcher<T>`
    - `RegistryUnsubscribeListenerCatcher<T>`
    - `InvokeListenersReflection`
    - `InvokeListeners<T>`
    - `ForeachListeners<T>`

- **DBehaviour**: The following methods are now deprecated:
    - `SubscribeTo<T>`
    - `InvokeListeners<T>`
    - `RegistryCatcher<T>`
    - `RegistryUnsubscribeCatcher<T>`

### Added
- **MainInjector**: New methods introduced:
    - `DAction<T> GetDAction<T>(bool createInstance = true)`
    - `IDAction GetDAction(Type type, bool createInstance = true)`

- **DBehaviour**: New methods introduced:
    - `DAction<T> GetDAction<T>(bool createInstance = true)`
    - `IDAction GetDAction(Type type, bool createInstance = true)`

- **New Class**: `DAction<T>`
- **New Interface**: `IDAction`

### Changes
- **DynamicSingleton Attribute**: Can now be used without the `Singleton` attribute.
- **ListenerCatcher<T>**: Renamed to `EventHandler<T>`.

---

### Notes
This update introduces significant changes to the API, providing a more robust and flexible approach for dependency injection and event handling. Deprecated methods will remain available temporarily for backward compatibility but are recommended to be replaced with their new counterparts.

For detailed migration steps, please refer to the documentation.



## [2.4.2] - 2024-12-10

## Added

- Added InvokeListeners(Type, Action<object>) method.

## [2.4.1] - 2024-11-01

## Fixed

- Fix registry listeners attributes ignore from inherit classes

## [2.4.0] - 2024-10-29

## Added

- Added Universal Event Invoker component (Experimental)

## [2.3.9] - 2024-10-25

### Improved

- Registry/Remove listener check interface implementation

## [2.3.8] - 2024-10-18

### Improved

- Invoke listeners now is safe for exceptions.

## [2.3.7] - 2024-10-07

### Fixed

- Fixed safe sub/unsubscribe on double invoke one event listener.

## [2.3.6] - 2024-09-23

### Added

- Add DSystemBase class

### Fixed

- Fixed unsubscribe listener function

## [2.3.5] - 2024-07-24

### Fixed

- Fixed double subscribe listener for local listeners

## [2.3.4] - 2024-07-23

### Added

- Add SceneInjected event

## [2.3.3] - 2024-07-01

### Fixed

- Fixed unsubscribe listener on same event

## [2.3.2] - 2024-07-01

### Fixed

- Fixed subscribe listener on same event

## [2.3.1] - 2024-07-01

### Fixed

- Disable listeners when disable gameObject
- Protect multi subscribe for global listeners

## [2.3.0] - 2024-06-20

### Added

- Add listener catchers

### Changed

- Rewrite readme
- Rename DBehaviour method InvokeListener to InvokeListeners

## [2.2.7] - 2024-06-12

### Fixed

- Auto unsubscribe for dynamic injection instances

## [2.2.6] - 2024-06-12

### Fixed

- Fix pool destroy catchers.

## [2.2.5] - 2024-06-12

### Fixed

- Fix find singleton attribute.

### Added

- Dynamic singleton pattern.

## [2.2.4] - 2024-06-12

### Added

- New disable initialized on dispose catching system.