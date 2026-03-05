using System.Collections.Generic;
using UI.Runtime.CustomControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime
{
    /// <summary>
    /// Manages the cargo map UI, allowing transfer of items between cargo and market.
    /// </summary>
    public class UICargoMap : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int columns = 5;
        [SerializeField] private int cargoRows = 7;
        [SerializeField] private int marketRows = 5;

        private UIGridContainer _cargoContainer;
        private UIGridContainer _marketContainer;
        private Label _goldLabel;
        private Button _settleDepartBtn;

        private UICargoItemType[,] _cargoGrid;
        private UICargoItemType[,] _marketGrid;
        
        [Header("Boat Behavior")]
        public BoatController boatController;

        public Transform undockTarget;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            _cargoContainer = root.Q<UIGridContainer>("cargo-container");
            _marketContainer = root.Q<UIGridContainer>("market-container");
            _goldLabel = root.Q<Label>("gold-label");
            _settleDepartBtn = root.Q<Button>("settle-depart-btn");

            InitializeGridData();
            InitializeContainers();
            RegisterCallbacks();
            RegisterButtonCallbacks();
            
            PopulateMarketItems();
            UpdateGoldDisplay();
        }

        private void InitializeGridData()
        {
            _cargoGrid = new UICargoItemType[columns, cargoRows];
            _marketGrid = new UICargoItemType[columns, marketRows];
        }

        private void InitializeContainers()
        {
            if (_cargoContainer != null)
            {
                _cargoContainer.Columns = columns;
                _cargoContainer.Rows = cargoRows;
            }

            if (_marketContainer != null)
            {
                _marketContainer.Columns = columns;
                _marketContainer.Rows = marketRows;
            }
        }

        private void PopulateMarketItems()
        {
            if (_marketContainer == null) return;

            // Item sizes: Toolkit(1x1), Plank(2x1), Shipwright(1x2), Crate(1x1)

            // Row 0: Crate, Plank (2x1), Crate, Crate
            PlaceItemInMarket(UICargoItemType.Crate, 0, 0);
            PlaceItemInMarket(UICargoItemType.Plank, 1, 0);    // spans (1,0)-(2,0)
            PlaceItemInMarket(UICargoItemType.Crate, 3, 0);
            PlaceItemInMarket(UICargoItemType.Crate, 4, 0);

            // Row 1: Shipwright (1x2), Toolkit, Crate, Shipwright (1x2), Toolkit
            PlaceItemInMarket(UICargoItemType.Shipwright, 0, 1);  // spans (0,1)-(0,2)
            PlaceItemInMarket(UICargoItemType.Toolkit, 1, 1);
            PlaceItemInMarket(UICargoItemType.Crate, 2, 1);
            PlaceItemInMarket(UICargoItemType.Shipwright, 3, 1);  // spans (3,1)-(3,2)
            PlaceItemInMarket(UICargoItemType.Toolkit, 4, 1);

            // Row 2: (Shipwright continues), Plank (2x1), (Shipwright continues), Crate
            PlaceItemInMarket(UICargoItemType.Plank, 1, 2);    // spans (1,2)-(2,2)
            PlaceItemInMarket(UICargoItemType.Crate, 4, 2);

            // Row 3: Toolkit, Crate, Shipwright (1x2), Plank (2x1)
            PlaceItemInMarket(UICargoItemType.Toolkit, 0, 3);
            PlaceItemInMarket(UICargoItemType.Crate, 1, 3);
            PlaceItemInMarket(UICargoItemType.Shipwright, 2, 3);  // spans (2,3)-(2,4)
            PlaceItemInMarket(UICargoItemType.Plank, 3, 3);    // spans (3,3)-(4,3)

            // Row 4: Crate, Toolkit, (Shipwright continues), Toolkit, Crate
            PlaceItemInMarket(UICargoItemType.Crate, 0, 4);
            PlaceItemInMarket(UICargoItemType.Toolkit, 1, 4);
            PlaceItemInMarket(UICargoItemType.Toolkit, 3, 4);
            PlaceItemInMarket(UICargoItemType.Crate, 4, 4);
        }

        private void PlaceItemInMarket(UICargoItemType itemType, int x, int y)
        {
            PlaceItem(_marketContainer, _marketGrid, itemType, x, y);
        }

        private void PlaceItemInCargo(UICargoItemType itemType, int x, int y)
        {
            PlaceItem(_cargoContainer, _cargoGrid, itemType, x, y);
        }

        private bool PlaceItem(UIGridContainer container, UICargoItemType[,] grid, UICargoItemType itemType, int x, int y)
        {
            var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);

            if (!CanPlaceItem(grid, x, y, width, height)) return false;

            // Mark grid cells as occupied
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    grid[x + dx, y + dy] = itemType;
                }
            }

            // Create and place the visual cell
            var cell = UICargoItemRegistry.CreateCell(itemType);
            if (cell != null)
            {
                container.PlaceCell(cell, x, y, width, height);
            }

            return true;
        }

        private bool CanPlaceItem(UICargoItemType[,] grid, int x, int y, int width, int height)
        {
            int gridColumns = grid.GetLength(0);
            int gridRows = grid.GetLength(1);

            if (x + width > gridColumns || y + height > gridRows) return false;
            if (x < 0 || y < 0) return false;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    if (grid[x + dx, y + dy] != UICargoItemType.None) return false;
                }
            }
            return true;
        }

        private void RemoveItem(UIGridContainer container, UICargoItemType[,] grid, int x, int y)
        {
            var itemType = grid[x, y];
            if (itemType == UICargoItemType.None) return;

            var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);

            // Find the primary (top-left) cell of this item
            var (primaryX, primaryY) = FindPrimaryCell(grid, x, y, itemType);

            // Clear grid cells
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    grid[primaryX + dx, primaryY + dy] = UICargoItemType.None;
                }
            }

            // Remove the visual cell
            var cell = container.FindCellAt(primaryX, primaryY);
            if (cell != null)
            {
                container.RemoveCell(cell);
            }
        }

        private (int x, int y) FindPrimaryCell(UICargoItemType[,] grid, int startX, int startY, UICargoItemType itemType)
        {
            int x = startX;
            int y = startY;

            while (x > 0 && grid[x - 1, y] == itemType) x--;
            while (y > 0 && grid[x, y - 1] == itemType) y--;

            return (x, y);
        }

        private bool TryPlaceItemAnywhere(UIGridContainer container, UICargoItemType[,] grid, UICargoItemType itemType)
        {
            var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);
            int gridColumns = grid.GetLength(0);
            int gridRows = grid.GetLength(1);

            for (int y = 0; y < gridRows; y++)
            {
                for (int x = 0; x < gridColumns; x++)
                {
                    if (CanPlaceItem(grid, x, y, width, height))
                    {
                        PlaceItem(container, grid, itemType, x, y);
                        return true;
                    }
                }
            }
            return false;
        }

        private void RegisterCallbacks()
        {
            if (_cargoContainer != null)
            {
                _cargoContainer.onCellClicked += OnCargoContainerClicked;
            }

            if (_marketContainer != null)
            {
                _marketContainer.onCellClicked += OnMarketContainerClicked;
            }
        }

        private void RegisterButtonCallbacks()
        {
            if (_settleDepartBtn != null)
            {
                _settleDepartBtn.clicked += () =>
                {
                    boatController?.TransitionToUndocking(undockTarget);
                    gameObject.SetActive(false);
                };
                
            }
        }

        private void OnCargoContainerClicked(int gridX, int gridY, UIIconGridCell cell)
        {
            if (cell == null || !(cell is UICargoItemCell)) return;

            var itemType = _cargoGrid[gridX, gridY];
            if (itemType == UICargoItemType.None) return;

            if (TryPlaceItemAnywhere(_marketContainer, _marketGrid, itemType))
            {
                RemoveItem(_cargoContainer, _cargoGrid, gridX, gridY);
                UpdateGoldDisplay();
            }
        }

        private void OnMarketContainerClicked(int gridX, int gridY, UIIconGridCell cell)
        {
            if (cell == null || !(cell is UICargoItemCell)) return;

            var itemType = _marketGrid[gridX, gridY];
            if (itemType == UICargoItemType.None) return;

            if (TryPlaceItemAnywhere(_cargoContainer, _cargoGrid, itemType))
            {
                RemoveItem(_marketContainer, _marketGrid, gridX, gridY);
                UpdateGoldDisplay();
            }
        }

        private int CalculateCargoValue()
        {
            int totalValue = 0;
            var counted = new HashSet<(int, int)>();

            for (int y = 0; y < _cargoGrid.GetLength(1); y++)
            {
                for (int x = 0; x < _cargoGrid.GetLength(0); x++)
                {
                    var itemType = _cargoGrid[x, y];
                    if (itemType == UICargoItemType.None) continue;

                    var primary = FindPrimaryCell(_cargoGrid, x, y, itemType);
                    if (counted.Contains(primary)) continue;

                    counted.Add(primary);
                    var (_, _, goldValue) = UICargoItemRegistry.GetItemInfo(itemType);
                    totalValue += goldValue;
                }
            }

            return totalValue;
        }

        private void UpdateGoldDisplay()
        {
            if (_goldLabel != null)
            {
                int cargoValue = CalculateCargoValue();
                _goldLabel.text = $"Estimate Gold: {cargoValue}";
            }
        }

        public int CargoValue => CalculateCargoValue();
    }
}
