# Hybrid Mono System

[![Unity Version](https://img.shields.io/badge/Unity-2022.3.62f3-blue.svg)](https://unity.com/)
[![Package](https://img.shields.io/badge/UPM-dev.bxbstudio.hybrid--mono-blue)](https://github.com/bxbstudio/hybrid-mono)

Hybrid Mono System provides a lightweight bridge between Unity `GameObject` workflows and a dedicated ECS data world. It is intended for runtime systems that want ECS-style component storage, queries, and baker-driven setup while keeping the GameObject world authoritative for scene objects and engine APIs.

## Features

- Dedicated Hybrid Mono ECS world created at runtime
- `GameObject <-> Entity` registration and lookup through `MonoHybridAPI`
- ECS component, buffer, enableable-component, and query helpers for mirrored entities
- Runtime baker discovery with automatic scene-load bake passes through `MonoBakingSystem`
- `MonoBaker<TAuthoring>` base class for authoring-to-entity adapters
- Bulk read/write helpers for component arrays and buffer access

## Requirements

- Unity `2022.3.62f3` or later in the `2022.3` line
- `com.unity.entities` `1.4.3`
- `dev.bxbstudio.utilities` `1.1.11`

## Installation

### Using Unity Package Manager

1. Open `Window > Package Manager`.
2. Select `+ > Add package from git URL...`.
3. Enter:

```text
https://github.com/bxbstudio/hybrid-mono.git
```

## Core APIs

### `MonoHybridAPI`

`MonoHybridAPI` is the main runtime surface for the Hybrid Mono world. It handles registration, entity lookup, ECS data access, and query creation for mirrored `GameObject` instances.

Common capabilities include:

- `RegisterGameObject()` to create or fetch the mirrored entity for a `GameObject`
- `TryGetEntity()` / `TryGetGameObject()` for bridge lookups
- `AddComponentData()`, `GetComponentData()`, `SetComponentData()`, and `RemoveComponent()`
- `AddBuffer()`, `EnsureBuffer()`, `GetBuffer()`, and `RemoveBuffer()`
- `IsComponentEnabled()` and `SetComponentEnabled()` for enableable components
- `CreateQuery()` and bulk helpers for working with groups of entities

### `MonoBakingSystem`

`MonoBakingSystem` discovers concrete `MonoBaker<TAuthoring>` implementations at runtime, runs bake passes for loaded scenes, and skips authoring components inside SubScenes so Unity's ECS baking pipeline can own those objects.

Useful entry points include:

- `EnsureInitialBakeCompleted()`
- `BakeAllInScene()`
- `BakeScene()`
- `BakeAuthoring()`
- `HasBaker()`
- `IsBaked()`

### `MonoBaker<TAuthoring>`

Derive from `MonoBaker<TAuthoring>` when you want to mirror a runtime authoring component into the Hybrid Mono world. The base class provides:

- `Authoring` for the current source component
- `GameObject` for the mirrored object
- `Entity` for the mirrored ECS entity
- `IsRebake` to distinguish first bake from rebake

## Usage Examples

### Register a `GameObject` and write component data

```csharp
using Unity.Entities;
using UnityEngine;
using Utilities.HybridMono;

public struct Health : IComponentData
{
    public float Value;
}

public class HybridHealthBootstrap : MonoBehaviour
{
    private void Awake()
    {
        Entity entity = MonoHybridAPI.RegisterGameObject(gameObject);
        MonoHybridAPI.AddComponentData(entity, new Health { Value = 100f });

        Health health = MonoHybridAPI.GetComponentData<Health>(gameObject);
        health.Value -= 10f;
        MonoHybridAPI.SetComponentData(gameObject, health);
    }
}
```

### Ensure and populate a dynamic buffer

```csharp
using Unity.Entities;
using UnityEngine;
using Utilities.HybridMono;

public struct WheelReference : IBufferElementData
{
    public Entity Value;
}

public class HybridWheelBufferBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject[] wheels;

    private void Awake()
    {
        DynamicBuffer<WheelReference> buffer =
            MonoHybridAPI.EnsureBuffer<WheelReference>(gameObject, wheels.Length);

        buffer.Clear();

        foreach (GameObject wheel in wheels)
        {
            Entity wheelEntity = MonoHybridAPI.RegisterGameObject(wheel);
            buffer.Add(new WheelReference { Value = wheelEntity });
        }
    }
}
```

### Create a runtime baker

```csharp
using Unity.Entities;
using UnityEngine;
using Utilities.HybridMono;

public struct VehicleTag : IComponentData {}

public class VehicleAuthoring : MonoBehaviour
{
    public float mass = 1200f;
}

public struct VehicleMass : IComponentData
{
    public float Value;
}

public sealed class VehicleAuthoringBaker : MonoBaker<VehicleAuthoring>
{
    public override void Bake(VehicleAuthoring authoring)
    {
        MonoHybridAPI.AddComponentData(Entity, new VehicleTag());
        MonoHybridAPI.AddComponentData(Entity, new VehicleMass { Value = authoring.mass });
    }
}
```

When the scene loads, `MonoBakingSystem` discovers the baker automatically and runs it for matching runtime authoring components outside SubScenes.

## What This Package Does Not Do

- It does not provide a generic physics bridge between Unity Physics and PhysX.
- It does not synchronize transforms between GameObjects and a second simulation world.
- It does not abstract `Rigidbody` force application into a package-level runtime pipeline.
- It does not include higher-level `MonoSystem` infrastructure in the current package source.

## Typical Use Cases

- Runtime authoring adapters that mirror scene `MonoBehaviour` data into ECS entities
- Hybrid gameplay systems that read/write ECS-style data while resolving back to `GameObject` owners
- Packages that want shared data-oriented processing without moving all authoring into SubScenes

---

Developed by [BxB Studio](https://bxbstudio.dev)
