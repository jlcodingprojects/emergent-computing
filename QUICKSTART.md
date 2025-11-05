# Quick Start Guide

## Prerequisites

- **Windows 10/11** (WPF is Windows-only)
- **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)

## Installation

1. **Verify .NET installation:**
   ```bash
   dotnet --version
   ```
   Should output: `8.0.x` or higher

2. **Navigate to project directory:**
   ```bash
   cd emergent-computing
   ```

3. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

## Running the Application

### Option 1: Command Line

```bash
dotnet run
```

### Option 2: Visual Studio

1. Open `EmergentComputing.csproj` in Visual Studio 2022
2. Press `F5` or click the green "Start" button

### Option 3: Build and Run Executable

```bash
# Build release version
dotnet build -c Release

# Run the executable
./bin/Release/net8.0-windows/EmergentComputing.exe
```

## First Steps

1. **Launch the application** - The simulator will start with the "Flocking Behavior" preset

2. **Start the simulation** - Click the "Start" button in the control panel

3. **Explore the visualization:**
   - **Pan**: Click and drag with the mouse
   - **Zoom**: Use mouse wheel or +/- keys
   - **Reset Camera**: Press `0`

4. **Try different presets** - Select from the Configuration dropdown:
   - Flocking Behavior (Boids)
   - Segregation Model
   - Predator-Prey
   - Crystal Growth
   - Chaotic System
   - Gravity & Bouncing
   - Magnetic Particles

5. **Adjust settings:**
   - Change simulation speed with the slider
   - Enable "Show Velocity" to see particle movement
   - Enable "Show Sense Radius" to see interaction ranges

## Running Trials

1. **Single Trial:**
   - Set "Trial Duration" (milliseconds)
   - Click "Run Single Trial"
   - View results in the Trials section

2. **Batch Analysis:**
   - Set "Number of Trials"
   - Click "Run Batch Trials"
   - Analyze aggregate metrics

## Common Issues

### Application won't start
**Solution:** Ensure .NET 8.0 SDK is installed:
```bash
dotnet --list-sdks
```

### Poor performance
**Solutions:**
- Reduce particle count in configuration
- Disable visualization options
- Lower simulation speed
- Close other applications

### Missing SkiaSharp
**Solution:** Restore NuGet packages:
```bash
dotnet restore --force
```

## Understanding the UI

### Control Panel (Right Side)

**Configuration Section:**
- Select preset simulations
- Generate random configurations
- View configuration details

**Simulation Controls:**
- Start/Pause/Reset simulation
- Step through one tick at a time
- Adjust simulation speed

**Visualization Options:**
- Toggle velocity vectors
- Toggle sense radius circles

**Statistics:**
- Real-time particle count
- Tick counter
- Emergent metrics (complexity, clustering, etc.)
- State distribution

**Trials:**
- Run single or batch simulations
- Analyze emergent behavior
- Track progress with progress bar

### Visualization Area (Left Side)

**On-Screen Display:**
- Top-left: Simulation statistics
- Top-right: Camera controls help

**World View:**
- Gray grid: Coordinate system
- Colored circles: Particles
- Lines: Walls (if present)
- Particle colors: Current state

## Example Workflows

### Exploring Flocking Behavior

1. Select "Flocking Behavior" from Configuration dropdown
2. Click "Start"
3. Zoom in (+) to see individual particles
4. Enable "Show Velocity" to see movement patterns
5. Watch flocking behavior emerge

### Analyzing Segregation

1. Select "Segregation Model"
2. Click "Start" and watch initial mixing
3. Observe gradual separation into red/blue clusters
4. Run batch trials to measure clustering metric
5. Compare results across multiple runs

### Testing Custom Parameters

1. Click "Random Config" to generate new configuration
2. Start simulation
3. Observe emergent behavior
4. If interesting, note the metrics
5. Generate more random configs to explore parameter space

## Performance Tips

### For Large Particle Counts
- Start with lower tick rate (adjust speed slider)
- Disable visualization options
- Zoom out to reduce rendering load
- Close statistics panel if not needed

### For Smooth Animation
- Keep particle count under 200
- Disable "Show Sense Radius"
- Maintain 60 FPS by adjusting speed

### For Analysis
- Run trials without recording
- Batch process multiple trials
- Export results for external analysis

## Next Steps

- Read `README-CSharp.md` for detailed documentation
- Review `MIGRATION-GUIDE.md` for technical details
- Explore `Configuration/ConfigurationManager.cs` to create custom presets
- Modify `Models/Types.cs` to add new behaviors
- Check `Engine/Particle.cs` to understand particle logic

## Getting Help

Common questions:

**Q: How do I create custom configurations?**
A: Modify `Configuration/ConfigurationManager.cs` and add a new method similar to existing presets.

**Q: Can I export simulation results?**
A: Not currently implemented, but you can add JSON export in `TrialManager.ExportResults()`.

**Q: Why is the world so large (80000x60000)?**
A: This allows particles to spread out and form large-scale patterns. Use zoom to view different scales.

**Q: Can I run this on Mac/Linux?**
A: WPF is Windows-only. Consider porting to Avalonia UI for cross-platform support.

**Q: How do I adjust particle counts?**
A: Modify the `InitialParticleCount` in the configuration or create a custom config.

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `W` / `↑` | Pan up |
| `S` / `↓` | Pan down |
| `A` / `←` | Pan left |
| `D` / `→` | Pan right |
| `+` / `=` | Zoom in |
| `-` / `_` | Zoom out |
| `0` | Reset camera |

## Mouse Controls

| Action | Effect |
|--------|--------|
| Click + Drag | Pan camera |
| Scroll Wheel | Zoom in/out |

## Tips and Tricks

1. **Finding Interesting Patterns:**
   - Start with preset configurations
   - Generate random configs until you find interesting behavior
   - Run batch trials to identify stable patterns

2. **Performance Optimization:**
   - Lower particle counts for faster iteration
   - Disable visualization options during trials
   - Use Step mode for frame-by-frame analysis

3. **Understanding Emergence:**
   - Watch complexity metric over time
   - Compare clustering across different configurations
   - Look for phase transitions in state distributions

4. **Debugging Configurations:**
   - Use Step mode to advance one tick at a time
   - Enable Show Velocity to see force applications
   - Check Statistics panel for state distribution issues

Happy simulating! 🎨✨

