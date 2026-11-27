using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.World
{
    /// <summary>What the player knows of one part of the map (maps.md, Visibility).</summary>
    public enum FogState : byte
    {
        /// <summary>Never seen: drawn black.</summary>
        Unexplored,

        /// <summary>Seen before but not now: drawn dimmed, and what moves there is hidden.</summary>
        Explored,

        /// <summary>In the player's line of sight now.</summary>
        Visible,
    }

    /// <summary>
    /// The fog of war as a grid of square cells over the map, on the XZ plane (x is X, y is Z). Each update the
    /// player's sight is cast as rays: <see cref="BeginUpdate"/>, <see cref="RevealRay"/> for each, then
    /// <see cref="EndUpdate"/>. Cells a ray reaches become Visible; cells that were Visible and no ray reached become
    /// Explored. The map starts unexplored.
    /// </summary>
    public sealed class VisibilityGrid
    {
        // Between BeginUpdate and EndUpdate: was Visible, not reached again yet.
        private const byte Fading = 3;

        private readonly byte[] cells;
        private List<int> visible = new List<int>();
        private List<int> previous = new List<int>();
        private bool isChanged;

        /// <param name="area">The map on the XZ plane.</param>
        /// <param name="cellSize">Meters across one cell.</param>
        public VisibilityGrid(Rect area, float cellSize)
        {
            if (cellSize <= 0f || area.width <= 0f || area.height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "The area and cell size must be above 0.");
            }

            Area = area;
            CellSize = cellSize;
            Columns = Math.Max(1, (int)Math.Ceiling(area.width / cellSize));
            Rows = Math.Max(1, (int)Math.Ceiling(area.height / cellSize));
            cells = new byte[Columns * Rows];
        }

        public Rect Area { get; }
        public float CellSize { get; }
        public int Columns { get; }
        public int Rows { get; }

        /// <summary>Goes up whenever any cell's state changes, so a view redraws only then.</summary>
        public int Version { get; private set; }

        /// <summary>A cell's state, row by row from the -X, -Z corner.</summary>
        public FogState this[int column, int row] => ToState(cells[row * Columns + column]);

        /// <summary>The state where a point is; Unexplored off the map.</summary>
        public FogState StateAt(Vector2 point)
        {
            int index = IndexOf(point);
            return index < 0 ? FogState.Unexplored : ToState(cells[index]);
        }

        public bool IsVisible(Vector2 point)
        {
            return StateAt(point) == FogState.Visible;
        }

        public void BeginUpdate()
        {
            (previous, visible) = (visible, previous);
            visible.Clear();
            isChanged = false;
            foreach (int index in previous)
            {
                cells[index] = Fading;
            }
        }

        /// <summary>Makes every cell from the origin along the direction Visible, up to the distance.</summary>
        /// <param name="direction">Normalized.</param>
        public void RevealRay(Vector2 origin, Vector2 direction, float distance)
        {
            // Half-cell steps never skip a cell the ray crosses through its middle.
            float step = CellSize * 0.5f;
            int steps = Math.Max(0, (int)Math.Ceiling(distance / step));
            for (int i = 0; i <= steps; i++)
            {
                float along = Math.Min(i * step, distance);
                int index = IndexOf(origin + direction * along);
                if (index >= 0)
                {
                    MarkVisible(index);
                }
            }
        }

        public void EndUpdate()
        {
            foreach (int index in previous)
            {
                if (cells[index] == Fading)
                {
                    cells[index] = (byte)FogState.Explored;
                    isChanged = true;
                }
            }

            previous.Clear();
            if (isChanged)
            {
                Version++;
            }
        }

        /// <summary>Every cell not in sight now becomes Explored, such as when the player buys the map's intel.</summary>
        public void RevealAll()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == (byte)FogState.Unexplored)
                {
                    cells[i] = (byte)FogState.Explored;
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                Version++;
                isChanged = false;
            }
        }

        private void MarkVisible(int index)
        {
            byte cell = cells[index];
            if (cell == (byte)FogState.Visible)
            {
                return;
            }

            if (cell != Fading)
            {
                isChanged = true;
            }

            cells[index] = (byte)FogState.Visible;
            visible.Add(index);
        }

        private int IndexOf(Vector2 point)
        {
            float x = (point.x - Area.xMin) / CellSize;
            float y = (point.y - Area.yMin) / CellSize;
            if (x < 0f || y < 0f)
            {
                return -1;
            }

            int column = (int)x;
            int row = (int)y;
            return column < Columns && row < Rows ? row * Columns + column : -1;
        }

        private static FogState ToState(byte cell)
        {
            return cell == Fading ? FogState.Explored : (FogState)cell;
        }
    }
}
