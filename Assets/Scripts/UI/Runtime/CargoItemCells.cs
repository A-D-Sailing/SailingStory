using System.Collections.Generic;
using UnityEngine.UIElements;
using UI.Runtime.CustomControl;

namespace UI.Runtime
{
    /// <summary>
    /// Static registry providing item information without creating cell instances.
    /// </summary>
    public static class CargoItemRegistry
    {
        private static readonly Dictionary<CargoItemType, (int width, int height, int goldValue)> _itemInfo = new()
        {
            { CargoItemType.Toolkit, (1, 1, 50) },
            { CargoItemType.Plank, (2, 1, 100) },
            { CargoItemType.Shipwright, (1, 2, 75) },
            { CargoItemType.Crate, (1, 1, 10) }
        };

        /// <summary>
        /// Gets item dimensions and gold value for a given item type.
        /// </summary>
        public static (int width, int height, int goldValue) GetItemInfo(CargoItemType itemType)
        {
            return _itemInfo.TryGetValue(itemType, out var info) ? info : (1, 1, 0);
        }

        /// <summary>
        /// Creates a cargo item cell for the specified item type.
        /// </summary>
        public static CargoItemCell CreateCell(CargoItemType itemType)
        {
            return itemType switch
            {
                CargoItemType.Toolkit => new ToolkitCell(),
                CargoItemType.Plank => new PlankCell(),
                CargoItemType.Shipwright => new ShipwrightCell(),
                CargoItemType.Crate => new CrateCell(),
                _ => null
            };
        }
    }

    /// <summary>
    /// Base class for cargo item cells with specific item types.
    /// </summary>
    public abstract class CargoItemCell : SSIconGridCell
    {
        public abstract CargoItemType ItemType { get; }
        
        public int Width => CargoItemRegistry.GetItemInfo(ItemType).width;
        public int Height => CargoItemRegistry.GetItemInfo(ItemType).height;
        public int GoldValue => CargoItemRegistry.GetItemInfo(ItemType).goldValue;

        protected CargoItemCell()
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
    public partial class ToolkitCell : CargoItemCell
    {
        public override CargoItemType ItemType => CargoItemType.Toolkit;

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
    public partial class PlankCell : CargoItemCell
    {
        public override CargoItemType ItemType => CargoItemType.Plank;

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
    public partial class ShipwrightCell : CargoItemCell
    {
        public override CargoItemType ItemType => CargoItemType.Shipwright;

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
    public partial class CrateCell : CargoItemCell
    {
        public override CargoItemType ItemType => CargoItemType.Crate;

        public CrateCell()
        {
            AddToClassList("crate-cell");
            InitializeDimensions();
        }
    }
}
