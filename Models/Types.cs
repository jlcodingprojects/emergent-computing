using System;
using System.Collections.Generic;

namespace EmergentComputing.Models
{
    public struct Vector2D
    {
        public double X;
        public double Y;

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public readonly double LengthSquared()
        {
            return X * X + Y * Y;
        }

        public readonly double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public readonly Vector2D Normalized()
        {
            var len = Length();
            return len > 0 ? new Vector2D(X / len, Y / len) : new Vector2D(0, 0);
        }

        public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator *(Vector2D a, double scalar) => new(a.X * scalar, a.Y * scalar);
        public static Vector2D operator /(Vector2D a, double scalar) => new(a.X / scalar, a.Y / scalar);
    }

    public class PhysicsProperties
    {
        public double Mass { get; set; } = 1;
        public double Friction { get; set; } = 0.98;
        public double Elasticity { get; set; } = 0.5;
        public double Drag { get; set; } = 0.99;
        public double Stickiness { get; set; } = 0;
        public double Magnetism { get; set; } = 0;
    }

    public class StateInteraction
    {
        public string TargetState { get; set; } = "";
        public double AttractionForce { get; set; }
        public double AttractionRange { get; set; }
        public bool StickOnContact { get; set; }
        public double StickStrength { get; set; }
    }

    public enum WallInteractionType
    {
        BOUNCE,
        STICK,
        WRAP,
        SLIDE,
        PHASE
    }

    public class WallBehavior
    {
        public WallInteractionType Type { get; set; } = WallInteractionType.BOUNCE;
        public double Bounciness { get; set; } = 0.8;
        public double Friction { get; set; } = 0.95;
        public double Stickiness { get; set; } = 0;
    }

    public class ParticleState
    {
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#FFFFFF";
        public double Radius { get; set; } = 5;
        public List<Behavior> Behaviors { get; set; } = new();
        public PhysicsProperties Physics { get; set; } = new();
        public List<StateInteraction> Interactions { get; set; } = new();
        public WallBehavior WallBehavior { get; set; } = new();
    }

    public enum BehaviorType
    {
        MOVE_RANDOM,
        MOVE_TOWARDS,
        MOVE_AWAY,
        SEEK_RESOURCE,
        EMIT_SIGNAL,
        REPLICATE,
        CHANGE_STATE,
        ATTACH,
        DETACH,
        IDLE
    }

    public class Behavior
    {
        public BehaviorType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum ConditionType
    {
        TIMER,
        NEIGHBOR_COUNT,
        NEIGHBOR_STATE,
        ENERGY_LEVEL,
        SIGNAL_RECEIVED,
        RANDOM_CHANCE,
        DISTANCE_TO_TARGET,
        ALWAYS
    }

    public class TransitionCondition
    {
        public ConditionType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class StateTransition
    {
        public string FromState { get; set; } = "";
        public string ToState { get; set; } = "";
        public TransitionCondition Condition { get; set; } = new();
    }

    public class ParticleConfiguration
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, ParticleState> States { get; set; } = new();
        public List<StateTransition> Transitions { get; set; } = new();
        public string InitialState { get; set; } = "";
        public double MaxSpeed { get; set; } = 5;
        public double SenseRadius { get; set; } = 50;
        public double Energy { get; set; } = 100;
    }

    public enum WallType
    {
        SOLID,
        ONE_WAY,
        STICKY,
        DEADLY
    }

    public class Wall
    {
        public string Id { get; set; } = "";
        public WallType Type { get; set; } = WallType.SOLID;
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Thickness { get; set; } = 10;
        public string? Color { get; set; }
    }

    public class GravitySettings
    {
        public bool Enabled { get; set; }
        public Vector2D Direction { get; set; }
        public double Strength { get; set; } = 1;
    }

    public class SimulationConfiguration
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ParticleConfiguration> ParticleConfigs { get; set; } = new();
        public double WorldWidth { get; set; } = 800;
        public double WorldHeight { get; set; } = 600;
        public int InitialParticleCount { get; set; } = 50;
        public int MaxParticles { get; set; } = 200;
        public int TickRate { get; set; } = 60;
        public bool WrapEdges { get; set; }
        public GravitySettings Gravity { get; set; } = new();
        public List<Wall> Walls { get; set; } = new();
    }

    public class ParticleData
    {
        public string Id { get; set; } = "";
        public string ConfigId { get; set; } = "";
        public Vector2D Position { get; set; }
        public Vector2D Velocity { get; set; }
        public Vector2D Acceleration { get; set; }
        public string CurrentState { get; set; } = "";
        public double StateTimer { get; set; }
        public double Energy { get; set; } = 100;
        public List<string> Signals { get; set; } = new();
        public List<string> AttachedTo { get; set; } = new();
        public string? StuckToWall { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class EmergentMetrics
    {
        public double Clustering { get; set; }
        public double Movement { get; set; }
        public double StateChanges { get; set; }
        public double Diversity { get; set; }
        public double Stability { get; set; }
        public double Complexity { get; set; }
    }

    public class ParticleSnapshot
    {
        public int Tick { get; set; }
        public List<SnapshotParticle> Particles { get; set; } = new();
    }

    public class SnapshotParticle
    {
        public string Id { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public string State { get; set; } = "";
        public Vector2D Velocity { get; set; }
    }

    public class TrialResult
    {
        public string ConfigId { get; set; } = "";
        public string TrialId { get; set; } = "";
        public long Timestamp { get; set; }
        public long Duration { get; set; }
        public int FinalParticleCount { get; set; }
        public Dictionary<string, int> StateDistribution { get; set; } = new();
        public EmergentMetrics EmergentMetrics { get; set; } = new();
        public List<ParticleSnapshot>? RecordedFrames { get; set; }
    }

    public class WallCollision
    {
        public Vector2D Normal { get; set; }
        public double Penetration { get; set; }
    }
}

