using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime.CustomControl
{
    /// <summary>
    /// A custom UI Toolkit container that arranges its children in a grid layout.
    /// Children are displayed in rows with a configurable number of cells per row.
    /// Uses flexbox with wrap to achieve the grid behavior.
    /// Provides click callbacks with the index of the clicked cell.
    /// </summary>
    /// <remarks>
    /// Flexbox styles are applied in the constructor, no external USS required.
    /// Typically used with <see cref="SSIconGridCell"/> as children, but supports any VisualElement.
    /// </remarks>
    [UxmlElement]
    public partial class SSGridContainer : VisualElement
    {
        private int _cellPerRow = 4;

        /// <summary>
        /// Event fired when a SSIconGridCell is clicked.
        /// Parameters: (index, clicked cell)
        /// </summary>
        public event Action<int, SSIconGridCell> onCellClicked;

        /// <summary>
        /// The number of cells to display per row.
        /// Each child's width is calculated as (100% / cellPerRow).
        /// Minimum value is 1.
        /// </summary>
        [UxmlAttribute]
        public int CellPerRow
        {
            get => _cellPerRow;
            set
            {
                _cellPerRow = Mathf.Max(1, value);
                UpdateChildrenLayout();
            }
        }

        /// <summary>
        /// Creates a new GridContainer with default settings (4 cells per row).
        /// </summary>
        public SSGridContainer()
        {
            AddToClassList("grid-container");
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>
        /// Called when the container is attached to a panel. Updates children layout.
        /// </summary>
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UpdateChildrenLayout();
        }

        /// <summary>
        /// Called when the container's geometry changes. Updates children layout.
        /// </summary>
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateChildrenLayout();
        }

        /// <summary>
        /// Called when a click event occurs within the container.
        /// Only triggers the callback if the clicked element is a SSIconGridCell.
        /// </summary>
        private void OnClick(ClickEvent evt)
        {
            // Find which child was clicked
            var clickedElement = evt.target as VisualElement;
            
            // Walk up to find a SSIconGridCell
            var cell = FindIconGridCell(clickedElement);
            
            if (cell != null)
            {
                int index = IndexOf(cell);
                if (index >= 0)
                {
                    onCellClicked?.Invoke(index, cell);
                }
            }
        }

        /// <summary>
        /// Finds a SSIconGridCell in the hierarchy starting from the given element.
        /// </summary>
        /// <param name="element">The element to search from.</param>
        /// <returns>The SSIconGridCell if found, or null if not found.</returns>
        private SSIconGridCell FindIconGridCell(VisualElement element)
        {
            while (element != null && element != this)
            {
                if (element is SSIconGridCell cell && element.parent == this)
                {
                    return cell;
                }
                element = element.parent;
            }
            return null;
        }

        /// <summary>
        /// Updates the width of all children based on the current cellPerRow value.
        /// Call this method after modifying children directly without using Add().
        /// </summary>
        public void UpdateChildrenLayout()
        {
            if (_cellPerRow <= 0) return;

            float cellWidthPercent = 100f / _cellPerRow;

            foreach (var child in Children())
            {
                child.style.width = new Length(cellWidthPercent, LengthUnit.Percent);
            }
        }

        /// <summary>
        /// Adds a child element to the grid and automatically sets its width
        /// based on the current cellPerRow value.
        /// </summary>
        /// <param name="child">The child element to add.</param>
        public new void Add(VisualElement child)
        {
            base.Add(child);

            if (_cellPerRow > 0)
            {
                float cellWidthPercent = 100f / _cellPerRow;
                child.style.width = new Length(cellWidthPercent, LengthUnit.Percent);
            }
        }

        /// <summary>
        /// Gets the cell at the specified index.
        /// </summary>
        /// <param name="index">The index of the cell (0-based).</param>
        /// <returns>The cell at the index, or null if out of range.</returns>
        public VisualElement GetCellAt(int index)
        {
            if (index < 0 || index >= childCount)
            {
                return null;
            }
            return ElementAt(index);
        }

        /// <summary>
        /// Gets the cell at the specified index as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast the cell to.</typeparam>
        /// <param name="index">The index of the cell (0-based).</param>
        /// <returns>The cell at the index cast to T, or null if out of range or wrong type.</returns>
        public T GetCellAt<T>(int index) where T : VisualElement
        {
            return GetCellAt(index) as T;
        }
    }
}
