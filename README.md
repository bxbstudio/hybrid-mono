# Hybrid Mono

Hybrid Mono is an Entity-based architecture for Unity GameObjects. It provides a bridge between traditional MonoBehaviour-based development and the Unity ECS (Entities) system, allowing you to use proper ECS Entities with IComponentData and IBufferElementData while maintaining GameObject references.

## Purpose

The package creates mirror ECS Entities for GameObjects, enabling:
- **ECS Performance**: Use `IJobEntity` with `Schedule` and `ScheduleParallel` for burst-compiled parallel processing
- **GameObject Integration**: Access Entity data via GameObject references through `MonoHybridAPI`
- **Automatic Lifecycle**: Entities are created during scene load and destroyed when GameObjects are destroyed
- **Isolated World**: Dedicated "HybridMono World" keeps hybrid entities separate from standard ECS

## Core Components

### MonoHybridAPI (Static Class)
Central API for the HybridMono system. All component and buffer operations go through this static class:

```csharp
// Access the dedicated World
World world = MonoHybridAPI.World;
EntityManager em = MonoHybridAPI.EntityManager;

// Component access via GameObject
var data = MonoHybridAPI.GetComponentData<MyComponent>(gameObject);
MonoHybridAPI.SetComponentData(gameObject, newData);
MonoHybridAPI.AddComponentData(gameObject, newData);

// Component access via Entity (when you already have the Entity)
var data = MonoHybridAPI.GetComponentData<MyComponent>(entity);
MonoHybridAPI.SetComponentData(entity, newData);

// Buffer access
var buffer = MonoHybridAPI.GetBuffer<MyBufferElement>(gameObject);
var buffer = MonoHybridAPI.EnsureBuffer<MyBufferElement>(entity, capacity: 8);

// Entity lookup
if (MonoHybridAPI.TryGetEntity(gameObject, out Entity entity)) { }

// Query creation
EntityQuery query = MonoHybridAPI.CreateQuery(ComponentType.ReadWrite<MyComponent>());

// Bulk export/import for job processing
using var dataArray = MonoHybridAPI.GetComponentDataArray<MyComponent>(query);
// Process in jobs...
MonoHybridAPI.SetComponentDataArray(query, dataArray); // Write back

// Bulk buffer access (modifications affect entities directly)
var buffers = MonoHybridAPI.GetBufferArray<MyBufferElement>(query);

// Enabled component support
bool enabled = MonoHybridAPI.IsComponentEnabled<MyEnableableComponent>(gameObject);
MonoHybridAPI.SetComponentEnabled<MyEnableableComponent>(entity, false);
```

### MonoBaker<TAuthoring>
Base class for baking authoring MonoBehaviours into Entity components. Provides context properties; all component operations use `MonoHybridAPI`:

```csharp
public class VehicleBaker : MonoBaker<VehicleAuthoring>
{
    public override void Bake(VehicleAuthoring authoring)
    {
        // Add components via MonoHybridAPI using the Entity property
        MonoHybridAPI.AddComponentData(Entity, new VehicleData { MaxSpeed = authoring.maxSpeed });

        // Add buffers with optional capacity
        var buffer = MonoHybridAPI.EnsureBuffer<WheelElement>(Entity, 4);

        // Check if rebaking
        if (IsRebake)
        {
            // Update existing data instead of resetting
            var existing = MonoHybridAPI.GetComponentData<VehicleData>(Entity);
            MonoHybridAPI.SetComponentData(Entity, new VehicleData {
                MaxSpeed = authoring.maxSpeed,
                CurrentSpeed = existing.CurrentSpeed // Preserve runtime state
            });
        }

        // Access Unity components via GameObject property
        var rb = GameObject.GetComponentInParent<Rigidbody>();
    }
}
```

### MonoSystem
Base class for systems that process HybridMono entities. Provides lifecycle hooks; all component operations use `MonoHybridAPI`:

