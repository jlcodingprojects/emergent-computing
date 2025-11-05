# Emergent Computing Simulator - C# WPF Edition

A complete rewrite of the emergent computing particle simulator in C# using WPF and SkiaSharp for high-performance visualization.

## Features

- **Particle System**: Complex particle behaviors with physics, state transitions, and emergent patterns
- **WPF Interface**: Modern desktop UI with full simulation controls
- **SkiaSharp Rendering**: Hardware-accelerated 2D graphics for smooth visualization
- **Camera Controls**: Pan, zoom, and navigate through large simulation worlds
- **Preset Configurations**: 7 built-in simulation presets including:
  - Flocking Behavior (Boids)
  - Segregation Model (Schelling)
  - Predator-Prey Dynamics
  - Crystal Growth
  - Chaotic Systems
  - Gravity & Bouncing
  - Magnetic Particles
- **Trial System**: Run batch simulations and analyze emergent metrics
- **Real-time Metrics**: Track complexity, clustering, diversity, and stability

## Requirements

- .NET 8.0 SDK or later
- Windows 10/11

## Building and Running

### Using .NET CLI

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Using Visual Studio

1. Open `EmergentComputing.csproj` in Visual Studio 2022 or later
2. Press F5 to build and run

## Project Structure

```
EmergentComputing/
├── Models/
│   └── Types.cs                 # Core data types and structures
├── Engine/
│   ├── Particle.cs              # Particle physics and behaviors
│   └── SimulationEngine.cs      # Main simulation loop
├── Rendering/
│   └── SimulationRenderer.cs    # SkiaSharp rendering
├── Configuration/
│   └── ConfigurationManager.cs  # Preset configurations
├── UI/
│   └── TrialManager.cs          # Batch simulation trials
├── App.xaml / App.xaml.cs       # Application entry point
├── MainWindow.xaml              # Main window UI
└── MainWindow.xaml.cs           # Main window logic
```

## Controls

### Simulation Controls
- **Start**: Begin the simulation
- **Pause**: Pause the simulation
- **Reset**: Reset to initial state
- **Step**: Advance one simulation tick

### Camera Controls
- **Mouse Drag**: Pan the camera
- **Mouse Wheel**: Zoom in/out
- **WASD / Arrow Keys**: Pan the camera
- **+/-**: Zoom in/out
- **0**: Reset camera to center

### Visualization Options
- **Show Velocity**: Display velocity vectors
- **Show Sense Radius**: Display particle sensing range

### Trial System
- **Run Single Trial**: Execute one complete simulation run
- **Run Batch Trials**: Execute multiple trials and analyze results
- **Clear Trials**: Clear all trial data

## Simulation Presets

### 1. Flocking Behavior
Classic Boids algorithm demonstrating emergent flocking patterns through simple rules:
- Cohesion (move toward neighbors)
- Separation (avoid crowding)
- Alignment (match velocity)

### 2. Segregation Model
Schelling's segregation model showing how slight preferences lead to large-scale clustering.

### 3. Predator-Prey
Emergent chase and escape behaviors with state transitions based on proximity.

### 4. Crystal Growth
Particles form crystalline structures through attractive forces and state transitions.

### 5. Chaotic System
Random state transitions create unpredictable emergent patterns.

### 6. Gravity & Bouncing
Physical simulation with gravity, platforms, and elastic collisions.

### 7. Magnetic Particles
Positive and negative charges with long-range attraction and repulsion forces.

## Emergent Metrics

The simulation tracks several metrics to measure emergent behavior:

- **Complexity**: Overall system complexity (combination of diversity and clustering)
- **Clustering**: How tightly particles group together
- **Movement**: Average particle velocity
- **Diversity**: Shannon entropy of state distribution
- **Stability**: How long particles remain in their current state
- **State Changes**: Number of recent state transitions

## Performance

The simulation is optimized for real-time performance:
- SkiaSharp hardware acceleration
- Spatial optimization for neighbor queries
- Adaptive grid rendering based on zoom level
- Efficient state management

Typical performance:
- 100+ particles at 60 FPS
- World sizes up to 80,000 x 60,000 units
- Sub-millisecond tick times

## Extending the Simulator

### Creating Custom Configurations

You can create custom particle configurations by modifying `ConfigurationManager.cs` or using the "Random Config" button to generate new configurations programmatically.

### Adding Behaviors

To add new particle behaviors:
1. Add a new `BehaviorType` enum value in `Models/Types.cs`
2. Implement the behavior in `Engine/Particle.cs` `ExecuteBehavior` method
3. Create configurations that use the new behavior

### Custom Metrics

Add custom emergent metrics by extending the `CalculateEmergentMetrics` method in `SimulationEngine.cs`.

## License

This is a demonstration project created for educational purposes.

## Architecture Notes

### Differences from TypeScript Version

- **Type Safety**: Strong typing throughout with C# generics
- **Performance**: Compiled code runs significantly faster
- **Memory Management**: Automatic GC instead of JavaScript engine
- **UI Framework**: WPF instead of HTML/Canvas
- **Rendering**: SkiaSharp instead of Canvas2D API
- **Threading**: Built-in support for async operations

### Key Design Decisions

1. **Separation of Concerns**: Engine, rendering, and UI are fully decoupled
2. **Data-Oriented**: Particle data stored separately from behavior logic
3. **Configuration-Driven**: All simulations defined through configuration objects
4. **Event-Based UI**: UI updates driven by events and timers
5. **Vector Math**: Custom Vector2D struct for performance

## Troubleshooting

### Application won't start
- Ensure .NET 8.0 SDK is installed
- Check that SkiaSharp packages were restored correctly

### Poor performance
- Reduce particle count in configuration
- Disable "Show Sense Radius" visualization
- Lower the simulation speed

### Visual artifacts
- Update graphics drivers
- Disable hardware acceleration if issues persist

## Future Enhancements

Possible improvements:
- Multi-threading for large particle counts
- GPU acceleration via compute shaders
- Network multiplayer simulations
- Configuration file import/export
- Recording and playback of simulations
- Machine learning for configuration optimization

