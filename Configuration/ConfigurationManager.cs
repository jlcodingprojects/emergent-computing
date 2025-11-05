using EmergentComputing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmergentComputing.Configuration
{
    public class ConfigurationManager
    {
        private readonly Dictionary<string, SimulationConfiguration> _configurations = new();
        private readonly List<SimulationConfiguration> _presets;
        private static readonly Random _random = new();

        public ConfigurationManager()
        {
            _presets = CreatePresets();
            foreach (var preset in _presets)
            {
                _configurations[preset.Id] = preset;
            }
        }

        private List<SimulationConfiguration> CreatePresets()
        {
            return new List<SimulationConfiguration>
            {
                CreateFlockingConfig(),
                CreateSegregationConfig(),
                CreatePredatorPreyConfig(),
                CreateCrystalGrowthConfig(),
                CreateChaoticConfig(),
                CreateGravityBouncingConfig(),
                CreateMagneticConfig()
            };
        }

        private PhysicsProperties DefaultPhysics() => new()
        {
            Mass = 1,
            Friction = 0.98,
            Elasticity = 0.5,
            Drag = 0.99,
            Stickiness = 0,
            Magnetism = 0
        };

        private WallBehavior DefaultWallBehavior() => new()
        {
            Type = WallInteractionType.BOUNCE,
            Bounciness = 0.8,
            Friction = 0.95,
            Stickiness = 0
        };

        private SimulationConfiguration CreateFlockingConfig()
        {
            var boid = new ParticleConfiguration
            {
                Id = "boid",
                Name = "Boid",
                Description = "Flocking behavior particle",
                InitialState = "moving",
                MaxSpeed = 3,
                SenseRadius = 50,
                States = new Dictionary<string, ParticleState>
                {
                    ["moving"] = new ParticleState
                    {
                        Name = "moving",
                        Color = "#00aaff",
                        Radius = 5,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["strength"] = 0.5 } },
                            new() { Type = BehaviorType.MOVE_AWAY, Parameters = new() { ["strength"] = 2.0 } },
                            new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 0.1 } }
                        },
                        Physics = new PhysicsProperties { Mass = 1, Friction = 0.98, Elasticity = 0.5, Drag = 0.97, Stickiness = 0, Magnetism = 0 },
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    }
                },
                Transitions = new List<StateTransition>()
            };

            return new SimulationConfiguration
            {
                Id = "flocking",
                Name = "Flocking Behavior",
                Description = "Classic boid flocking simulation",
                ParticleConfigs = new List<ParticleConfiguration> { boid },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 5000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = true,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        private SimulationConfiguration CreateSegregationConfig()
        {
            var red = new ParticleConfiguration
            {
                Id = "red",
                Name = "Red",
                Description = "Red particle seeking similar",
                InitialState = "seeking",
                MaxSpeed = 2,
                SenseRadius = 60,
                States = new Dictionary<string, ParticleState>
                {
                    ["seeking"] = new ParticleState
                    {
                        Name = "seeking",
                        Color = "#ff4444",
                        Radius = 6,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "seeking", ["strength"] = 1.0 } },
                            new() { Type = BehaviorType.MOVE_AWAY, Parameters = new() { ["targetState"] = "wandering", ["strength"] = 1.5 } }
                        },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "seeking", AttractionForce = 2, AttractionRange = 60, StickOnContact = false, StickStrength = 0 },
                            new() { TargetState = "wandering", AttractionForce = -3, AttractionRange = 60, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    },
                    ["satisfied"] = new ParticleState
                    {
                        Name = "satisfied",
                        Color = "#ff8888",
                        Radius = 6,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.IDLE, Parameters = new() { ["friction"] = 0.9 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = DefaultWallBehavior()
                    }
                },
                Transitions = new List<StateTransition>
                {
                    new() { FromState = "seeking", ToState = "satisfied", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_STATE, Parameters = new() { ["state"] = "seeking", ["count"] = 3 } } },
                    new() { FromState = "satisfied", ToState = "seeking", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_COUNT, Parameters = new() { ["min"] = 0, ["max"] = 2 } } }
                }
            };

            var blue = new ParticleConfiguration
            {
                Id = "blue",
                Name = "Blue",
                Description = "Blue particle seeking similar",
                InitialState = "wandering",
                MaxSpeed = 2,
                SenseRadius = 60,
                States = new Dictionary<string, ParticleState>
                {
                    ["wandering"] = new ParticleState
                    {
                        Name = "wandering",
                        Color = "#4444ff",
                        Radius = 6,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "wandering", ["strength"] = 1.0 } },
                            new() { Type = BehaviorType.MOVE_AWAY, Parameters = new() { ["targetState"] = "seeking", ["strength"] = 1.5 } }
                        },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "wandering", AttractionForce = 2, AttractionRange = 60, StickOnContact = false, StickStrength = 0 },
                            new() { TargetState = "seeking", AttractionForce = -3, AttractionRange = 60, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    },
                    ["content"] = new ParticleState
                    {
                        Name = "content",
                        Color = "#8888ff",
                        Radius = 6,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.IDLE, Parameters = new() { ["friction"] = 0.9 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = DefaultWallBehavior()
                    }
                },
                Transitions = new List<StateTransition>
                {
                    new() { FromState = "wandering", ToState = "content", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_STATE, Parameters = new() { ["state"] = "wandering", ["count"] = 3 } } },
                    new() { FromState = "content", ToState = "wandering", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_COUNT, Parameters = new() { ["min"] = 0, ["max"] = 2 } } }
                }
            };

            return new SimulationConfiguration
            {
                Id = "segregation",
                Name = "Segregation Model",
                Description = "Schelling segregation model with emergent clustering",
                ParticleConfigs = new List<ParticleConfiguration> { red, blue },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 8000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = false,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        private SimulationConfiguration CreatePredatorPreyConfig()
        {
            var prey = new ParticleConfiguration
            {
                Id = "prey",
                Name = "Prey",
                Description = "Prey that flees predators",
                InitialState = "grazing",
                MaxSpeed = 2.5,
                SenseRadius = 80,
                States = new Dictionary<string, ParticleState>
                {
                    ["grazing"] = new ParticleState
                    {
                        Name = "grazing",
                        Color = "#44ff44",
                        Radius = 5,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 0.3 } },
                            new() { Type = BehaviorType.IDLE, Parameters = new() { ["friction"] = 0.95 } }
                        },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "hunting", AttractionForce = -5, AttractionRange = 80, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    },
                    ["fleeing"] = new ParticleState
                    {
                        Name = "fleeing",
                        Color = "#ffff44",
                        Radius = 5,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_AWAY, Parameters = new() { ["targetState"] = "hunting", ["strength"] = 3.0 } }
                        },
                        Physics = new PhysicsProperties { Mass = 1, Friction = 0.98, Elasticity = 0.5, Drag = 0.96, Stickiness = 0, Magnetism = 0 },
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "hunting", AttractionForce = -8, AttractionRange = 80, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    }
                },
                Transitions = new List<StateTransition>
                {
                    new() { FromState = "grazing", ToState = "fleeing", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_STATE, Parameters = new() { ["state"] = "hunting", ["count"] = 1 } } },
                    new() { FromState = "fleeing", ToState = "grazing", Condition = new TransitionCondition { Type = ConditionType.TIMER, Parameters = new() { ["duration"] = 100.0 } } }
                }
            };

            var predator = new ParticleConfiguration
            {
                Id = "predator",
                Name = "Predator",
                Description = "Predator that hunts prey",
                InitialState = "hunting",
                MaxSpeed = 2,
                SenseRadius = 100,
                States = new Dictionary<string, ParticleState>
                {
                    ["hunting"] = new ParticleState
                    {
                        Name = "hunting",
                        Color = "#ff4444",
                        Radius = 7,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "fleeing", ["strength"] = 1.5 } },
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "grazing", ["strength"] = 1.5 } },
                            new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 0.2 } }
                        },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "grazing", AttractionForce = 4, AttractionRange = 100, StickOnContact = false, StickStrength = 0 },
                            new() { TargetState = "fleeing", AttractionForce = 5, AttractionRange = 100, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    }
                },
                Transitions = new List<StateTransition>()
            };

            return new SimulationConfiguration
            {
                Id = "predator-prey",
                Name = "Predator-Prey",
                Description = "Predator-prey dynamics with emergent chase behavior",
                ParticleConfigs = new List<ParticleConfiguration> { prey, predator },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 6000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = true,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        private SimulationConfiguration CreateCrystalGrowthConfig()
        {
            var seed = new ParticleConfiguration
            {
                Id = "seed",
                Name = "Seed",
                Description = "Crystal seed that attracts wanderers",
                InitialState = "anchored",
                MaxSpeed = 0,
                SenseRadius = 40,
                States = new Dictionary<string, ParticleState>
                {
                    ["anchored"] = new ParticleState
                    {
                        Name = "anchored",
                        Color = "#ff44ff",
                        Radius = 8,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.EMIT_SIGNAL, Parameters = new() { ["signal"] = "anchor", ["range"] = 40.0 } }
                        },
                        Physics = new PhysicsProperties { Mass = 10, Friction = 0.98, Elasticity = 0.5, Drag = 0.99, Stickiness = 0, Magnetism = 0 },
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "attracted", AttractionForce = 3, AttractionRange = 60, StickOnContact = false, StickStrength = 0 },
                            new() { TargetState = "crystallized", AttractionForce = 0, AttractionRange = 0, StickOnContact = true, StickStrength = 0.9 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    }
                },
                Transitions = new List<StateTransition>()
            };

            var wanderer = new ParticleConfiguration
            {
                Id = "wanderer",
                Name = "Wanderer",
                Description = "Particle that crystallizes near seeds",
                InitialState = "wandering",
                MaxSpeed = 1.5,
                SenseRadius = 40,
                States = new Dictionary<string, ParticleState>
                {
                    ["wandering"] = new ParticleState
                    {
                        Name = "wandering",
                        Color = "#44ffff",
                        Radius = 5,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 0.5 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = DefaultWallBehavior()
                    },
                    ["attracted"] = new ParticleState
                    {
                        Name = "attracted",
                        Color = "#88ffff",
                        Radius = 5,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "anchored", ["strength"] = 2.0 } },
                            new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "crystallized", ["strength"] = 1.0 } }
                        },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "anchored", AttractionForce = 5, AttractionRange = 60, StickOnContact = true, StickStrength = 0.7 },
                            new() { TargetState = "crystallized", AttractionForce = 3, AttractionRange = 50, StickOnContact = true, StickStrength = 0.8 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    },
                    ["crystallized"] = new ParticleState
                    {
                        Name = "crystallized",
                        Color = "#ff44ff",
                        Radius = 6,
                        Behaviors = new List<Behavior>
                        {
                            new() { Type = BehaviorType.IDLE, Parameters = new() { ["friction"] = 0.8 } },
                            new() { Type = BehaviorType.EMIT_SIGNAL, Parameters = new() { ["signal"] = "anchor", ["range"] = 40.0 } }
                        },
                        Physics = new PhysicsProperties { Mass = 1, Friction = 0.5, Elasticity = 0.5, Drag = 0.99, Stickiness = 0, Magnetism = 0 },
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "anchored", AttractionForce = 0, AttractionRange = 0, StickOnContact = true, StickStrength = 0.9 },
                            new() { TargetState = "crystallized", AttractionForce = 0, AttractionRange = 0, StickOnContact = true, StickStrength = 0.9 },
                            new() { TargetState = "attracted", AttractionForce = 3, AttractionRange = 50, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    }
                },
                Transitions = new List<StateTransition>
                {
                    new() { FromState = "wandering", ToState = "attracted", Condition = new TransitionCondition { Type = ConditionType.SIGNAL_RECEIVED, Parameters = new() { ["signal"] = "anchor" } } },
                    new() { FromState = "attracted", ToState = "crystallized", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_STATE, Parameters = new() { ["state"] = "anchored", ["count"] = 1 } } },
                    new() { FromState = "attracted", ToState = "crystallized", Condition = new TransitionCondition { Type = ConditionType.NEIGHBOR_STATE, Parameters = new() { ["state"] = "crystallized", ["count"] = 2 } } }
                }
            };

            return new SimulationConfiguration
            {
                Id = "crystal-growth",
                Name = "Crystal Growth",
                Description = "Emergent crystalline structure formation with sticky particles",
                ParticleConfigs = new List<ParticleConfiguration> { seed, wanderer },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 7000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = false,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        private SimulationConfiguration CreateChaoticConfig()
        {
            var chaotic = new ParticleConfiguration
            {
                Id = "chaotic",
                Name = "Chaotic",
                Description = "Particle with random state transitions",
                InitialState = "state_a",
                MaxSpeed = 3,
                SenseRadius = 50,
                States = new Dictionary<string, ParticleState>
                {
                    ["state_a"] = new ParticleState
                    {
                        Name = "state_a",
                        Color = "#ff0000",
                        Radius = 6,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "state_b", ["strength"] = 1.0 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    },
                    ["state_b"] = new ParticleState
                    {
                        Name = "state_b",
                        Color = "#00ff00",
                        Radius = 6,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "state_c", ["strength"] = 1.0 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    },
                    ["state_c"] = new ParticleState
                    {
                        Name = "state_c",
                        Color = "#0000ff",
                        Radius = 6,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_TOWARDS, Parameters = new() { ["targetState"] = "state_a", ["strength"] = 1.0 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = new WallBehavior { Type = WallInteractionType.WRAP, Bounciness = 0.8, Friction = 0.95, Stickiness = 0 }
                    }
                },
                Transitions = new List<StateTransition>
                {
                    new() { FromState = "state_a", ToState = "state_b", Condition = new TransitionCondition { Type = ConditionType.RANDOM_CHANCE, Parameters = new() { ["probability"] = 0.02 } } },
                    new() { FromState = "state_b", ToState = "state_c", Condition = new TransitionCondition { Type = ConditionType.RANDOM_CHANCE, Parameters = new() { ["probability"] = 0.02 } } },
                    new() { FromState = "state_c", ToState = "state_a", Condition = new TransitionCondition { Type = ConditionType.RANDOM_CHANCE, Parameters = new() { ["probability"] = 0.02 } } }
                }
            };

            return new SimulationConfiguration
            {
                Id = "chaotic",
                Name = "Chaotic System",
                Description = "Unpredictable emergent patterns",
                ParticleConfigs = new List<ParticleConfiguration> { chaotic },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 6000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = true,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        private SimulationConfiguration CreateGravityBouncingConfig()
        {
            var bouncer = new ParticleConfiguration
            {
                Id = "bouncer",
                Name = "Bouncer",
                Description = "Particle that bounces with gravity",
                InitialState = "bouncing",
                MaxSpeed = 50,
                SenseRadius = 500,
                States = new Dictionary<string, ParticleState>
                {
                    ["bouncing"] = new ParticleState
                    {
                        Name = "bouncing",
                        Color = "#ff8800",
                        Radius = 8,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 2.0 } } },
                        Physics = new PhysicsProperties { Mass = 1.5, Friction = 0.98, Elasticity = 0.9, Drag = 0.99, Stickiness = 0, Magnetism = 0 },
                        Interactions = new List<StateInteraction>(),
                        WallBehavior = new WallBehavior { Type = WallInteractionType.BOUNCE, Bounciness = 0.9, Friction = 0.95, Stickiness = 0 }
                    }
                },
                Transitions = new List<StateTransition>()
            };

            var walls = new List<Wall>
            {
                new() { Id = "floor", Type = WallType.SOLID, X1 = 0, Y1 = 59000, X2 = 80000, Y2 = 59000, Thickness = 100, Color = "#666666" },
                new() { Id = "platform1", Type = WallType.SOLID, X1 = 10000, Y1 = 45000, X2 = 30000, Y2 = 45000, Thickness = 100, Color = "#888888" },
                new() { Id = "platform2", Type = WallType.SOLID, X1 = 50000, Y1 = 35000, X2 = 70000, Y2 = 35000, Thickness = 100, Color = "#888888" },
                new() { Id = "platform3", Type = WallType.SOLID, X1 = 25000, Y1 = 25000, X2 = 55000, Y2 = 25000, Thickness = 100, Color = "#888888" }
            };

            return new SimulationConfiguration
            {
                Id = "gravity-bouncing",
                Name = "Gravity & Bouncing",
                Description = "Particles falling and bouncing with gravity",
                ParticleConfigs = new List<ParticleConfiguration> { bouncer },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 8000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = false,
                Gravity = new GravitySettings { Enabled = true, Direction = new Vector2D(0, 0.5), Strength = 1 },
                Walls = walls
            };
        }

        private SimulationConfiguration CreateMagneticConfig()
        {
            var positive = new ParticleConfiguration
            {
                Id = "positive",
                Name = "Positive",
                Description = "Positively charged particle",
                InitialState = "charged",
                MaxSpeed = 40,
                SenseRadius = 2000,
                States = new Dictionary<string, ParticleState>
                {
                    ["charged"] = new ParticleState
                    {
                        Name = "charged",
                        Color = "#ff0000",
                        Radius = 7,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 0.5 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "charged", AttractionForce = -15, AttractionRange = 2000, StickOnContact = false, StickStrength = 0 },
                            new() { TargetState = "negative", AttractionForce = 20, AttractionRange = 2000, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    }
                },
                Transitions = new List<StateTransition>()
            };

            var negative = new ParticleConfiguration
            {
                Id = "negative",
                Name = "Negative",
                Description = "Negatively charged particle",
                InitialState = "negative",
                MaxSpeed = 40,
                SenseRadius = 2000,
                States = new Dictionary<string, ParticleState>
                {
                    ["negative"] = new ParticleState
                    {
                        Name = "negative",
                        Color = "#0000ff",
                        Radius = 7,
                        Behaviors = new List<Behavior> { new() { Type = BehaviorType.MOVE_RANDOM, Parameters = new() { ["strength"] = 0.5 } } },
                        Physics = DefaultPhysics(),
                        Interactions = new List<StateInteraction>
                        {
                            new() { TargetState = "negative", AttractionForce = -15, AttractionRange = 2000, StickOnContact = false, StickStrength = 0 },
                            new() { TargetState = "charged", AttractionForce = 20, AttractionRange = 2000, StickOnContact = false, StickStrength = 0 }
                        },
                        WallBehavior = DefaultWallBehavior()
                    }
                },
                Transitions = new List<StateTransition>()
            };

            return new SimulationConfiguration
            {
                Id = "magnetic",
                Name = "Magnetic Particles",
                Description = "Particles with attraction and repulsion forces",
                ParticleConfigs = new List<ParticleConfiguration> { positive, negative },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 10000,
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = false,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        public SimulationConfiguration? GetConfiguration(string id)
        {
            return _configurations.TryGetValue(id, out var config) ? config : null;
        }

        public List<SimulationConfiguration> GetAllConfigurations()
        {
            return _configurations.Values.ToList();
        }

        public void AddConfiguration(SimulationConfiguration config)
        {
            _configurations[config.Id] = config;
        }

        public SimulationConfiguration GenerateRandomConfiguration()
        {
            var id = $"random_{DateTime.Now.Ticks}";
            var numStates = 2 + _random.Next(3);
            var states = new Dictionary<string, ParticleState>();
            var transitions = new List<StateTransition>();

            var stateNames = Enumerable.Range(0, numStates).Select(i => $"state_{i}").ToArray();
            var colors = new[] { "#ff4444", "#44ff44", "#4444ff", "#ffff44", "#ff44ff", "#44ffff" };

            for (int i = 0; i < stateNames.Length; i++)
            {
                states[stateNames[i]] = new ParticleState
                {
                    Name = stateNames[i],
                    Color = colors[i % colors.Length],
                    Radius = 5 + _random.NextDouble() * 3,
                    Behaviors = GenerateRandomBehaviors(),
                    Physics = DefaultPhysics(),
                    Interactions = new List<StateInteraction>(),
                    WallBehavior = DefaultWallBehavior()
                };
            }

            foreach (var fromState in stateNames)
            {
                if (_random.NextDouble() > 0.3)
                {
                    var toState = stateNames[_random.Next(stateNames.Length)];
                    transitions.Add(new StateTransition
                    {
                        FromState = fromState,
                        ToState = toState,
                        Condition = GenerateRandomCondition()
                    });
                }
            }

            var particleConfig = new ParticleConfiguration
            {
                Id = $"particle_{id}",
                Name = "Random Particle",
                Description = "Randomly generated particle configuration",
                States = states,
                Transitions = transitions,
                InitialState = stateNames[0],
                MaxSpeed = 1 + _random.NextDouble() * 3,
                SenseRadius = 40 + _random.NextDouble() * 60
            };

            return new SimulationConfiguration
            {
                Id = id,
                Name = "Random Configuration",
                Description = "Randomly generated simulation",
                ParticleConfigs = new List<ParticleConfiguration> { particleConfig },
                WorldWidth = 8000,
                WorldHeight = 6000,
                InitialParticleCount = 4000 + _random.Next(6000),
                MaxParticles = 100000,
                TickRate = 60,
                WrapEdges = _random.NextDouble() > 0.5,
                Gravity = new GravitySettings { Enabled = false, Direction = new Vector2D(0, 0), Strength = 1 },
                Walls = new List<Wall>()
            };
        }

        private List<Behavior> GenerateRandomBehaviors()
        {
            var behaviors = new List<Behavior>();
            var behaviorTypes = new[] { BehaviorType.MOVE_RANDOM, BehaviorType.MOVE_TOWARDS, BehaviorType.MOVE_AWAY, BehaviorType.IDLE };

            var numBehaviors = 1 + _random.Next(3);
            for (int i = 0; i < numBehaviors; i++)
            {
                var type = behaviorTypes[_random.Next(behaviorTypes.Length)];
                behaviors.Add(new Behavior
                {
                    Type = type,
                    Parameters = GenerateBehaviorParams(type)
                });
            }

            return behaviors;
        }

        private Dictionary<string, object> GenerateBehaviorParams(BehaviorType type)
        {
            return type switch
            {
                BehaviorType.MOVE_RANDOM => new Dictionary<string, object> { ["strength"] = 0.1 + _random.NextDouble() * 0.5 },
                BehaviorType.MOVE_TOWARDS or BehaviorType.MOVE_AWAY => new Dictionary<string, object> { ["strength"] = 0.5 + _random.NextDouble() * 2 },
                BehaviorType.IDLE => new Dictionary<string, object> { ["friction"] = 0.8 + _random.NextDouble() * 0.15 },
                _ => new Dictionary<string, object>()
            };
        }

        private TransitionCondition GenerateRandomCondition()
        {
            var conditionTypes = new[] { ConditionType.TIMER, ConditionType.NEIGHBOR_COUNT, ConditionType.RANDOM_CHANCE };
            var type = conditionTypes[_random.Next(conditionTypes.Length)];

            return type switch
            {
                ConditionType.TIMER => new TransitionCondition { Type = type, Parameters = new() { ["duration"] = 50.0 + _random.NextDouble() * 200 } },
                ConditionType.NEIGHBOR_COUNT => new TransitionCondition { Type = type, Parameters = new() { ["min"] = _random.Next(3), ["max"] = 3 + _random.Next(5) } },
                ConditionType.RANDOM_CHANCE => new TransitionCondition { Type = type, Parameters = new() { ["probability"] = 0.005 + _random.NextDouble() * 0.03 } },
                _ => new TransitionCondition { Type = ConditionType.ALWAYS, Parameters = new() }
            };
        }
    }
}

