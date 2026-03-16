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
        private Button _fixHoldBtn;
        private VisualElement _popupContainer;
        private Label _popupLabel;

        private UICargoItemType[,] _cargoGrid;
        private UICargoItemType[,] _marketGrid;
        private bool[,] _damagedCells;

        private CargoUIState _currentState = CargoUIState.Load;
        private int _earnedGold = 0;
        [SerializeField] private int _playerGold = 0;
        [SerializeField] private int fixHoldCost = 50;
        private bool _repairMode = false;
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
            _fixHoldBtn = _rootElement.Q<Button>("fix-hold-btn");
            _popupContainer = _rootElement.Q<VisualElement>("popup-container");
            _popupLabel = _rootElement.Q<Label>("popup-label");

            // Always start current run with zero total gold.
            _playerGold = 0;

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
            ApplyDamageStylesToCargoItems();
            UpdateFixHoldButtonVisibility();
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
            ApplyDamageStylesToCargoItems();
            UpdateFixHoldButtonVisibility();
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
            RecreateCargoGridDamagedVisuals();
        }

        private void RecreateCargoGridDamagedVisuals()
        {
            if (_damagedCells == null) return;
            
            for (int y = 0; y < cargoRows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (_damagedCells[x, y])
                    {
                        CreateDamagedCellVisual(x, y);
                    }
                }
            }
        }

        private void ApplyDamageStylesToCargoItems()
        {
            if (_cargoContainer == null || _damagedCells == null) return;

            foreach (var child in _cargoContainer.Children())
            {
                if (child is not UICargoItemCell cargoCell) continue;

                bool isDamaged = false;
                for (int dy = 0; dy < cargoCell.CellHeight && cargoCell.GridY + dy < cargoRows; dy++)
                {
                    for (int dx = 0; dx < cargoCell.CellWidth && cargoCell.GridX + dx < columns; dx++)
                    {
                        if (_damagedCells[cargoCell.GridX + dx, cargoCell.GridY + dy])
                        {
                            isDamaged = true;
                            break;
                        }
                    }
                    if (isDamaged) break;
                }

                if (isDamaged) cargoCell.AddToClassList("damaged-cell");
                else cargoCell.RemoveFromClassList("damaged-cell");
            }
        }

        private bool HasDamagedCells()
        {
            if (_damagedCells == null) return false;

            for (int y = 0; y < cargoRows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (_damagedCells[x, y]) return true;
                }
            }

            return false;
        }

        private void UpdateFixHoldButtonVisibility()
        {
            if (_fixHoldBtn == null) return;

            bool hasDamage = HasDamagedCells();
            _fixHoldBtn.style.display = hasDamage ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasDamage)
            {
                _repairMode = false;
            }

            _fixHoldBtn.text = _repairMode ? "Cancel Fix Hold" : "Fix Hold";
            if (_repairMode) _fixHoldBtn.AddToClassList("repair-mode");
            else _fixHoldBtn.RemoveFromClassList("repair-mode");
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
            _damagedCells = new bool[columns, cargoRows];
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

        private bool CanPlaceItemInCargo(int x, int y, int width, int height)
        {
            int gridColumns = _cargoGrid.GetLength(0);
            int gridRows = _cargoGrid.GetLength(1);
            if (x + width > gridColumns || y + height > gridRows) return false;
            if (x < 0 || y < 0) return false;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    if (_cargoGrid[x + dx, y + dy] != UICargoItemType.None) return false;
                    if (_damagedCells[x + dx, y + dy]) return false;
                }
            }
            return true;
        }

        private void RemoveItem(UIGridContainer container, UICargoItemType[,] grid, int x, int y)
        {
            var itemType = grid[x, y];
            if (itemType == UICargoItemType.None) return;

            var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);
            var cell = container.FindCellAt(x, y);
            int primaryX = cell?.GridX ?? x;
            int primaryY = cell?.GridY ?? y;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int clearX = primaryX + dx;
                    int clearY = primaryY + dy;
                    if (clearX >= 0 && clearX < grid.GetLength(0) && clearY >= 0 && clearY < grid.GetLength(1))
                    {
                        grid[clearX, clearY] = UICargoItemType.None;
                    }
                }
            }

            if (cell != null)
            {
                container.RemoveCell(cell);
            }

            if (container == _cargoContainer && _damagedCells != null)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        int damagedX = primaryX + dx;
                        int damagedY = primaryY + dy;
                        if (damagedX >= 0 && damagedX < columns &&
                            damagedY >= 0 && damagedY < cargoRows &&
                            _damagedCells[damagedX, damagedY])
                        {
                            CreateDamagedCellVisual(damagedX, damagedY);
                        }
                    }
                }
            }
        }

        private bool TryPlaceItemAnywhere(UIGridContainer container, UICargoItemType[,] grid, UICargoItemType itemType)
        {
            var (width, height, _) = UICargoItemRegistry.GetItemInfo(itemType);
            int gridColumns = grid.GetLength(0);
            int gridRows = grid.GetLength(1);
            bool isCargoGrid = grid == _cargoGrid;

            for (int y = 0; y < gridRows; y++)
            {
                for (int x = 0; x < gridColumns; x++)
                {
                    bool canPlace = isCargoGrid 
                        ? CanPlaceItemInCargo(x, y, width, height) 
                        : CanPlaceItem(grid, x, y, width, height);
                    
                    if (canPlace)
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

            if (_fixHoldBtn != null)
            {
                _fixHoldBtn.clicked += OnFixHoldClicked;
            }
        }

        private void OnFixHoldClicked()
        {
            if (!HasDamagedCells()) return;

            _repairMode = !_repairMode;
            UpdateFixHoldButtonVisibility();
        }

        private void OnSettleDepartClicked()
        {
            _repairMode = false;

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
            if (_repairMode)
            {
                TryRepairDamagedCell(gridX, gridY);
                return;
            }

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
                    _playerGold += goldValue;
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
                    _playerGold -= goldValue;
                    if (_playerGold < 0) _playerGold = 0;
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
                _goldLabel.text = $"Estimate Gold: {CalculateCargoValue()} | Gold: {_playerGold}";
            }
        }

        private void UpdateGoldDisplayUnload()
        {
            if (_goldLabel != null)
            {
                _goldLabel.text = $"Earned Gold: {_earnedGold} | Gold: {_playerGold}";
            }
        }

        private void TryRepairDamagedCell(int gridX, int gridY)
        {
            if (!IsCellDamaged(gridX, gridY)) return;

            if (_playerGold < fixHoldCost)
            {
                ShowPopup("Not enough gold to fix hold.");
                return;
            }

            _playerGold -= fixHoldCost;
            _damagedCells[gridX, gridY] = false;

            var clickedCell = _cargoContainer.FindCellAt(gridX, gridY);
            if (clickedCell != null && clickedCell.ClassListContains("damaged-cell") && clickedCell is not UICargoItemCell)
            {
                _cargoContainer.RemoveCell(clickedCell);
            }

            ApplyDamageStylesToCargoItems();
            UpdateFixHoldButtonVisibility();
            if (_currentState == CargoUIState.Load) UpdateGoldDisplayLoad();
            else UpdateGoldDisplayUnload();
        }

        private void ShowPopup(string message)
        {
            if (_popupContainer == null || _popupLabel == null) return;

            _popupLabel.text = message;
            _popupContainer.style.display = DisplayStyle.Flex;
            _popupContainer.schedule.Execute(() =>
            {
                _popupContainer.style.display = DisplayStyle.None;
            }).ExecuteLater(1500);
        }

        /// <summary>
        /// Responds to the boat hitting an obstacle by damaging all occupied cargo cells.
        /// Damaged cells cannot hold cargo in future load states.
        /// </summary>
        public void ResponseToHittingObstacle()
        {
            if (_cargoGrid == null || _damagedCells == null)
            {
                return;
            }

            bool hasCargo = false;
            for (int y = 0; y < cargoRows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (_cargoGrid[x, y] == UICargoItemType.None) continue;
                    _damagedCells[x, y] = true;
                    hasCargo = true;
                }
            }

            if (!hasCargo)
            {
                Debug.Log("[UICargoLoad] No cargo cells to mark as damaged.");
                return;
            }

            ApplyDamageStylesToCargoItems();
            UpdateFixHoldButtonVisibility();
        }

        private void CreateDamagedCellVisual(int x, int y)
        {
            if (_cargoContainer == null) return;
            if (x < 0 || x >= columns || y < 0 || y >= cargoRows) return;
            if (_cargoGrid[x, y] != UICargoItemType.None) return;
            if (_cargoContainer.FindCellAt(x, y) != null) return;

            var damagedCell = new UIIconGridCell();
            damagedCell.AddToClassList("damaged-cell");
            _cargoContainer.PlaceCell(damagedCell, x, y, 1, 1);
        }

        /// <summary>
        /// Checks if a specific cargo cell is damaged.
        /// </summary>
        public bool IsCellDamaged(int x, int y)
        {
            if (x < 0 || x >= columns || y < 0 || y >= cargoRows) return false;
            return _damagedCells[x, y];
        }

        /// <summary>
        /// Resets all damaged cells. Use when starting a new voyage.
        /// </summary>
        public void RepairAllDamage()
        {
            for (int y = 0; y < cargoRows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (_damagedCells[x, y])
                    {
                        _damagedCells[x, y] = false;
                        var cell = _cargoContainer.FindCellAt(x, y);
                        if (cell != null && cell.ClassListContains("damaged-cell"))
                        {
                            if (cell is UICargoItemCell cargoCell)
                            {
                                cargoCell.RemoveFromClassList("damaged-cell");
                            }
                            else
                            {
                                _cargoContainer.RemoveCell(cell);
                            }
                        }
                    }
                }
            }

            UpdateFixHoldButtonVisibility();
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
