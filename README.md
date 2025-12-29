# Hybrid Mono

Hybrid Mono is a Data-Oriented Design (DOD) architecture for Unity GameObjects. It provides a bridge between the traditional MonoBehaviour-based approach and the efficiency of Data-Oriented Design, allowing you to use ECS-like patterns while remaining within the familiar GameObject ecosystem.

## Purpose

The package is designed to bring the benefits of Data-Oriented Design to GameObjects by:
- Separating data from logic using `MonoComponent` and `MonoBuffer`.
- Providing a systematic way to process data through `MonoSystem`.
- Enabling lightweight job execution on MonoBehaviours via `IMonoJob`.
- Automating the transition from authoring components to runtime data through a flexible `MonoBaker` system.

## Core Runtime Components

The following components located in the `Runtime` folder form the backbone of the Hybrid Mono architecture:

### Components
- **MonoComponent<T>**: A MonoBehaviour wrapper for ECS `IComponentData`. It allows data-only structs to be attached to GameObjects and edited in the Inspector.
- **MonoBuffer<T>**: A MonoBehaviour wrapper for a `NativeList<T>` (equivalent to ECS Dynamic Buffers). It provides efficient, contiguous memory storage for collections of data on GameObjects.
- **MonoAuthoring**: A base class for components that serve as the "authoring" source, which gets baked into runtime data.

### Core
- **MonoBaker<TAuthoring>**: The base class for custom bakers. It defines how an authoring MonoBehaviour is converted into `MonoComponent` or `MonoBuffer` data.
- **MonoBakingSystem**: A static system that automatically discovers and executes bakers in the editor (via `OnValidate`) and at runtime (when scenes load).
- **MonoSystemBootstrap**: Automatically instantiates all `MonoSystem` types in the project when the game starts.
- **Singleton<T> & PersistentSingleton<T>**: Utility base classes for creating singleton MonoBehaviours, used by the system for global management.

### Jobs
- **IMonoJob**: An interface for creating data-processing jobs that can be run on MonoBehaviours.
- **MonoJobExtensions**: Provides a `Run` extension method to execute `IMonoJob` logic sequentially with built-in profiling support.

### System
- **MonoSystem**: The base class for logic-heavy systems. It provides overridable lifecycle hooks (`OnUpdate`, `OnFixedUpdate`, `OnLateUpdate`) and utility methods to find components and run jobs, centralizing logic execution rather than spreading it across individual MonoBehaviours.

## Architecture

Hybrid Mono follows a simple flow:
1. **Authoring**: Create standard MonoBehaviours (inheriting from `MonoAuthoring` or containing raw data).
2. **Baking**: Use a `MonoBaker` to translate that authoring data into `MonoComponent<T>` or `MonoBuffer<T>`.
3. **Processing**: Create a `MonoSystem` that finds these components and processes their data, often using `IMonoJob` for structured execution.

This architecture ensures that your data is cleanly separated from your logic, making your codebase more maintainable and performant while still leveraging Unity's built-in GameObject features.
