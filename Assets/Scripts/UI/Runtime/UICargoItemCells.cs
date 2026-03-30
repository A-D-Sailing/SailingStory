using System.Collections.Generic;
using UnityEngine.UIElements;
using UI.Runtime.CustomControl;

namespace UI.Runtime
{
    /// <summary>
    /// Static registry providing item information without creating cell instances.
    /// </summary>
    public static class UICargoItemRegistry
    {
        private static readonly Dictionary<UICargoItemType, (int width, int height, int goldValue)> _itemInfo = new()
        {
            { UICargoItemType.Toolkit, (1, 1, 50) },
            { UICargoItemType.Plank, (2, 1, 100) },
            { UICargoItemType.Shipwright, (1, 2, 75) },
            { UICargoItemType.Crate, (1, 1, 10) }
        };

        /// <summary>
        /// Gets item dimensions and gold value for a given item type.
        /// </summary>
        public static (int width, int height, int goldValue) GetItemInfo(UICargoItemType itemType)
        {
            return _itemInfo.TryGetValue(itemType, out var info) ? info : (1, 1, 0);
        }

        /// <summary>
        /// Creates a cargo item cell for the specified item type.
        /// </summary>
        public static UICargoItemCell CreateCell(UICargoItemType itemType)
        {
            return itemType switch
            {
                UICargoItemType.Toolkit => new ToolkitCell(),
                UICargoItemType.Plank => new PlankCell(),
                UICargoItemType.Shipwright => new ShipwrightCell(),
                UICargoItemType.Crate => new CrateCell(),
                _ => null
            };
        }
    }

    /// <summary>
    /// Base class for cargo item cells with specific item types.
    /// </summary>
    public abstract class UICargoItemCell : UIIconGridCell
    {
        public abstract UICargoItemType ItemType { get; }
        
        public int Width => UICargoItemRegistry.GetItemInfo(ItemType).width;
        public int Height => UICargoItemRegistry.GetItemInfo(ItemType).height;
        public int GoldValue => UICargoItemRegistry.GetItemInfo(ItemType).goldValue;

        protected UICargoItemCell()
        {
            AddToClassList("cargo-item");
        }

        protected void InitializeDimensions()
        {
            CellWidth = Width;
            CellHeight = Height;
        }
    }

    /// <summary>
    /// Toolkit item cell (1x1, 50 gold)
    /// </summary>
    [UxmlElement]
    public partial class ToolkitCell : UICargoItemCell
    {
        public override UICargoItemType ItemType => UICargoItemType.Toolkit;

        public ToolkitCell()
        {
            AddToClassList("toolkit-cell");
            InitializeDimensions();
        }
    }

    /// <summary>
    /// Plank item cell (1x2, 100 gold)
    /// </summary>
    [UxmlElement]
    public partial class PlankCell : UICargoItemCell
    {
        public override UICargoItemType ItemType => UICargoItemType.Plank;

        public PlankCell()
        {
            AddToClassList("plank-cell");
            InitializeDimensions();
        }
    }

    /// <summary>
    /// Shipwright item cell (2x1, 75 gold)
    /// </summary>
    [UxmlElement]
    public partial class ShipwrightCell : UICargoItemCell
    {
        public override UICargoItemType ItemType => UICargoItemType.Shipwright;

        public ShipwrightCell()
        {
            AddToClassList("shipwright-cell");
            InitializeDimensions();
        }
    }

    /// <summary>
    /// Crate item cell (1x1, 10 gold)
    /// </summary>
    [UxmlElement]
    public partial class CrateCell : UICargoItemCell
    {
        public override UICargoItemType ItemType => UICargoItemType.Crate;

        public CrateCell()
        {
            AddToClassList("crate-cell");
            InitializeDimensions();
        }
    }
}
