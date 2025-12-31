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
Central API for the HybridMono system:

```csharp
// Access the dedicated World
World world = MonoHybridAPI.World;
EntityManager em = MonoHybridAPI.EntityManager;

// Component access via GameObject
var data = MonoHybridAPI.GetComponentData<MyComponent>(gameObject);
MonoHybridAPI.SetComponentData(gameObject, newData);

// Buffer access via GameObject
var buffer = MonoHybridAPI.GetBuffer<MyBufferElement>(gameObject);

// Entity lookup
if (MonoHybridAPI.TryGetEntity(gameObject, out Entity entity)) { }

// Query creation
EntityQuery query = MonoHybridAPI.CreateQuery(ComponentType.ReadWrite<MyComponent>());
```

### MonoBaker<TAuthoring>
Base class for baking authoring MonoBehaviours into Entity components:

```csharp
public class VehicleBaker : MonoBaker<VehicleAuthoring>
{
    public override void Bake(VehicleAuthoring authoring)
    {
        // Add components directly to the Entity
        AddComponent(new VehicleData { MaxSpeed = authoring.maxSpeed });

        // Add buffers with optional capacity
        var buffer = AddBuffer<WheelElement>(4);

        // Check if rebaking
        if (IsRebake)
        {
            // Update existing data instead of resetting
        }
    }
}
```

### MonoSystem
Base class for systems that process HybridMono entities:

```csharp
public class VehicleSystem : MonoSystem
{
    private EntityQuery _vehicleQuery;

    protected override void OnCreate()
    {
        _vehicleQuery = CreateQuery(ComponentType.ReadWrite<VehicleData>());
    }

    protected override void OnFixedUpdate()
    {
        // Schedule jobs for parallel execution
        ScheduleParallel(new UpdateVehicleJob
        {
            DeltaTime = Time.fixedDeltaTime
        }, _vehicleQuery);
    }

    [BurstCompile]
    partial struct UpdateVehicleJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref VehicleData vehicle)
        {
            vehicle.CurrentSpeed += DeltaTime;
        }
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
Baker.AddComponent/AddBuffer adds data to Entity
    ↓
MonoSystem processes Entities with IJobEntity
    ↓
GameObject destroyed → MonoEntityTracker.OnDestroy() → Entity destroyed
```

## Key Features

| Feature | Description |
|---------|-------------|
| **Dedicated World** | "HybridMono World" isolates hybrid entities |
| **GameObject-Entity Mapping** | Dictionary-based O(1) lookup |
| **Automatic Cleanup** | MonoEntityTracker handles destruction |
| **IJobEntity Support** | Schedule/ScheduleParallel for parallel processing |
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

// 3. Create a baker
public class HealthBaker : MonoBaker<HealthAuthoring>
{
    public override void Bake(HealthAuthoring auth)
    {
        AddComponent(new HealthData
        {
            Current = auth.maxHealth,
            Max = auth.maxHealth
        });
    }
}

// 4. Create a system
public class HealthSystem : MonoSystem
{
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _query = CreateQuery(ComponentType.ReadOnly<HealthData>());
    }

    protected override void OnUpdate()
    {
        // Process all entities with HealthData
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
