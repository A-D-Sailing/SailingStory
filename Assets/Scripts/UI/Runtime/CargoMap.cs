using System;
using System.Collections.Generic;
using UI.Runtime.CustomControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime
{
    /// <summary>
    /// Manages the cargo map UI, allowing transfer of items between cargo and market.
    /// </summary>
    public class CargoMap : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int columns = 5;
        [SerializeField] private int cargoRows = 7;
        [SerializeField] private int marketRows = 5;

        private SSGridContainer _cargoContainer;
        private SSGridContainer _marketContainer;
        private Label _goldLabel;
        private Button _settleDepartBtn;

        private CargoItemType[,] _cargoGrid;
        private CargoItemType[,] _marketGrid;

        public event Action OnSettleAndDepart;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            _cargoContainer = root.Q<SSGridContainer>("cargo-container");
            _marketContainer = root.Q<SSGridContainer>("market-container");
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
            _cargoGrid = new CargoItemType[columns, cargoRows];
            _marketGrid = new CargoItemType[columns, marketRows];
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
            PlaceItemInMarket(CargoItemType.Crate, 0, 0);
            PlaceItemInMarket(CargoItemType.Plank, 1, 0);    // spans (1,0)-(2,0)
            PlaceItemInMarket(CargoItemType.Crate, 3, 0);
            PlaceItemInMarket(CargoItemType.Crate, 4, 0);

            // Row 1: Shipwright (1x2), Toolkit, Crate, Shipwright (1x2), Toolkit
            PlaceItemInMarket(CargoItemType.Shipwright, 0, 1);  // spans (0,1)-(0,2)
            PlaceItemInMarket(CargoItemType.Toolkit, 1, 1);
            PlaceItemInMarket(CargoItemType.Crate, 2, 1);
            PlaceItemInMarket(CargoItemType.Shipwright, 3, 1);  // spans (3,1)-(3,2)
            PlaceItemInMarket(CargoItemType.Toolkit, 4, 1);

            // Row 2: (Shipwright continues), Plank (2x1), (Shipwright continues), Crate
            PlaceItemInMarket(CargoItemType.Plank, 1, 2);    // spans (1,2)-(2,2)
            PlaceItemInMarket(CargoItemType.Crate, 4, 2);

            // Row 3: Toolkit, Crate, Shipwright (1x2), Plank (2x1)
            PlaceItemInMarket(CargoItemType.Toolkit, 0, 3);
            PlaceItemInMarket(CargoItemType.Crate, 1, 3);
            PlaceItemInMarket(CargoItemType.Shipwright, 2, 3);  // spans (2,3)-(2,4)
            PlaceItemInMarket(CargoItemType.Plank, 3, 3);    // spans (3,3)-(4,3)

            // Row 4: Crate, Toolkit, (Shipwright continues), Toolkit, Crate
            PlaceItemInMarket(CargoItemType.Crate, 0, 4);
            PlaceItemInMarket(CargoItemType.Toolkit, 1, 4);
            PlaceItemInMarket(CargoItemType.Toolkit, 3, 4);
            PlaceItemInMarket(CargoItemType.Crate, 4, 4);
        }

        private void PlaceItemInMarket(CargoItemType itemType, int x, int y)
        {
            PlaceItem(_marketContainer, _marketGrid, itemType, x, y);
        }

        private void PlaceItemInCargo(CargoItemType itemType, int x, int y)
        {
            PlaceItem(_cargoContainer, _cargoGrid, itemType, x, y);
        }

        private bool PlaceItem(SSGridContainer container, CargoItemType[,] grid, CargoItemType itemType, int x, int y)
        {
            var (width, height, _) = CargoItemRegistry.GetItemInfo(itemType);

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
            var cell = CargoItemRegistry.CreateCell(itemType);
            if (cell != null)
            {
                container.PlaceCell(cell, x, y, width, height);
            }

            return true;
        }

        private bool CanPlaceItem(CargoItemType[,] grid, int x, int y, int width, int height)
        {
            int gridColumns = grid.GetLength(0);
            int gridRows = grid.GetLength(1);

            if (x + width > gridColumns || y + height > gridRows) return false;
            if (x < 0 || y < 0) return false;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    if (grid[x + dx, y + dy] != CargoItemType.None) return false;
                }
            }
            return true;
        }

        private void RemoveItem(SSGridContainer container, CargoItemType[,] grid, int x, int y)
        {
            var itemType = grid[x, y];
            if (itemType == CargoItemType.None) return;

            var (width, height, _) = CargoItemRegistry.GetItemInfo(itemType);

            // Find the primary (top-left) cell of this item
            var (primaryX, primaryY) = FindPrimaryCell(grid, x, y, itemType);

            // Clear grid cells
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    grid[primaryX + dx, primaryY + dy] = CargoItemType.None;
                }
            }

            // Remove the visual cell
            var cell = container.FindCellAt(primaryX, primaryY);
            if (cell != null)
            {
                container.RemoveCell(cell);
            }
        }

        private (int x, int y) FindPrimaryCell(CargoItemType[,] grid, int startX, int startY, CargoItemType itemType)
        {
            int x = startX;
            int y = startY;

            while (x > 0 && grid[x - 1, y] == itemType) x--;
            while (y > 0 && grid[x, y - 1] == itemType) y--;

            return (x, y);
        }

        private bool TryPlaceItemAnywhere(SSGridContainer container, CargoItemType[,] grid, CargoItemType itemType)
        {
            var (width, height, _) = CargoItemRegistry.GetItemInfo(itemType);
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
                _settleDepartBtn.clicked += () => OnSettleAndDepart?.Invoke();
            }
        }

        private void OnCargoContainerClicked(int gridX, int gridY, SSIconGridCell cell)
        {
            if (cell == null || !(cell is CargoItemCell)) return;

            var itemType = _cargoGrid[gridX, gridY];
            if (itemType == CargoItemType.None) return;

            if (TryPlaceItemAnywhere(_marketContainer, _marketGrid, itemType))
            {
                RemoveItem(_cargoContainer, _cargoGrid, gridX, gridY);
                UpdateGoldDisplay();
            }
        }

        private void OnMarketContainerClicked(int gridX, int gridY, SSIconGridCell cell)
        {
            if (cell == null || !(cell is CargoItemCell)) return;

            var itemType = _marketGrid[gridX, gridY];
            if (itemType == CargoItemType.None) return;

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
                    if (itemType == CargoItemType.None) continue;

                    var primary = FindPrimaryCell(_cargoGrid, x, y, itemType);
                    if (counted.Contains(primary)) continue;

                    counted.Add(primary);
                    var (_, _, goldValue) = CargoItemRegistry.GetItemInfo(itemType);
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
                _goldLabel.text = $"Gold: {cargoValue}";
            }
        }

        public int CargoValue => CalculateCargoValue();
    }
}