```csharp
public class VehicleSystem : MonoSystem
{
    private EntityQuery _vehicleQuery;

    protected override void OnCreate()
    {
        _vehicleQuery = MonoHybridAPI.CreateQuery(ComponentType.ReadWrite<VehicleData>());
    }

    protected override void OnFixedUpdate()
    {
        // Option 1: Bulk export for job processing
        using var dataArray = MonoHybridAPI.GetComponentDataArray<VehicleData>(_vehicleQuery);
        // Schedule jobs with NativeArrays...
        MonoHybridAPI.SetComponentDataArray(_vehicleQuery, dataArray); // Write back

        // Option 2: Direct access via MonoHybridAPI
        foreach (var go in MonoHybridAPI.GetRegisteredGameObjects())
        {
            if (MonoHybridAPI.HasComponent<VehicleData>(go))
            {
                var data = MonoHybridAPI.GetComponentData<VehicleData>(go);
                data.CurrentSpeed += Time.fixedDeltaTime;
                MonoHybridAPI.SetComponentData(go, data);
            }
        }

        // Option 3: Get buffers for direct modification
        var buffers = MonoHybridAPI.GetBufferArray<WheelElement>(_vehicleQuery);
    }
}
```

### MonoBakingSystem
Static system that automatically discovers and executes bakers:
- Runs at runtime when scenes load
- Discovers all `MonoBaker<T>` implementations via reflection
- Skips GameObjects in SubScenes (handled by standard ECS baking)

### IMonoJob (Legacy Support)
Lightweight job interface for main-thread operations:

```csharp
public struct ApplyForcesJob : IMonoJob
{
    public Rigidbody[] rigidbodies;
    public Vector3[] forces;

    public void Execute(int index)
    {
        rigidbodies[index].AddForce(forces[index]);
    }
}

// Execute via RunMonoJob
RunMonoJob(job, length);
```

## Architecture

```
GameObject with Authoring Component
    ↓ Scene Load
MonoBakingSystem discovers and executes MonoBaker<T>
    ↓
MonoHybridAPI.RegisterGameObject() creates Entity + MonoEntityTracker
    ↓
Baker uses MonoHybridAPI.AddComponentData/EnsureBuffer to add data to Entity
    ↓
MonoSystem uses MonoHybridAPI for component access and bulk export/import
    ↓
GameObject destroyed → MonoEntityTracker.OnDestroy() → Entity destroyed
```

## Key Features

| Feature | Description |
|---------|-------------|
| **Dedicated World** | "HybridMono World" isolates hybrid entities |
| **GameObject-Entity Mapping** | Dictionary-based O(1) lookup |
| **Automatic Cleanup** | MonoEntityTracker handles destruction |
| **Bulk Export/Import** | `GetComponentDataArray`/`SetComponentDataArray` for job processing |
| **Buffer Access** | `GetBufferArray` returns DynamicBuffers for direct modification |
| **Rebake Support** | IsRebake property detects update vs. initial bake |
| **SubScene Detection** | Skips GameObjects in SubScenes |

## Usage Example

```csharp
// 1. Define your IComponentData
public struct HealthData : IComponentData
{
    public float Current;
    public float Max;
}

// 2. Create an authoring component
public class HealthAuthoring : MonoBehaviour
{
    public float maxHealth = 100f;
}

// 3. Create a baker (uses MonoHybridAPI for component operations)
public class HealthBaker : MonoBaker<HealthAuthoring>
{
    public override void Bake(HealthAuthoring auth)
    {
        MonoHybridAPI.AddComponentData(Entity, new HealthData
        {
            Current = auth.maxHealth,
            Max = auth.maxHealth
        });
    }
}

// 4. Create a system (uses MonoHybridAPI for queries and data access)
public class HealthSystem : MonoSystem
{
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _query = MonoHybridAPI.CreateQuery(ComponentType.ReadOnly<HealthData>());
    }

    protected override void OnUpdate()
    {
        // Bulk export for processing
        using var healthData = MonoHybridAPI.GetComponentDataArray<HealthData>(_query);
        // Process...
    }
}

// 5. Access from other scripts
public class HealthUI : MonoBehaviour
{
    void Update()
    {
        if (MonoHybridAPI.TryGetComponentData<HealthData>(gameObject, out var health))
        {
            healthBar.fillAmount = health.Current / health.Max;
        }
    }
}
```

## Requirements

- Unity 2022.3+
- Entities 1.3.8+
- Burst 1.8.18+
- Collections 2.5.3+
