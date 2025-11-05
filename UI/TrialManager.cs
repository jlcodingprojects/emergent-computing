using EmergentComputing.Engine;
using EmergentComputing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmergentComputing.UI
{
    public class TrialProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public bool Running { get; set; }
    }

    public class TrialAnalysis
    {
        public int Count { get; set; }
        public EmergentMetrics AverageMetrics { get; set; } = new();
        public TrialResult? BestTrial { get; set; }
        public TrialResult? WorstTrial { get; set; }
    }

    public class TrialManager
    {
        private readonly List<TrialResult> _trials = new();
        private TrialResult? _currentTrial;
        private TrialProgress _batchProgress = new();
        
        public event Action<TrialProgress>? OnProgressUpdate;
        public event Action<TrialResult>? OnTrialComplete;

        public async Task<TrialResult> RunSingleTrial(
            SimulationConfiguration config,
            int duration = 5000,
            bool record = false)
        {
            var engine = new SimulationEngine(config);
            var trialId = $"trial_{DateTime.Now.Ticks}_{new Random().Next()}";
            var startTime = DateTime.Now.Ticks;

            if (record)
            {
                engine.StartRecording();
            }

            engine.Start();

            var ticksPerSecond = config.TickRate;
            var totalTicks = (int)Math.Floor((duration / 1000.0) * ticksPerSecond);

            for (int i = 0; i < totalTicks; i++)
            {
                engine.Tick(1);

                if (i % 100 == 0)
                {
                    await Task.Delay(0);
                }
            }

            engine.Pause();

            if (record)
            {
                engine.StopRecording();
            }

            var endTime = DateTime.Now.Ticks;
            var result = new TrialResult
            {
                ConfigId = config.Id,
                TrialId = trialId,
                Timestamp = startTime,
                Duration = endTime - startTime,
                FinalParticleCount = engine.GetParticles().Count,
                StateDistribution = engine.GetStateDistribution(),
                EmergentMetrics = engine.CalculateEmergentMetrics(),
                RecordedFrames = record ? engine.GetRecordedFrames() : null
            };

            _trials.Add(result);
            _currentTrial = result;

            OnTrialComplete?.Invoke(result);

            return result;
        }

        public async Task<List<TrialResult>> RunBatchTrials(
            SimulationConfiguration config,
            int numTrials = 10,
            int duration = 3000)
        {
            _batchProgress = new TrialProgress { Current = 0, Total = numTrials, Running = true };
            UpdateProgress();

            var results = new List<TrialResult>();

            for (int i = 0; i < numTrials; i++)
            {
                var result = await RunSingleTrial(config, duration, false);
                results.Add(result);

                _batchProgress.Current = i + 1;
                UpdateProgress();
            }

            _batchProgress.Running = false;
            UpdateProgress();

            return results;
        }

        public async Task<List<TrialResult>> RunBatchTrialsWithRecording(
            SimulationConfiguration config,
            int numTrials = 10,
            int duration = 3000)
        {
            _batchProgress = new TrialProgress { Current = 0, Total = numTrials, Running = true };
            UpdateProgress();

            var results = new List<TrialResult>();

            for (int i = 0; i < numTrials; i++)
            {
                // Record frames for each trial
                var result = await RunSingleTrial(config, duration, true);
                results.Add(result);

                _batchProgress.Current = i + 1;
                UpdateProgress();
            }

            _batchProgress.Running = false;
            UpdateProgress();

            return results;
        }

        public List<TrialResult> GetTrialResults() => new(_trials);
        public TrialResult? GetCurrentTrial() => _currentTrial;
        public TrialProgress GetBatchProgress() => _batchProgress;

        public void ClearTrials()
        {
            _trials.Clear();
            _currentTrial = null;
        }

        public TrialAnalysis AnalyzeTrials(string? configId = null)
        {
            var relevantTrials = configId != null
                ? _trials.Where(t => t.ConfigId == configId).ToList()
                : _trials;

            if (relevantTrials.Count == 0)
            {
                return new TrialAnalysis
                {
                    Count = 0,
                    AverageMetrics = new EmergentMetrics(),
                    BestTrial = null,
                    WorstTrial = null
                };
            }

            var avgMetrics = new EmergentMetrics();
            var bestComplexity = double.MinValue;
            var worstComplexity = double.MaxValue;
            TrialResult? bestTrial = null;
            TrialResult? worstTrial = null;

            foreach (var trial in relevantTrials)
            {
                var metrics = trial.EmergentMetrics;
                avgMetrics.Clustering += metrics.Clustering;
                avgMetrics.Movement += metrics.Movement;
                avgMetrics.StateChanges += metrics.StateChanges;
                avgMetrics.Diversity += metrics.Diversity;
                avgMetrics.Stability += metrics.Stability;
                avgMetrics.Complexity += metrics.Complexity;

                if (metrics.Complexity > bestComplexity)
                {
                    bestComplexity = metrics.Complexity;
                    bestTrial = trial;
                }
                if (metrics.Complexity < worstComplexity)
                {
                    worstComplexity = metrics.Complexity;
                    worstTrial = trial;
                }
            }

            var count = relevantTrials.Count;
            avgMetrics.Clustering /= count;
            avgMetrics.Movement /= count;
            avgMetrics.StateChanges /= count;
            avgMetrics.Diversity /= count;
            avgMetrics.Stability /= count;
            avgMetrics.Complexity /= count;

            return new TrialAnalysis
            {
                Count = count,
                AverageMetrics = avgMetrics,
                BestTrial = bestTrial,
                WorstTrial = worstTrial
            };
        }

        private void UpdateProgress()
        {
            OnProgressUpdate?.Invoke(_batchProgress);
        }

        public string ExportResults()
        {
            return System.Text.Json.JsonSerializer.Serialize(_trials, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        public void ImportResults(string json)
        {
            try
            {
                var imported = System.Text.Json.JsonSerializer.Deserialize<List<TrialResult>>(json);
                if (imported != null)
                {
                    _trials.AddRange(imported);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to import results: {ex.Message}");
            }
        }
    }
}

