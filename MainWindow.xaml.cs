using EmergentComputing.Configuration;
using EmergentComputing.Engine;
using EmergentComputing.Models;
using EmergentComputing.Rendering;
using EmergentComputing.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EmergentComputing
{
    public partial class MainWindow : Window
    {
        private SimulationEngine _engine;
        private SimulationRenderer _renderer;
        private readonly ConfigurationManager _configManager;
        private readonly TrialManager _trialManager;
        private DispatcherTimer? _simulationTimer;
        private DispatcherTimer _renderTimer;
        private SimulationConfiguration _currentConfig;
        private SimulationConfiguration _trialConfig;
        private bool _isPlayingBack = false;
        private List<ParticleSnapshot>? _playbackFrames;
        private int _playbackFrameIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            _configManager = new ConfigurationManager();
            _trialManager = new TrialManager();

            // Get first configuration
            var configs = _configManager.GetAllConfigurations();
            _currentConfig = configs[0];
            _trialConfig = configs[0];

            _engine = new SimulationEngine(_currentConfig);
            _renderer = new SimulationRenderer(SimulationCanvas, _engine);

            // Setup trial callbacks
            _trialManager.OnProgressUpdate += OnTrialProgress;
            _trialManager.OnTrialComplete += OnTrialComplete;

            // Setup render timer (30 FPS to reduce overhead)
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _renderTimer.Tick += (s, e) => 
            {
                if (_isPlayingBack)
                {
                    PlaybackNextFrame();
                }
                _renderer.Render();
            };
            _renderTimer.Start();

            PopulateConfigSelect();
            PopulateTrialConfigSelect();
            UpdateUI();
            UpdateStats();
        }

        private void PopulateConfigSelect()
        {
            var configs = _configManager.GetAllConfigurations();
            ConfigSelect.ItemsSource = configs;
            ConfigSelect.DisplayMemberPath = "Name";
            ConfigSelect.SelectedValuePath = "Id";
            ConfigSelect.SelectedIndex = 0;
        }

        private void PopulateTrialConfigSelect()
        {
            var configs = _configManager.GetAllConfigurations();
            TrialConfigSelect.ItemsSource = configs;
            TrialConfigSelect.DisplayMemberPath = "Name";
            TrialConfigSelect.SelectedValuePath = "Id";
            TrialConfigSelect.SelectedIndex = 0;
        }

        private void ConfigSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConfigSelect.SelectedItem is SimulationConfiguration config)
            {
                LoadConfiguration(config);
            }
        }

        private void TrialConfigSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrialConfigSelect.SelectedItem is SimulationConfiguration config)
            {
                _trialConfig = config;
                UpdateTrialResultsUI();
            }
        }

        private void LoadConfiguration(SimulationConfiguration config)
        {
            _currentConfig = config;
            _engine.UpdateConfig(config);
            _renderer.SetEngine(_engine);
            UpdateUI();
            UpdateStats();
        }

        private void BtnRandomConfig_Click(object sender, RoutedEventArgs e)
        {
            var config = _configManager.GenerateRandomConfiguration();
            _configManager.AddConfiguration(config);
            PopulateConfigSelect();
            ConfigSelect.SelectedValue = config.Id;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _engine.Start();
            StartSimulationTimer();
            UpdateUI();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            _engine.Pause();
            StopSimulationTimer();
            UpdateUI();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _engine.Reset();
            StopSimulationTimer();
            UpdateStats();
            UpdateUI();
        }

        private void BtnStep_Click(object sender, RoutedEventArgs e)
        {
            _engine.Tick(1);
            _renderer.Render();
            UpdateStats();
        }

        private void StartSimulationTimer()
        {
            if (_simulationTimer == null)
            {
                var tickRate = _engine.GetConfig().TickRate;
                var interval = 1000.0 / tickRate;

                _simulationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(interval)
                };
                _simulationTimer.Tick += (s, e) =>
                {
                    _engine.Tick(1);
                    UpdateStats();
                };
            }
            _simulationTimer.Start();
        }

        private void StopSimulationTimer()
        {
            _simulationTimer?.Stop();
        }

        private void SpeedControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedLabel == null) return;

            var speed = SpeedControl.Value / 50.0;
            SpeedLabel.Text = $"Speed: {speed:F1}x";

            if (_engine.IsRunning())
            {
                StopSimulationTimer();
                StartSimulationTimer();
            }
        }

        private void ChkShowVelocity_Changed(object sender, RoutedEventArgs e)
        {
            _renderer.SetShowVelocity(ChkShowVelocity.IsChecked ?? false);
        }

        private void ChkShowSenseRadius_Changed(object sender, RoutedEventArgs e)
        {
            _renderer.SetShowSenseRadius(ChkShowSenseRadius.IsChecked ?? false);
        }

        private async void BtnRunTrial_Click(object sender, RoutedEventArgs e)
        {
            BtnRunTrial.IsEnabled = false;
            _engine.Pause();
            StopSimulationTimer();

            if (int.TryParse(TrialDuration.Text, out var duration))
            {
                // Record frames for single trial playback
                await _trialManager.RunSingleTrial(_trialConfig, duration, true);
                UpdateTrialResultsUI();
            }

            BtnRunTrial.IsEnabled = true;
        }

        private async void BtnRunBatch_Click(object sender, RoutedEventArgs e)
        {
            BtnRunBatch.IsEnabled = false;
            _engine.Pause();
            StopSimulationTimer();

            if (int.TryParse(NumTrials.Text, out var numTrials) &&
                int.TryParse(TrialDuration.Text, out var duration))
            {
                await _trialManager.RunBatchTrialsWithRecording(_trialConfig, numTrials, duration);
                UpdateTrialResultsUI();
            }

            BtnRunBatch.IsEnabled = true;
        }

        private void BtnClearTrials_Click(object sender, RoutedEventArgs e)
        {
            _trialManager.ClearTrials();
            UpdateTrialResultsUI();
        }

        private void OnTrialProgress(TrialProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                TrialProgress.Maximum = progress.Total;
                TrialProgress.Value = progress.Current;
                ProgressText.Text = $"{progress.Current} / {progress.Total}";
            });
        }

        private void OnTrialComplete(TrialResult result)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateTrialResultsUI();
            });
        }

        private void UpdateTrialResultsUI()
        {
            var trials = _trialManager.GetTrialResults()
                .Where(t => t.ConfigId == _trialConfig.Id)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            TrialResultsPanel.Children.Clear();

            if (trials.Count == 0)
            {
                TrialResultsPanel.Children.Add(new TextBlock
                {
                    Text = "No trials completed yet",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 14
                });
                return;
            }

            // Add summary statistics
            var analysis = _trialManager.AnalyzeTrials(_trialConfig.Id);
            var summaryBorder = new Border
            {
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var summaryPanel = new StackPanel();
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"Summary ({analysis.Count} trials)",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var metrics = analysis.AverageMetrics;
            var summaryGrid = new Grid();
            summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var row = 0;
            AddMetricRow(summaryGrid, ref row, "Avg Complexity:", $"{metrics.Complexity:F3}");
            AddMetricRow(summaryGrid, ref row, "Avg Clustering:", $"{metrics.Clustering:F3}");
            AddMetricRow(summaryGrid, ref row, "Avg Movement:", $"{metrics.Movement:F3}");
            AddMetricRow(summaryGrid, ref row, "Avg Diversity:", $"{metrics.Diversity:F3}");
            AddMetricRow(summaryGrid, ref row, "Avg Stability:", $"{metrics.Stability:F3}");

            summaryPanel.Children.Add(summaryGrid);
            summaryBorder.Child = summaryPanel;
            TrialResultsPanel.Children.Add(summaryBorder);

            // Add individual trial cards
            TrialResultsPanel.Children.Add(new TextBlock
            {
                Text = "Individual Trials",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            });

            for (int i = 0; i < trials.Count; i++)
            {
                var trial = trials[i];
                var trialCard = CreateTrialCard(trial, i + 1);
                TrialResultsPanel.Children.Add(trialCard);
            }
        }

        private Border CreateTrialCard(TrialResult trial, int index)
        {
            var border = new Border
            {
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var mainPanel = new StackPanel();

            // Header
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Trial #{index}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White
            });

            var durationMs = trial.Duration / TimeSpan.TicksPerMillisecond;
            headerPanel.Children.Add(new TextBlock
            {
                Text = $" ({durationMs}ms)",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            mainPanel.Children.Add(headerPanel);

            // Metrics Grid
            var metricsGrid = new Grid { Margin = new Thickness(0, 10, 0, 10) };
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var row = 0;
            var m = trial.EmergentMetrics;
            AddMetricRow(metricsGrid, ref row, "Complexity:", $"{m.Complexity:F3}");
            AddMetricRow(metricsGrid, ref row, "Clustering:", $"{m.Clustering:F3}");
            AddMetricRow(metricsGrid, ref row, "Movement:", $"{m.Movement:F3}");
            AddMetricRow(metricsGrid, ref row, "Diversity:", $"{m.Diversity:F3}");
            AddMetricRow(metricsGrid, ref row, "Stability:", $"{m.Stability:F3}");
            AddMetricRow(metricsGrid, ref row, "Particles:", $"{trial.FinalParticleCount}");

            mainPanel.Children.Add(metricsGrid);

            // Playback button
            if (trial.RecordedFrames != null && trial.RecordedFrames.Count > 0)
            {
                var playbackBtn = new Button
                {
                    Content = "▶ Play at 10x Speed",
                    Width = 150,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = System.Windows.Media.Brushes.DodgerBlue,
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10, 5, 10, 5)
                };
                playbackBtn.Click += (s, e) => StartPlayback(trial);
                mainPanel.Children.Add(playbackBtn);
            }
            else
            {
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "No recording available",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontStyle = FontStyles.Italic
                });
            }

            border.Child = mainPanel;
            return border;
        }

        private void StartPlayback(TrialResult trial)
        {
            if (trial.RecordedFrames == null || trial.RecordedFrames.Count == 0)
                return;

            // Stop current simulation
            _engine.Pause();
            StopSimulationTimer();

            // Setup playback
            _playbackFrames = trial.RecordedFrames;
            _playbackFrameIndex = 0;
            _isPlayingBack = true;

            // Switch to Experiment tab to show the playback
            MainTabControl.SelectedItem = ExperimentTab;

            // The render timer will call PlaybackNextFrame automatically
        }

        private void PlaybackNextFrame()
        {
            if (_playbackFrames == null || _playbackFrameIndex >= _playbackFrames.Count)
            {
                _isPlayingBack = false;
                _playbackFrames = null;
                _playbackFrameIndex = 0;
                return;
            }

            var frame = _playbackFrames[_playbackFrameIndex];
            
            // For 10x speed, skip 9 out of 10 frames
            _playbackFrameIndex += 10;
            
            // Reconstruct particles from snapshot for visualization
            _renderer.RenderSnapshot(frame);
        }

        private void AddMetricRow(Grid grid, ref int row, string label, string value)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 2, 0, 2)
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(5, 2, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);

            row++;
        }

        private void UpdateStats()
        {
            StatTick.Text = _engine.GetTickCount().ToString();
            StatParticles.Text = _engine.GetParticles().Count.ToString();

            var metrics = _engine.CalculateEmergentMetrics();
            StatComplexity.Text = metrics.Complexity.ToString("F3");
            StatClustering.Text = metrics.Clustering.ToString("F3");
            StatMovement.Text = metrics.Movement.ToString("F3");

            var distribution = _engine.GetStateDistribution();
            StateDistributionList.ItemsSource = distribution;
        }

        private void UpdateUI()
        {
            var isRunning = _engine.IsRunning();
            BtnStart.IsEnabled = !isRunning;
            BtnPause.IsEnabled = isRunning;

            ConfigName.Text = _currentConfig.Name;
            ConfigDescription.Text = _currentConfig.Description;

            ConfigDetails.Children.Clear();
            ConfigDetails.Children.Add(new TextBlock
            {
                Text = $"World Size: {_currentConfig.WorldWidth}x{_currentConfig.WorldHeight}",
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            });
            ConfigDetails.Children.Add(new TextBlock
            {
                Text = $"Particle Types: {_currentConfig.ParticleConfigs.Count}",
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            });
            ConfigDetails.Children.Add(new TextBlock
            {
                Text = $"Wrap Edges: {(_currentConfig.WrapEdges ? "Yes" : "No")}",
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            _renderer.HandleKeyDown(e.Key);
        }

        protected override void OnClosed(EventArgs e)
        {
            _renderTimer.Stop();
            StopSimulationTimer();
            base.OnClosed(e);
        }
    }
}

