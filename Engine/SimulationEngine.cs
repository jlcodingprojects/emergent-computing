using EmergentComputing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmergentComputing.Engine
{
    public class SimulationEngine
    {
        private List<Particle> _particles = new();
        private SimulationConfiguration _config;
        private int _tickCount;
        private bool _running;
        private bool _recording;
        private List<ParticleSnapshot> _recordedFrames = new();
        private static readonly Random _random = new();
        private SpatialGrid _spatialGrid;

        public SimulationEngine(SimulationConfiguration config)
        {
            _config = config;
            _spatialGrid = new SpatialGrid(config.WorldWidth, config.WorldHeight, 200);
            Initialize();
        }

        private void Initialize()
        {
            _particles.Clear();
            _tickCount = 0;
            _recordedFrames.Clear();

            for (int i = 0; i < _config.InitialParticleCount; i++)
            {
                SpawnParticle();
            }
        }

        private Particle? SpawnParticle(Vector2D? position = null, int? configIndex = null)
        {
            if (_particles.Count >= _config.MaxParticles)
                return null;

            var particleConfig = configIndex.HasValue && configIndex.Value < _config.ParticleConfigs.Count
                ? _config.ParticleConfigs[configIndex.Value]
                : _config.ParticleConfigs[_random.Next(_config.ParticleConfigs.Count)];

            if (particleConfig == null) return null;

            var pos = position ?? new Vector2D(
                _random.NextDouble() * _config.WorldWidth,
                _random.NextDouble() * _config.WorldHeight
            );

            var particle = new Particle(
                $"particle_{DateTime.Now.Ticks}_{_random.Next()}",
                particleConfig,
                pos
            );

            _particles.Add(particle);
            return particle;
        }

        public void Tick(double dt = 1)
        {
            if (!_running) return;

            var gravity = _config.Gravity.Enabled
                ? new Vector2D(
                    _config.Gravity.Direction.X * _config.Gravity.Strength,
                    _config.Gravity.Direction.Y * _config.Gravity.Strength
                )
                : new Vector2D(0, 0);

            // Build spatial grid
            _spatialGrid.Clear();
            foreach (var particle in _particles)
            {
                _spatialGrid.Insert(particle);
            }

            // Update particles using spatial grid for neighbor queries
            foreach (var particle in _particles)
            {
                var neighbors = _spatialGrid.GetNearby(particle, particle.GetConfig().SenseRadius);
                particle.Update(dt, neighbors, (_config.WorldWidth, _config.WorldHeight), _config.Walls, gravity);
            }

            // Handle edge wrapping or boundaries
            foreach (var particle in _particles)
            {
                var data = particle.GetData();
                var currentState = particle.GetCurrentState();

                if (_config.WrapEdges || (currentState?.WallBehavior.Type == WallInteractionType.WRAP))
                {
                    var pos = data.Position;
                    if (pos.X < 0) pos.X += _config.WorldWidth;
                    if (pos.X > _config.WorldWidth) pos.X -= _config.WorldWidth;
                    if (pos.Y < 0) pos.Y += _config.WorldHeight;
                    if (pos.Y > _config.WorldHeight) pos.Y -= _config.WorldHeight;
                    data.Position = pos;
                }
                else if (_config.Walls.Count == 0)
                {
                    var pos = data.Position;
                    var vel = data.Velocity;
                    
                    if (pos.X < 0 || pos.X > _config.WorldWidth)
                    {
                        vel.X *= -0.8;
                        pos.X = Math.Max(0, Math.Min(_config.WorldWidth, pos.X));
                    }
                    if (pos.Y < 0 || pos.Y > _config.WorldHeight)
                    {
                        vel.Y *= -0.8;
                        pos.Y = Math.Max(0, Math.Min(_config.WorldHeight, pos.Y));
                    }
                    
                    data.Position = pos;
                    data.Velocity = vel;
                }
            }

            // Remove particles marked for removal
            _particles = _particles.Where(p => p.GetData().Energy > 0).ToList();

            _tickCount++;

            if (_recording && _tickCount % 5 == 0)
            {
                RecordFrame();
            }
        }

        // Removed - now using SpatialGrid.GetNearby() instead

        private void RecordFrame()
        {
            var snapshot = new ParticleSnapshot
            {
                Tick = _tickCount,
                Particles = _particles.Select(p =>
                {
                    var data = p.GetData();
                    return new SnapshotParticle
                    {
                        Id = data.Id,
                        X = data.Position.X,
                        Y = data.Position.Y,
                        State = data.CurrentState,
                        Velocity = data.Velocity
                    };
                }).ToList()
            };
            _recordedFrames.Add(snapshot);
        }

        public void Start() => _running = true;
        public void Pause() => _running = false;
        
        public void Reset()
        {
            Initialize();
            _running = false;
        }

        public void StartRecording()
        {
            _recording = true;
            _recordedFrames.Clear();
        }

        public void StopRecording() => _recording = false;

        public List<ParticleSnapshot> GetRecordedFrames() => _recordedFrames;
        public List<Particle> GetParticles() => _particles;
        public int GetTickCount() => _tickCount;
        public bool IsRunning() => _running;

        public Dictionary<string, int> GetStateDistribution()
        {
            var distribution = new Dictionary<string, int>();
            foreach (var particle in _particles)
            {
                var state = particle.GetData().CurrentState;
                distribution[state] = distribution.ContainsKey(state) ? distribution[state] + 1 : 1;
            }
            return distribution;
        }

        public EmergentMetrics CalculateEmergentMetrics()
        {
            if (_particles.Count == 0)
            {
                return new EmergentMetrics
                {
                    Clustering = 0,
                    Movement = 0,
                    StateChanges = 0,
                    Diversity = 0,
                    Stability = 0,
                    Complexity = 0
                };
            }

            var clustering = CalculateClustering();
            var movement = CalculateAverageMovement();
            var diversity = CalculateDiversity();

            var stateChanges = _particles.Count(p => p.GetData().StateTimer < 10);
            var stability = 1 - (double)stateChanges / _particles.Count;

            var complexity = (diversity + clustering) / 2;

            return new EmergentMetrics
            {
                Clustering = clustering,
                Movement = movement,
                StateChanges = stateChanges,
                Diversity = diversity,
                Stability = stability,
                Complexity = complexity
            };
        }

        private double CalculateClustering()
        {
            if (_particles.Count < 2) return 0;

            // Rebuild spatial grid for clustering calculation
            _spatialGrid.Clear();
            foreach (var particle in _particles)
            {
                _spatialGrid.Insert(particle);
            }

            double totalClustering = 0;
            foreach (var particle in _particles)
            {
                var neighbors = _spatialGrid.GetNearby(particle, particle.GetConfig().SenseRadius);
                if (neighbors.Count < 2) continue;

                int connections = 0;
                var senseRadius = particle.GetConfig().SenseRadius;
                var senseRadiusSquared = senseRadius * senseRadius;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    var pos1 = neighbors[i].GetData().Position;
                    for (int j = i + 1; j < neighbors.Count; j++)
                    {
                        var pos2 = neighbors[j].GetData().Position;
                        var dx = pos1.X - pos2.X;
                        var dy = pos1.Y - pos2.Y;
                        var distSquared = dx * dx + dy * dy;
                        
                        if (distSquared <= senseRadiusSquared)
                        {
                            connections++;
                        }
                    }
                }

                var maxConnections = (neighbors.Count * (neighbors.Count - 1)) / 2.0;
                totalClustering += maxConnections > 0 ? connections / maxConnections : 0;
            }

            return totalClustering / _particles.Count;
        }

        private double CalculateAverageMovement()
        {
            var totalSpeed = _particles.Sum(p => p.GetData().Velocity.Length());
            return totalSpeed / _particles.Count;
        }

        private double CalculateDiversity()
        {
            var distribution = GetStateDistribution();
            var total = _particles.Count;
            double entropy = 0;

            foreach (var count in distribution.Values)
            {
                var p = (double)count / total;
                if (p > 0)
                {
                    entropy -= p * Math.Log2(p);
                }
            }

            var numStates = distribution.Count;
            var maxEntropy = numStates > 1 ? Math.Log2(numStates) : 1;
            return maxEntropy > 0 ? entropy / maxEntropy : 0;
        }

        public SimulationConfiguration GetConfig() => _config;
        public List<Wall> GetWalls() => _config.Walls;

        public void UpdateConfig(SimulationConfiguration config)
        {
            _config = config;
            _spatialGrid = new SpatialGrid(config.WorldWidth, config.WorldHeight, 200);
            Reset();
        }
    }
}

