using EmergentComputing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmergentComputing.Engine
{
    public class Particle
    {
        private ParticleData _data;
        private readonly ParticleConfiguration _config;
        private static readonly Random _random = new();

        public Particle(string id, ParticleConfiguration config, Vector2D position, string? initialState = null)
        {
            _config = config;
            _data = new ParticleData
            {
                Id = id,
                ConfigId = config.Id,
                Position = position,
                Velocity = new Vector2D(0, 0),
                Acceleration = new Vector2D(0, 0),
                CurrentState = initialState ?? config.InitialState,
                StateTimer = 0,
                Energy = config.Energy,
                Signals = new List<string>(),
                AttachedTo = new List<string>(),
                StuckToWall = null,
                Metadata = new Dictionary<string, object>()
            };
        }

        public ParticleData GetData() => _data;
        public ParticleConfiguration GetConfig() => _config;
        
        public ParticleState? GetCurrentState()
        {
            return _config.States.TryGetValue(_data.CurrentState, out var state) ? state : null;
        }

        public void Update(double dt, List<Particle> neighbors, (double width, double height) worldBounds, List<Wall> walls, Vector2D gravity)
        {
            _data.StateTimer += dt;

            // Skip physics if stuck to wall
            if (_data.StuckToWall != null)
            {
                if (_random.NextDouble() < 0.001)
                {
                    _data.StuckToWall = null;
                }
                else
                {
                    _data.Signals.Clear();
                    return;
                }
            }

            // Reset acceleration
            _data.Acceleration = new Vector2D(0, 0);

            // Check for state transitions
            CheckTransitions(neighbors);

            var currentState = GetCurrentState();
            if (currentState == null) return;

            // Apply gravity
            if (gravity.X != 0 || gravity.Y != 0)
            {
                var mass = currentState.Physics.Mass;
                _data.Acceleration = new Vector2D(
                    _data.Acceleration.X + gravity.X * mass,
                    _data.Acceleration.Y + gravity.Y * mass
                );
            }

            // Apply state-based interactions
            ApplyStateInteractions(neighbors, currentState);

            // Execute behaviors
            foreach (var behavior in currentState.Behaviors)
            {
                ExecuteBehavior(behavior, neighbors, worldBounds);
            }

            // Apply physics properties
            var physics = currentState.Physics;

            // Apply drag
            _data.Velocity = new Vector2D(
                _data.Velocity.X * physics.Drag,
                _data.Velocity.Y * physics.Drag
            );

            // Apply base friction
            _data.Velocity = new Vector2D(
                _data.Velocity.X * physics.Friction,
                _data.Velocity.Y * physics.Friction
            );

            // Update velocity from acceleration
            _data.Velocity = new Vector2D(
                _data.Velocity.X + _data.Acceleration.X * dt,
                _data.Velocity.Y + _data.Acceleration.Y * dt
            );

            // Limit speed
            var speed = _data.Velocity.Length();
            if (speed > _config.MaxSpeed)
            {
                _data.Velocity = _data.Velocity.Normalized() * _config.MaxSpeed;
            }

            // Update position
            _data.Position = new Vector2D(
                _data.Position.X + _data.Velocity.X * dt,
                _data.Position.Y + _data.Velocity.Y * dt
            );

            // Handle wall collisions
            HandleWallCollisions(walls, currentState);

            // Clear signals
            _data.Signals.Clear();
        }

        private void CheckTransitions(List<Particle> neighbors)
        {
            foreach (var transition in _config.Transitions)
            {
                if (transition.FromState == _data.CurrentState)
                {
                    if (EvaluateCondition(transition.Condition, neighbors))
                    {
                        ChangeState(transition.ToState);
                        break;
                    }
                }
            }
        }

        private bool EvaluateCondition(TransitionCondition condition, List<Particle> neighbors)
        {
            switch (condition.Type)
            {
                case ConditionType.TIMER:
                    var duration = GetParam<double>(condition.Parameters, "duration", 1000);
                    return _data.StateTimer >= duration;

                case ConditionType.NEIGHBOR_COUNT:
                    var count = neighbors.Count;
                    var min = GetParam<int>(condition.Parameters, "min", 0);
                    var max = GetParam<int>(condition.Parameters, "max", int.MaxValue);
                    return count >= min && count <= max;

                case ConditionType.NEIGHBOR_STATE:
                    var targetState = GetParam<string>(condition.Parameters, "state", "");
                    var requiredCount = GetParam<int>(condition.Parameters, "count", 1);
                    var matchingNeighbors = neighbors.Count(n => n.GetData().CurrentState == targetState);
                    return matchingNeighbors >= requiredCount;

                case ConditionType.ENERGY_LEVEL:
                    var threshold = GetParam<double>(condition.Parameters, "threshold", 50);
                    var op = GetParam<string>(condition.Parameters, "operator", "less");
                    return op == "less" ? _data.Energy < threshold : _data.Energy > threshold;

                case ConditionType.SIGNAL_RECEIVED:
                    var signal = GetParam<string>(condition.Parameters, "signal", "");
                    return _data.Signals.Contains(signal);

                case ConditionType.RANDOM_CHANCE:
                    var probability = GetParam<double>(condition.Parameters, "probability", 0.01);
                    return _random.NextDouble() < probability;

                case ConditionType.ALWAYS:
                    return true;

                default:
                    return false;
            }
        }

        private void ExecuteBehavior(Behavior behavior, List<Particle> neighbors, (double width, double height) worldBounds)
        {
            switch (behavior.Type)
            {
                case BehaviorType.MOVE_RANDOM:
                    MoveRandom(behavior.Parameters);
                    break;
                case BehaviorType.MOVE_TOWARDS:
                    MoveTowards(neighbors, behavior.Parameters);
                    break;
                case BehaviorType.MOVE_AWAY:
                    MoveAway(neighbors, behavior.Parameters);
                    break;
                case BehaviorType.SEEK_RESOURCE:
                    SeekResource(neighbors, behavior.Parameters);
                    break;
                case BehaviorType.EMIT_SIGNAL:
                    EmitSignal(neighbors, behavior.Parameters);
                    break;
                case BehaviorType.IDLE:
                    ApplyFriction(behavior.Parameters);
                    break;
            }
        }

        private void MoveRandom(Dictionary<string, object> parameters)
        {
            var strength = GetParam<double>(parameters, "strength", 1.0);
            _data.Velocity = new Vector2D(
                _data.Velocity.X + (_random.NextDouble() - 0.5) * strength,
                _data.Velocity.Y + (_random.NextDouble() - 0.5) * strength
            );
        }

        private void MoveTowards(List<Particle> neighbors, Dictionary<string, object> parameters)
        {
            var targetState = GetParam<string>(parameters, "targetState", "");
            var strength = GetParam<double>(parameters, "strength", 1.0);

            var targets = string.IsNullOrEmpty(targetState)
                ? neighbors
                : neighbors.Where(n => n.GetData().CurrentState == targetState).ToList();

            if (targets.Count == 0) return;

            var avgX = targets.Average(n => n.GetData().Position.X);
            var avgY = targets.Average(n => n.GetData().Position.Y);

            var dx = avgX - _data.Position.X;
            var dy = avgY - _data.Position.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist > 0)
            {
                _data.Velocity = new Vector2D(
                    _data.Velocity.X + (dx / dist) * strength,
                    _data.Velocity.Y + (dy / dist) * strength
                );
            }
        }

        private void MoveAway(List<Particle> neighbors, Dictionary<string, object> parameters)
        {
            var targetState = GetParam<string>(parameters, "targetState", "");
            var strength = GetParam<double>(parameters, "strength", 1.0);

            var targets = string.IsNullOrEmpty(targetState)
                ? neighbors
                : neighbors.Where(n => n.GetData().CurrentState == targetState).ToList();

            if (targets.Count == 0) return;

            foreach (var neighbor in targets)
            {
                var neighborData = neighbor.GetData();
                var dx = _data.Position.X - neighborData.Position.X;
                var dy = _data.Position.Y - neighborData.Position.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist > 0 && dist < _config.SenseRadius)
                {
                    var force = strength / (dist * dist);
                    _data.Velocity = new Vector2D(
                        _data.Velocity.X + (dx / dist) * force,
                        _data.Velocity.Y + (dy / dist) * force
                    );
                }
            }
        }

        private void SeekResource(List<Particle> neighbors, Dictionary<string, object> parameters)
        {
            if (neighbors.Count == 0) return;

            var strength = GetParam<double>(parameters, "strength", 1.0);
            Particle? nearestParticle = null;
            var minDist = double.MaxValue;

            foreach (var neighbor in neighbors)
            {
                var neighborData = neighbor.GetData();
                if (neighborData.CurrentState != _data.CurrentState)
                {
                    var dx = neighborData.Position.X - _data.Position.X;
                    var dy = neighborData.Position.Y - _data.Position.Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestParticle = neighbor;
                    }
                }
            }

            if (nearestParticle != null)
            {
                var nearestData = nearestParticle.GetData();
                var dx = nearestData.Position.X - _data.Position.X;
                var dy = nearestData.Position.Y - _data.Position.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 0)
                {
                    _data.Velocity = new Vector2D(
                        _data.Velocity.X + (dx / dist) * strength,
                        _data.Velocity.Y + (dy / dist) * strength
                    );
                }
            }
        }

        private void EmitSignal(List<Particle> neighbors, Dictionary<string, object> parameters)
        {
            var signal = GetParam<string>(parameters, "signal", "default");
            var range = GetParam<double>(parameters, "range", _config.SenseRadius);

            foreach (var neighbor in neighbors)
            {
                var neighborData = neighbor.GetData();
                var dx = neighborData.Position.X - _data.Position.X;
                var dy = neighborData.Position.Y - _data.Position.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist <= range)
                {
                    neighbor.ReceiveSignal(signal);
                }
            }
        }

        private void ApplyFriction(Dictionary<string, object> parameters)
        {
            var friction = GetParam<double>(parameters, "friction", 0.95);
            _data.Velocity = new Vector2D(
                _data.Velocity.X * friction,
                _data.Velocity.Y * friction
            );
        }

        private void ChangeState(string newState)
        {
            if (_config.States.ContainsKey(newState))
            {
                _data.CurrentState = newState;
                _data.StateTimer = 0;
            }
        }

        public void ReceiveSignal(string signal)
        {
            _data.Signals.Add(signal);
        }

        public double DistanceTo(Particle other)
        {
            var dx = _data.Position.X - other._data.Position.X;
            var dy = _data.Position.Y - other._data.Position.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void ApplyStateInteractions(List<Particle> neighbors, ParticleState currentState)
        {
            if (currentState.Interactions.Count == 0) return;

            foreach (var neighbor in neighbors)
            {
                var neighborData = neighbor.GetData();
                var neighborState = neighbor.GetCurrentState();
                if (neighborState == null) continue;

                var interaction = currentState.Interactions.FirstOrDefault(i => i.TargetState == neighborData.CurrentState);
                if (interaction == null) continue;

                var dx = neighborData.Position.X - _data.Position.X;
                var dy = neighborData.Position.Y - _data.Position.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist == 0) continue;

                // Apply attraction/repulsion
                if (dist <= interaction.AttractionRange && interaction.AttractionForce != 0)
                {
                    var forceMagnitude = interaction.AttractionForce / (dist * dist + 1);
                    _data.Acceleration = new Vector2D(
                        _data.Acceleration.X + (dx / dist) * forceMagnitude,
                        _data.Acceleration.Y + (dy / dist) * forceMagnitude
                    );
                }

                // Check for sticking
                var combinedRadius = currentState.Radius + neighborState.Radius;
                if (interaction.StickOnContact && dist <= combinedRadius)
                {
                    if (_random.NextDouble() < interaction.StickStrength)
                    {
                        _data.Velocity = new Vector2D(_data.Velocity.X * 0.1, _data.Velocity.Y * 0.1);
                        if (!_data.AttachedTo.Contains(neighborData.Id))
                        {
                            _data.AttachedTo.Add(neighborData.Id);
                        }
                    }
                }
                else
                {
                    _data.AttachedTo.Remove(neighborData.Id);
                }
            }
        }

        private void HandleWallCollisions(List<Wall> walls, ParticleState currentState)
        {
            var wallBehavior = currentState.WallBehavior;

            foreach (var wall in walls)
            {
                var collision = CheckWallCollision(wall, currentState.Radius);
                if (collision == null) continue;

                switch (wallBehavior.Type)
                {
                    case WallInteractionType.BOUNCE:
                        HandleWallBounce(collision, wallBehavior.Bounciness);
                        break;
                    case WallInteractionType.STICK:
                        if (_random.NextDouble() < wallBehavior.Stickiness)
                        {
                            _data.StuckToWall = wall.Id;
                            _data.Velocity = new Vector2D(0, 0);
                        }
                        else
                        {
                            HandleWallBounce(collision, wallBehavior.Bounciness);
                        }
                        break;
                    case WallInteractionType.SLIDE:
                        HandleWallSlide(collision, wallBehavior.Friction);
                        break;
                    case WallInteractionType.WRAP:
                    case WallInteractionType.PHASE:
                        break;
                }

                if (wall.Type == WallType.DEADLY)
                {
                    _data.Energy = 0;
                }
            }
        }

        private WallCollision? CheckWallCollision(Wall wall, double radius)
        {
            var wallDx = wall.X2 - wall.X1;
            var wallDy = wall.Y2 - wall.Y1;
            var wallLength = Math.Sqrt(wallDx * wallDx + wallDy * wallDy);

            if (wallLength == 0) return null;

            var wallNormalX = -wallDy / wallLength;
            var wallNormalY = wallDx / wallLength;

            var particleDx = _data.Position.X - wall.X1;
            var particleDy = _data.Position.Y - wall.Y1;

            var projection = (particleDx * wallDx + particleDy * wallDy) / (wallLength * wallLength);

            if (projection < 0 || projection > 1) return null;

            var distanceToWall = Math.Abs(particleDx * wallNormalX + particleDy * wallNormalY);
            var effectiveRadius = radius + wall.Thickness / 2;

            if (distanceToWall < effectiveRadius)
            {
                return new WallCollision
                {
                    Normal = new Vector2D(wallNormalX, wallNormalY),
                    Penetration = effectiveRadius - distanceToWall
                };
            }

            return null;
        }

        private void HandleWallBounce(WallCollision collision, double bounciness)
        {
            _data.Position = new Vector2D(
                _data.Position.X + collision.Normal.X * collision.Penetration,
                _data.Position.Y + collision.Normal.Y * collision.Penetration
            );

            var dotProduct = _data.Velocity.X * collision.Normal.X + _data.Velocity.Y * collision.Normal.Y;
            _data.Velocity = new Vector2D(
                _data.Velocity.X - 2 * dotProduct * collision.Normal.X * bounciness,
                _data.Velocity.Y - 2 * dotProduct * collision.Normal.Y * bounciness
            );
        }

        private void HandleWallSlide(WallCollision collision, double friction)
        {
            _data.Position = new Vector2D(
                _data.Position.X + collision.Normal.X * collision.Penetration,
                _data.Position.Y + collision.Normal.Y * collision.Penetration
            );

            var dotProduct = _data.Velocity.X * collision.Normal.X + _data.Velocity.Y * collision.Normal.Y;
            if (dotProduct < 0)
            {
                _data.Velocity = new Vector2D(
                    _data.Velocity.X - dotProduct * collision.Normal.X,
                    _data.Velocity.Y - dotProduct * collision.Normal.Y
                );
            }

            _data.Velocity = new Vector2D(
                _data.Velocity.X * friction,
                _data.Velocity.Y * friction
            );
        }

        private T GetParam<T>(Dictionary<string, object> parameters, string key, T defaultValue)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}

