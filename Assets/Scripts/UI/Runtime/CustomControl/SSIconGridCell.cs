using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime.CustomControl
{
    /// <summary>
    /// A custom UI Toolkit element that displays an icon image within a grid cell.
    /// Designed to be used as a child of <see cref="SSGridContainer"/>.
    /// </summary>
    /// <remarks>
    /// Requires the IconGridCell.uss stylesheet to be loaded for proper styling.
    /// The icon is displayed using an Image element that scales to fit the cell.
    /// Click events are handled by the parent <see cref="SSGridContainer"/>.
    /// </remarks>
    [UxmlElement]
    public partial class SSIconGridCell : VisualElement
    {
        private Image _icon;
        private Texture2D _iconTexture;

        /// <summary>
        /// The texture to display as the icon.
        /// Can be set via UXML attribute or programmatically.
        /// </summary>
        [UxmlAttribute]
        public Texture2D iconTexture
        {
            get => _iconTexture;
            set
            {
                _iconTexture = value;
                if (_icon != null)
                {
                    _icon.image = value;
                }
            }
        }

        /// <summary>
        /// Creates a new IconGridCell without an icon.
        /// Use <see cref="SetIcon"/> to set the icon later.
        /// </summary>
        public SSIconGridCell()
        {
            InitializeCell();
        }

        /// <summary>
        /// Creates a new IconGridCell with the specified icon.
        /// </summary>
        /// <param name="icon">The texture to display as the icon.</param>
        public SSIconGridCell(Texture2D icon)
        {
            InitializeCell();
            SetIcon(icon);
        }

        /// <summary>
        /// Initializes the cell's visual structure and applies USS classes.
        /// Registers geometry change callback to maintain square aspect ratio.
        /// </summary>
        private void InitializeCell()
        {
            AddToClassList("icon-grid-cell");

            _icon = new Image();
            _icon.name = "icon";
            _icon.AddToClassList("icon-grid-icon");
            Add(_icon);

            // Register callback to maintain square aspect ratio
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>
        /// Called when the cell's geometry changes.
        /// Sets the height equal to the width to maintain a square shape.
        /// </summary>
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Set height equal to width to maintain square aspect ratio
            float width = resolvedStyle.width;
            if (!float.IsNaN(width) && width > 0)
            {
                style.height = width;
            }
        }

        /// <summary>
        /// Sets the icon texture to display.
        /// </summary>
        /// <param name="icon">The texture to display, or null to clear the icon.</param>
        public void SetIcon(Texture2D icon)
        {
            _iconTexture = icon;
            if (_icon != null)
            {
                _icon.image = icon;
            }
        }
    }
}
