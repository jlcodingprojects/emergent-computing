using EmergentComputing.Models;
using System;
using System.Collections.Generic;

namespace EmergentComputing.Engine
{
    /// <summary>
    /// Spatial hash grid for efficient neighbor queries
    /// Reduces neighbor search from O(n²) to O(n)
    /// </summary>
    public class SpatialGrid
    {
        private readonly Dictionary<(int, int), List<Particle>> _grid;
        private readonly double _cellSize;
        private readonly double _worldWidth;
        private readonly double _worldHeight;

        public SpatialGrid(double worldWidth, double worldHeight, double cellSize)
        {
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _cellSize = cellSize;
            _grid = new Dictionary<(int, int), List<Particle>>(256);
        }

        public void Clear()
        {
            foreach (var cell in _grid.Values)
            {
                cell.Clear();
            }
        }

        public void Insert(Particle particle)
        {
            var pos = particle.GetData().Position;
            var cell = GetCell(pos.X, pos.Y);
            
            if (!_grid.TryGetValue(cell, out var list))
            {
                list = new List<Particle>(16);
                _grid[cell] = list;
            }
            
            list.Add(particle);
        }

        public List<Particle> GetNearby(Particle particle, double radius)
        {
            var pos = particle.GetData().Position;
            var result = new List<Particle>(32);
            
            // Calculate cell range to check
            var minCellX = (int)Math.Floor((pos.X - radius) / _cellSize);
            var maxCellX = (int)Math.Floor((pos.X + radius) / _cellSize);
            var minCellY = (int)Math.Floor((pos.Y - radius) / _cellSize);
            var maxCellY = (int)Math.Floor((pos.Y + radius) / _cellSize);

            var radiusSquared = radius * radius;

            // Check all cells in range
            for (int x = minCellX; x <= maxCellX; x++)
            {
                for (int y = minCellY; y <= maxCellY; y++)
                {
                    if (_grid.TryGetValue((x, y), out var cell))
                    {
                        foreach (var other in cell)
                        {
                            if (other == particle) continue;

                            var otherPos = other.GetData().Position;
                            var dx = pos.X - otherPos.X;
                            var dy = pos.Y - otherPos.Y;
                            var distSquared = dx * dx + dy * dy;

                            if (distSquared <= radiusSquared)
                            {
                                result.Add(other);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private (int, int) GetCell(double x, double y)
        {
            return (
                (int)Math.Floor(x / _cellSize),
                (int)Math.Floor(y / _cellSize)
            );
        }
    }
}

