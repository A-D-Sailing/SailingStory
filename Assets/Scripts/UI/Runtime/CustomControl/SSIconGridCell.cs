using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime.CustomControl
{
    /// <summary>
    /// A custom UI Toolkit element that displays an icon image within a grid cell.
    /// Supports multi-cell spanning via CellWidth and CellHeight properties.
    /// Designed to be used with <see cref="SSGridContainer"/>.
    /// </summary>
    [UxmlElement]
    public partial class SSIconGridCell : VisualElement
    {
        private Image _icon;
        private Sprite _iconSprite;

        /// <summary>
        /// Grid X position (column index).
        /// </summary>
        public int GridX { get; set; }

        /// <summary>
        /// Grid Y position (row index).
        /// </summary>
        public int GridY { get; set; }

        /// <summary>
        /// Number of columns this cell spans.
        /// </summary>
        public int CellWidth { get; set; } = 1;

        /// <summary>
        /// Number of rows this cell spans.
        /// </summary>
        public int CellHeight { get; set; } = 1;

        /// <summary>
        /// The sprite to display as the icon.
        /// </summary>
        [UxmlAttribute]
        public Sprite iconSprite
        {
            get => _iconSprite;
            set
            {
                _iconSprite = value;
                if (_icon != null)
                {
                    _icon.sprite = value;
                }
            }
        }

        public SSIconGridCell()
        {
            InitializeCell();
        }

        public SSIconGridCell(Sprite icon)
        {
            InitializeCell();
            SetIcon(icon);
        }

        private void InitializeCell()
        {
            AddToClassList("icon-grid-cell");

            _icon = new Image { name = "icon" };
            _icon.AddToClassList("icon-grid-icon");
            Add(_icon);
        }

        public void SetIcon(Sprite icon)
        {
            _iconSprite = icon;
            if (_icon != null)
            {
                _icon.sprite = icon;
            }
        }
    }
}
