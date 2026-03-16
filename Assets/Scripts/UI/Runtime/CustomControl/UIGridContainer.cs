using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime.CustomControl
{
    /// <summary>
    /// A custom UI Toolkit container that arranges items in a grid layout using absolute positioning.
    /// Supports multi-cell items that can span multiple columns and/or rows.
    /// </summary>
    [UxmlElement]
    public partial class UIGridContainer : VisualElement
    {
        private int _columns = 5;
        private int _rows = 4;
        private float _cellSize;

        /// <summary>
        /// Event fired when a cell position is clicked.
        /// Parameters: (gridX, gridY, clicked cell or null if empty)
        /// </summary>
        public event Action<int, int, UIIconGridCell> onCellClicked;

        /// <summary>
        /// Number of columns in the grid.
        /// </summary>
        [UxmlAttribute]
        public int Columns
        {
            get => _columns;
            set
            {
                _columns = Mathf.Max(1, value);
                UpdateLayout();
            }
        }

        /// <summary>
        /// Number of rows in the grid.
        /// </summary>
        [UxmlAttribute]
        public int Rows
        {
            get => _rows;
            set
            {
                _rows = Mathf.Max(1, value);
                UpdateLayout();
            }
        }

        /// <summary>
        /// The calculated size of each cell in pixels.
        /// </summary>
        public float CellSize => _cellSize;

        public UIGridContainer()
        {
            AddToClassList("grid-container");

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<ClickEvent>(OnClick);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateLayout();
        }

        /// <summary>
        /// Updates the layout, calculating cell size and repositioning all items.
        /// </summary>
        public void UpdateLayout()
        {
            float containerWidth = resolvedStyle.width;
            float containerHeight = resolvedStyle.height;
            if (float.IsNaN(containerWidth) || containerWidth <= 0 || _columns <= 0) return;

            float sizeByWidth = containerWidth / _columns;
            if (_rows > 0 && !float.IsNaN(containerHeight) && containerHeight > 0)
            {
                float sizeByHeight = containerHeight / _rows;
                _cellSize = Mathf.Min(sizeByWidth, sizeByHeight);
            }
            else
            {
                _cellSize = sizeByWidth;
            }

            style.width = _cellSize * _columns;
            style.height = _cellSize * _rows;

            foreach (var child in Children())
            {
                if (child is UIIconGridCell cell)
                {
                    UpdateCellLayout(cell);
                }
            }
        }

        /// <summary>
        /// Updates a single cell's position and size based on its grid data.
        /// </summary>
        private void UpdateCellLayout(UIIconGridCell cell)
        {
            if (_cellSize <= 0) return;

            int gridX = cell.GridX;
            int gridY = cell.GridY;
            int cellWidth = cell.CellWidth;
            int cellHeight = cell.CellHeight;

            cell.style.position = Position.Absolute;
            cell.style.left = gridX * _cellSize;
            cell.style.top = gridY * _cellSize;
            cell.style.width = cellWidth * _cellSize;
            cell.style.height = cellHeight * _cellSize;
        }

        private void OnClick(ClickEvent evt)
        {
            if (_cellSize <= 0) return;

            var localPos = evt.localPosition;
            int gridX = Mathf.FloorToInt(localPos.x / _cellSize);
            int gridY = Mathf.FloorToInt(localPos.y / _cellSize);

            gridX = Mathf.Clamp(gridX, 0, _columns - 1);
            gridY = Mathf.Clamp(gridY, 0, _rows - 1);

            var clickedCell = FindCellAt(gridX, gridY);
            onCellClicked?.Invoke(gridX, gridY, clickedCell);
        }

        /// <summary>
        /// Finds the cell that occupies the given grid position.
        /// </summary>
        public UIIconGridCell FindCellAt(int gridX, int gridY)
        {
            foreach (var child in Children())
            {
                if (child is UIIconGridCell cell)
                {
                    if (gridX >= cell.GridX && gridX < cell.GridX + cell.CellWidth &&
                        gridY >= cell.GridY && gridY < cell.GridY + cell.CellHeight)
                    {
                        return cell;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Places a cell at the specified grid position.
        /// </summary>
        public void PlaceCell(UIIconGridCell cell, int gridX, int gridY, int cellWidth = 1, int cellHeight = 1)
        {
            cell.GridX = gridX;
            cell.GridY = gridY;
            cell.CellWidth = cellWidth;
            cell.CellHeight = cellHeight;

            if (!Contains(cell))
            {
                Add(cell);
            }

            UpdateCellLayout(cell);
        }

        /// <summary>
        /// Removes a cell from the grid.
        /// </summary>
        public void RemoveCell(UIIconGridCell cell)
        {
            if (Contains(cell))
            {
                Remove(cell);
            }
        }

        /// <summary>
        /// Clears all cells from the grid.
        /// </summary>
        public void ClearAll()
        {
            Clear();
        }
    }
}
