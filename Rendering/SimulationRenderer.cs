using EmergentComputing.Engine;
using EmergentComputing.Models;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EmergentComputing.Rendering
{
    public class SimulationRenderer
    {
        private readonly SKElement _skElement;
        private SimulationEngine _engine;
        
        // Camera properties
        private double _cameraX;
        private double _cameraY;
        private double _zoom = 1.0;
        private bool _isDragging;
        private Point _lastMousePosition;

        // Display options
        private bool _showVelocity;
        private bool _showSenseRadius;

        // Snapshot rendering
        private ParticleSnapshot? _currentSnapshot;
        private bool _isRenderingSnapshot;

        public SimulationRenderer(SKElement skElement, SimulationEngine engine)
        {
            _skElement = skElement;
            _engine = engine;
            
            var config = _engine.GetConfig();
            _cameraX = config.WorldWidth / 2;
            _cameraY = config.WorldHeight / 2;

            _skElement.PaintSurface += OnPaintSurface;
            
            // Setup mouse events
            _skElement.MouseWheel += OnMouseWheel;
            _skElement.MouseDown += OnMouseDown;
            _skElement.MouseMove += OnMouseMove;
            _skElement.MouseUp += OnMouseUp;
            _skElement.MouseLeave += OnMouseLeave;
        }

        public void SetEngine(SimulationEngine engine)
        {
            _engine = engine;
            var config = _engine.GetConfig();
            _cameraX = config.WorldWidth / 2;
            _cameraY = config.WorldHeight / 2;
            _zoom = 1.0;
        }

        public void SetShowVelocity(bool show) => _showVelocity = show;
        public void SetShowSenseRadius(bool show) => _showSenseRadius = show;

        public void Render()
        {
            _skElement.InvalidateVisual();
        }

        public void RenderSnapshot(ParticleSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            _isRenderingSnapshot = true;
            _skElement.InvalidateVisual();
        }

        public void StopSnapshotRendering()
        {
            _isRenderingSnapshot = false;
            _currentSnapshot = null;
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColor.Parse("#0a0a0a"));

            var config = _engine.GetConfig();
            var width = e.Info.Width;
            var height = e.Info.Height;

            canvas.Save();

            // Apply camera transform
            canvas.Translate(width / 2f, height / 2f);
            canvas.Scale((float)_zoom, (float)_zoom);
            canvas.Translate(-(float)_cameraX, -(float)_cameraY);

            // Draw world bounds
            using (var paint = new SKPaint
            {
                Color = SKColor.Parse("#333333"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)(5 / _zoom),
                IsAntialias = true
            })
            {
                canvas.DrawRect(0, 0, (float)config.WorldWidth, (float)config.WorldHeight, paint);
            }

            // Draw grid
            DrawGrid(canvas, config, width, height);

            // Draw walls
            DrawWalls(canvas);

            // Calculate visible bounds (frustum culling)
            var viewLeft = _cameraX - (width / 2) / _zoom;
            var viewRight = _cameraX + (width / 2) / _zoom;
            var viewTop = _cameraY - (height / 2) / _zoom;
            var viewBottom = _cameraY + (height / 2) / _zoom;

            // Draw particles (either from snapshot or engine)
            if (_isRenderingSnapshot && _currentSnapshot != null)
            {
                DrawSnapshotParticles(canvas, _currentSnapshot, viewLeft, viewRight, viewTop, viewBottom);
            }
            else
            {
                // Draw only visible particles from engine
                var particles = _engine.GetParticles();
                foreach (var particle in particles)
                {
                    var pos = particle.GetData().Position;
                    var state = particle.GetCurrentState();
                    if (state == null) continue;

                    var radius = state.Radius;
                    
                    // Frustum culling - skip particles outside view
                    if (pos.X + radius < viewLeft || pos.X - radius > viewRight ||
                        pos.Y + radius < viewTop || pos.Y - radius > viewBottom)
                    {
                        continue;
                    }

                    DrawParticle(canvas, particle);
                }
            }

            canvas.Restore();

            // Draw stats (not affected by camera)
            DrawStats(canvas, width, height);
        }

        private void DrawGrid(SKCanvas canvas, SimulationConfiguration config, int width, int height)
        {
            using var paint = new SKPaint
            {
                Color = SKColor.Parse("#1a1a1a"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)(1 / _zoom),
                IsAntialias = true
            };

            // Adaptive grid size based on zoom
            double gridSize = 5000;
            if (_zoom > 0.05) gridSize = 1000;
            if (_zoom > 0.1) gridSize = 500;
            if (_zoom > 0.2) gridSize = 100;
            if (_zoom > 0.5) gridSize = 50;

            var viewLeft = _cameraX - (width / 2) / _zoom;
            var viewRight = _cameraX + (width / 2) / _zoom;
            var viewTop = _cameraY - (height / 2) / _zoom;
            var viewBottom = _cameraY + (height / 2) / _zoom;

            var startX = Math.Max(0, Math.Floor(viewLeft / gridSize) * gridSize);
            var endX = Math.Min(config.WorldWidth, Math.Ceiling(viewRight / gridSize) * gridSize);
            var startY = Math.Max(0, Math.Floor(viewTop / gridSize) * gridSize);
            var endY = Math.Min(config.WorldHeight, Math.Ceiling(viewBottom / gridSize) * gridSize);

            for (double x = startX; x <= endX; x += gridSize)
            {
                canvas.DrawLine(
                    (float)x, (float)Math.Max(0, viewTop),
                    (float)x, (float)Math.Min(config.WorldHeight, viewBottom),
                    paint
                );
            }

            for (double y = startY; y <= endY; y += gridSize)
            {
                canvas.DrawLine(
                    (float)Math.Max(0, viewLeft), (float)y,
                    (float)Math.Min(config.WorldWidth, viewRight), (float)y,
                    paint
                );
            }
        }

        private void DrawWalls(SKCanvas canvas)
        {
            var walls = _engine.GetWalls();
            if (walls.Count == 0) return;

            foreach (var wall in walls)
            {
                using var paint = new SKPaint
                {
                    Color = SKColor.Parse(wall.Color ?? "#666666"),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)(wall.Thickness / _zoom),
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };

                canvas.DrawLine(
                    (float)wall.X1, (float)wall.Y1,
                    (float)wall.X2, (float)wall.Y2,
                    paint
                );

                // Draw wall type indicator
                if (wall.Type == WallType.STICKY)
                {
                    using var indicatorPaint = new SKPaint
                    {
                        Color = SKColors.Yellow,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = (float)((wall.Thickness + 4) / _zoom),
                        PathEffect = SKPathEffect.CreateDash(new[] { 10f / (float)_zoom, 10f / (float)_zoom }, 0),
                        IsAntialias = true
                    };
                    canvas.DrawLine((float)wall.X1, (float)wall.Y1, (float)wall.X2, (float)wall.Y2, indicatorPaint);
                }
                else if (wall.Type == WallType.DEADLY)
                {
                    using var indicatorPaint = new SKPaint
                    {
                        Color = SKColors.Red,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = (float)((wall.Thickness + 4) / _zoom),
                        PathEffect = SKPathEffect.CreateDash(new[] { 5f / (float)_zoom, 5f / (float)_zoom }, 0),
                        IsAntialias = true
                    };
                    canvas.DrawLine((float)wall.X1, (float)wall.Y1, (float)wall.X2, (float)wall.Y2, indicatorPaint);
                }
            }
        }

        private void DrawSnapshotParticles(SKCanvas canvas, ParticleSnapshot snapshot, double viewLeft, double viewRight, double viewTop, double viewBottom)
        {
            var config = _engine.GetConfig();
            
            // Get particle configs to look up state information
            var particleConfigs = config.ParticleConfigs;
            
            foreach (var snapshotParticle in snapshot.Particles)
            {
                // Find the particle config and state for this particle
                ParticleState? state = null;
                ParticleConfiguration? particleConfig = null;
                
                foreach (var pConfig in particleConfigs)
                {
                    if (pConfig.States.ContainsKey(snapshotParticle.State))
                    {
                        state = pConfig.States[snapshotParticle.State];
                        particleConfig = pConfig;
                        break;
                    }
                }
                
                if (state == null) continue;

                var x = (float)snapshotParticle.X;
                var y = (float)snapshotParticle.Y;
                var radius = (float)state.Radius;

                // Frustum culling
                if (snapshotParticle.X + radius < viewLeft || snapshotParticle.X - radius > viewRight ||
                    snapshotParticle.Y + radius < viewTop || snapshotParticle.Y - radius > viewBottom)
                {
                    continue;
                }

                // Draw particle body
                using (var paint = new SKPaint
                {
                    Color = SKColor.Parse(state.Color),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawCircle(x, y, radius, paint);
                }

                // Draw glow effect when zoomed in
                if (_zoom > 0.05)
                {
                    using var shader = SKShader.CreateRadialGradient(
                        new SKPoint(x, y),
                        radius * 2,
                        new[] { SKColor.Parse(state.Color).WithAlpha(64), SKColors.Transparent },
                        new[] { 0f, 1f },
                        SKShaderTileMode.Clamp
                    );
                    using var glowPaint = new SKPaint
                    {
                        Shader = shader,
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    canvas.DrawCircle(x, y, radius * 2, glowPaint);
                }

                // Draw velocity vector
                if (_showVelocity)
                {
                    var vel = snapshotParticle.Velocity;
                    var speed = vel.Length();
                    if (speed > 0.1)
                    {
                        using var velPaint = new SKPaint
                        {
                            Color = SKColors.White.WithAlpha(128),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = (float)(2 / _zoom),
                            IsAntialias = true
                        };
                        canvas.DrawLine(
                            x, y,
                            (float)(x + vel.X * 5), (float)(y + vel.Y * 5),
                            velPaint
                        );
                    }
                }

                // Draw state name when zoomed in
                if (_zoom > 0.1)
                {
                    using var textPaint = new SKPaint
                    {
                        Color = SKColors.White,
                        TextSize = (float)(10 / _zoom),
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Center
                    };
                    var stateName = snapshotParticle.State.Length > 3 ? snapshotParticle.State.Substring(0, 3) : snapshotParticle.State;
                    canvas.DrawText(stateName, x, (float)(y + radius + 12 / _zoom), textPaint);
                }
            }
        }

        private void DrawParticle(SKCanvas canvas, Particle particle)
        {
            var data = particle.GetData();
            var state = particle.GetCurrentState();
            if (state == null) return;

            var x = (float)data.Position.X;
            var y = (float)data.Position.Y;
            var radius = (float)state.Radius;

            // Draw sense radius
            if (_showSenseRadius)
            {
                using var sensePaint = new SKPaint
                {
                    Color = SKColor.Parse(state.Color).WithAlpha(32),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)(1 / _zoom),
                    IsAntialias = true
                };
                canvas.DrawCircle(x, y, (float)particle.GetConfig().SenseRadius, sensePaint);
            }

            // Draw particle body
            using (var paint = new SKPaint
            {
                Color = SKColor.Parse(state.Color),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawCircle(x, y, radius, paint);
            }

            // Draw glow effect when zoomed in
            if (_zoom > 0.05)
            {
                using var shader = SKShader.CreateRadialGradient(
                    new SKPoint(x, y),
                    radius * 2,
                    new[] { SKColor.Parse(state.Color).WithAlpha(64), SKColors.Transparent },
                    new[] { 0f, 1f },
                    SKShaderTileMode.Clamp
                );
                using var glowPaint = new SKPaint
                {
                    Shader = shader,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawCircle(x, y, radius * 2, glowPaint);
            }

            // Draw velocity vector
            if (_showVelocity)
            {
                var vel = data.Velocity;
                var speed = vel.Length();
                if (speed > 0.1)
                {
                    using var velPaint = new SKPaint
                    {
                        Color = SKColors.White.WithAlpha(128),
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = (float)(2 / _zoom),
                        IsAntialias = true
                    };
                    canvas.DrawLine(
                        x, y,
                        (float)(x + vel.X * 5), (float)(y + vel.Y * 5),
                        velPaint
                    );
                }
            }

            // Draw state name when zoomed in
            if (_zoom > 0.1)
            {
                using var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = (float)(10 / _zoom),
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };
                var stateName = data.CurrentState.Length > 3 ? data.CurrentState.Substring(0, 3) : data.CurrentState;
                canvas.DrawText(stateName, x, (float)(y + radius + 12 / _zoom), textPaint);
            }

            // Draw stuck indicator
            if (data.StuckToWall != null)
            {
                using var stuckPaint = new SKPaint
                {
                    Color = SKColors.Yellow,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)(2 / _zoom),
                    IsAntialias = true
                };
                canvas.DrawCircle(x, y, radius + 3, stuckPaint);
            }

            // Draw attached indicator
            if (data.AttachedTo.Count > 0)
            {
                using var attachedPaint = new SKPaint
                {
                    Color = SKColors.Magenta,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)(1 / _zoom),
                    PathEffect = SKPathEffect.CreateDash(new[] { 3f / (float)_zoom, 3f / (float)_zoom }, 0),
                    IsAntialias = true
                };
                canvas.DrawCircle(x, y, radius + 5, attachedPaint);
            }
        }

        private void DrawStats(SKCanvas canvas, int width, int height)
        {
            var particles = _engine.GetParticles();
            var tick = _engine.GetTickCount();
            var distribution = _engine.GetStateDistribution();
            var config = _engine.GetConfig();

            // Background for stats
            using (var bgPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(178),
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRect(5, 5, 200, 160, bgPaint);
            }

            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 12,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
            };

            float y = 20;
            canvas.DrawText($"Tick: {tick}", 10, y, textPaint);
            y += 20;
            canvas.DrawText($"Particles: {particles.Count}", 10, y, textPaint);
            y += 20;
            canvas.DrawText($"Camera: ({(int)_cameraX}, {(int)_cameraY})", 10, y, textPaint);
            y += 20;
            canvas.DrawText($"Zoom: {(_zoom * 100):F1}%", 10, y, textPaint);
            y += 20;
            canvas.DrawText($"World: {config.WorldWidth}x{config.WorldHeight}", 10, y, textPaint);
            y += 20;

            foreach (var (state, count) in distribution)
            {
                canvas.DrawText($"{state}: {count}", 10, y, textPaint);
                y += 15;
            }

            // Controls hint
            using (var bgPaint2 = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(178),
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRect(width - 205, 5, 200, 90, bgPaint2);
            }

            using var hintPaint = new SKPaint
            {
                Color = SKColor.Parse("#aaaaaa"),
                TextSize = 10,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
            };

            y = 20;
            canvas.DrawText("Controls:", width - 195, y, hintPaint);
            y += 15;
            canvas.DrawText("WASD/Arrows: Pan", width - 195, y, hintPaint);
            y += 15;
            canvas.DrawText("Mouse Drag: Pan", width - 195, y, hintPaint);
            y += 15;
            canvas.DrawText("Scroll: Zoom", width - 195, y, hintPaint);
            y += 15;
            canvas.DrawText("+/-: Zoom in/out", width - 195, y, hintPaint);
            y += 15;
            canvas.DrawText("0: Reset camera", width - 195, y, hintPaint);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            _zoom *= zoomFactor;
            _zoom = Math.Max(0.001, Math.Min(1, _zoom));
            Render();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(_skElement);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var currentPos = e.GetPosition(_skElement);
                var dx = currentPos.X - _lastMousePosition.X;
                var dy = currentPos.Y - _lastMousePosition.Y;
                
                _cameraX -= dx / _zoom;
                _cameraY -= dy / _zoom;
                
                _lastMousePosition = currentPos;
                Render();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        public void HandleKeyDown(Key key)
        {
            var moveSpeed = 100 / _zoom;
            var config = _engine.GetConfig();

            switch (key)
            {
                case Key.W:
                case Key.Up:
                    _cameraY -= moveSpeed;
                    break;
                case Key.S:
                case Key.Down:
                    _cameraY += moveSpeed;
                    break;
                case Key.A:
                case Key.Left:
                    _cameraX -= moveSpeed;
                    break;
                case Key.D:
                case Key.Right:
                    _cameraX += moveSpeed;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    _zoom *= 1.1;
                    _zoom = Math.Min(1, _zoom);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    _zoom *= 0.9;
                    _zoom = Math.Max(0.001, _zoom);
                    break;
                case Key.D0:
                case Key.NumPad0:
                    _cameraX = config.WorldWidth / 2;
                    _cameraY = config.WorldHeight / 2;
                    _zoom = 1.0;
                    break;
            }
            Render();
        }
    }
}

