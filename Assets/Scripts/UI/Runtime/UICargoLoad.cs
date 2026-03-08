using System.Collections.Generic;
using UI.Runtime.CustomControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Runtime
{
    public enum CargoUIState
    {
        Load,   // Player buying from market to cargo
        Unload  // Player selling from cargo to market
    }

    /// <summary>
    /// Manages the cargo map UI, allowing transfer of items between cargo and market.
    /// Supports Load state (buying) and Unload state (selling).
    /// </summary>
    public class UICargoLoad : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int columns = 5;
        [SerializeField] private int cargoRows = 7;
        [SerializeField] private int marketRows = 5;

        private VisualElement _rootElement;
        private UIGridContainer _cargoContainer;
        private UIGridContainer _marketContainer;
        private Label _goldLabel;
        private Label _mainTitle;
        private Button _settleDepartBtn;

        private UICargoItemType[,] _cargoGrid;
        private UICargoItemType[,] _marketGrid;

        private CargoUIState _currentState = CargoUIState.Load;
        private int _earnedGold = 0;
        private bool _initialized = false;

        [Header("Boat Behavior")]
        public BoatController boatController;
        public Transform undockTarget;

        public CargoUIState CurrentState => _currentState;
        public int EarnedGold => _earnedGold;
        public int CargoValue => CalculateCargoValue();

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            _rootElement = uiDocument.rootVisualElement;

            _cargoContainer = _rootElement.Q<UIGridContainer>("cargo-container");
            _marketContainer = _rootElement.Q<UIGridContainer>("market-container");
            _goldLabel = _rootElement.Q<Label>("gold-label");
            _mainTitle = _rootElement.Q<Label>("main-title");
            _settleDepartBtn = _rootElement.Q<Button>("settle-depart-btn");

            InitializeGridData();
            InitializeContainers();
            RegisterCallbacks();
            RegisterButtonCallbacks();
            
            _initialized = true;
        }

        void OnEnable()
        {
            if (_initialized)
            {
                Show();
            }
        }

        void Start()
        {
            Hide();
        }

        /// <summary>
        /// Opens the UI in Load state (player buying from market).
        /// </summary>
        public void OpenLoadState()
        {
            _currentState = CargoUIState.Load;
            _earnedGold = 0;
            ClearAllGrids();
            PopulateMarketItems();
            ShowAndRefresh();
        }

        /// <summary>
        /// Opens the UI in Unload state (player selling to market).
        /// Cargo items are preserved from previous session.
        /// </summary>
        public void OpenUnloadState()
        {
            _currentState = CargoUIState.Unload;
            _earnedGold = 0;
            ClearMarketGrid();
            ShowAndRefresh();
        }

        /// <summary>
        /// Opens the UI in Unload state with specific cargo items.
        /// </summary>
        public void OpenUnloadState(List<UICargoItemType> cargoItems)
        {
            _currentState = CargoUIState.Unload;
            _earnedGold = 0;
            ClearAllGrids();
            
            foreach (var itemType in cargoItems)
            {
                TryPlaceItemAnywhere(_cargoContainer, _cargoGrid, itemType);
            }
            
            ShowAndRefresh();
        }

        /// <summary>
        /// Shows UI and refreshes display.
        /// </summary>
        private void ShowAndRefresh()
        {
            _rootElement.style.display = DisplayStyle.Flex;
            UpdateUIForState();
            _cargoContainer?.UpdateLayout();
            _marketContainer?.UpdateLayout();
        }

        public void Hide()
        {
            _rootElement.style.display = DisplayStyle.None;
        }

        public void Show()
        {
            _rootElement.style.display = DisplayStyle.Flex;
            
            // Initialize based on current state if needed
            if (_currentState == CargoUIState.Load && _marketContainer.childCount == 0)
            {
                ClearAllGrids();
                PopulateMarketItems();
            }
            
            UpdateUIForState();
            _cargoContainer?.UpdateLayout();
            _marketContainer?.UpdateLayout();
        }

        private void ClearAllGrids()
        {
            ClearCargoGrid();
            ClearMarketGrid();
        }

        private void ClearCargoGrid()
        {
            _cargoContainer?.ClearAll();
            _cargoGrid = new UICargoItemType[columns, cargoRows];
        }

        private void ClearMarketGrid()
        {
            _marketContainer?.ClearAll();
            _marketGrid = new UICargoItemType[columns, marketRows];
        }

        private void UpdateUIForState()
        {
            if (_currentState == CargoUIState.Load)
            {
                if (_mainTitle != null) _mainTitle.text = "Loading Cargo";
                if (_settleDepartBtn != null) _settleDepartBtn.text = "Load & Depart";
                UpdateGoldDisplayLoad();
            }
            else
            {
                if (_mainTitle != null) _mainTitle.text = "Unloading Cargo";
                if (_settleDepartBtn != null) _settleDepartBtn.text = "Settle & Depart";
                UpdateGoldDisplayUnload();
            }
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

            // Row 0: Crate, Plank (2x1), Crate, Crate
            PlaceItemInMarket(UICargoItemType.Crate, 0, 0);
            PlaceItemInMarket(UICargoItemType.Plank, 1, 0);
            PlaceItemInMarket(UICargoItemType.Crate, 3, 0);
            PlaceItemInMarket(UICargoItemType.Crate, 4, 0);

            // Row 1: Shipwright (1x2), Toolkit, Crate, Shipwright (1x2), Toolkit
            PlaceItemInMarket(UICargoItemType.Shipwright, 0, 1);
            PlaceItemInMarket(UICargoItemType.Toolkit, 1, 1);
            PlaceItemInMarket(UICargoItemType.Crate, 2, 1);
            PlaceItemInMarket(UICargoItemType.Shipwright, 3, 1);
            PlaceItemInMarket(UICargoItemType.Toolkit, 4, 1);

            // Row 2: (Shipwright continues), Plank (2x1), (Shipwright continues), Crate
            PlaceItemInMarket(UICargoItemType.Plank, 1, 2);
            PlaceItemInMarket(UICargoItemType.Crate, 4, 2);

            // Row 3: Toolkit, Crate, Shipwright (1x2), Plank (2x1)
            PlaceItemInMarket(UICargoItemType.Toolkit, 0, 3);
            PlaceItemInMarket(UICargoItemType.Crate, 1, 3);
            PlaceItemInMarket(UICargoItemType.Shipwright, 2, 3);
            PlaceItemInMarket(UICargoItemType.Plank, 3, 3);

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

        private bool PlaceItem(UIGridContainer container, UICargoItemType[,] grid, UICargoItemType itemType, int x, int y)
        {
            var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);

            if (!CanPlaceItem(grid, x, y, width, height)) return false;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    grid[x + dx, y + dy] = itemType;
                }
            }

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
            var (primaryX, primaryY) = FindPrimaryCell(grid, x, y, itemType);

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    grid[primaryX + dx, primaryY + dy] = UICargoItemType.None;
                }
            }

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
                _settleDepartBtn.clicked += OnSettleDepartClicked;
            }
        }

        private void OnSettleDepartClicked()
        {
            if (_currentState == CargoUIState.Load)
            {
                // Player loaded cargo and is departing
                ClearMarketGrid();
                _currentState = CargoUIState.Unload;
            }
            else
            {
                // Player unloaded cargo and is departing
                ClearAllGrids();
                _currentState = CargoUIState.Load;
            }
            
            boatController?.TransitionToUndocking(undockTarget);
            Hide();
        }

        private void OnCargoContainerClicked(int gridX, int gridY, UIIconGridCell cell)
        {
            if (cell == null || !(cell is UICargoItemCell)) return;

            var itemType = _cargoGrid[gridX, gridY];
            if (itemType == UICargoItemType.None) return;

            if (_currentState == CargoUIState.Load)
            {
                if (TryPlaceItemAnywhere(_marketContainer, _marketGrid, itemType))
                {
                    RemoveItem(_cargoContainer, _cargoGrid, gridX, gridY);
                    UpdateGoldDisplayLoad();
                }
            }
            else
            {
                var (_, _, goldValue) = UICargoItemRegistry.GetItemInfo(itemType);
                if (TryPlaceItemAnywhere(_marketContainer, _marketGrid, itemType))
                {
                    RemoveItem(_cargoContainer, _cargoGrid, gridX, gridY);
                    _earnedGold += goldValue;
                    UpdateGoldDisplayUnload();
                }
            }
        }

        private void OnMarketContainerClicked(int gridX, int gridY, UIIconGridCell cell)
        {
            if (cell == null || !(cell is UICargoItemCell)) return;

            var itemType = _marketGrid[gridX, gridY];
            if (itemType == UICargoItemType.None) return;

            if (_currentState == CargoUIState.Load)
            {
                if (TryPlaceItemAnywhere(_cargoContainer, _cargoGrid, itemType))
                {
                    RemoveItem(_marketContainer, _marketGrid, gridX, gridY);
                    UpdateGoldDisplayLoad();
                }
            }
            else
            {
                var (_, _, goldValue) = UICargoItemRegistry.GetItemInfo(itemType);
                if (TryPlaceItemAnywhere(_cargoContainer, _cargoGrid, itemType))
                {
                    RemoveItem(_marketContainer, _marketGrid, gridX, gridY);
                    _earnedGold -= goldValue;
                    if (_earnedGold < 0) _earnedGold = 0;
                    UpdateGoldDisplayUnload();
                }
            }
        }

        private int CalculateCargoValue()
        {
            int totalValue = 0;
            int gridWidth = _cargoGrid.GetLength(0);
            int gridHeight = _cargoGrid.GetLength(1);
            var visited = new bool[gridWidth, gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (visited[x, y]) continue;
                    
                    var itemType = _cargoGrid[x, y];
                    if (itemType == UICargoItemType.None) continue;

                    var (width, height, goldValue) = UICargoItemRegistry.GetItemInfo(itemType);
                    for (int dy = 0; dy < height && y + dy < gridHeight; dy++)
                    {
                        for (int dx = 0; dx < width && x + dx < gridWidth; dx++)
                        {
                            visited[x + dx, y + dy] = true;
                        }
                    }

                    totalValue += goldValue;
                }
            }

            return totalValue;
        }

        private void UpdateGoldDisplayLoad()
        {
            if (_goldLabel != null)
            {
                _goldLabel.text = $"Estimate Gold: {CalculateCargoValue()}";
            }
        }

        private void UpdateGoldDisplayUnload()
        {
            if (_goldLabel != null)
            {
                _goldLabel.text = $"Earned Gold: {_earnedGold}";
            }
        }

        /// <summary>
        /// Gets list of all cargo item types currently in cargo.
        /// </summary>
        public List<UICargoItemType> GetCargoItems()
        {
            var items = new List<UICargoItemType>();
            int gridWidth = _cargoGrid.GetLength(0);
            int gridHeight = _cargoGrid.GetLength(1);
            var visited = new bool[gridWidth, gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (visited[x, y]) continue;
                    
                    var itemType = _cargoGrid[x, y];
                    if (itemType == UICargoItemType.None) continue;

                    var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);
                    for (int dy = 0; dy < height && y + dy < gridHeight; dy++)
                    {
                        for (int dx = 0; dx < width && x + dx < gridWidth; dx++)
                        {
                            visited[x + dx, y + dy] = true;
                        }
                    }

                    items.Add(itemType);
                }
            }

            return items;
        }
    }
}
