using System;
using System.Collections.Generic;
using UI.Runtime.CustomControl;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the cargo map UI, allowing transfer of items between cargo and market.
/// </summary>
public class CargoMap : MonoBehaviour
{
    [Header("Item Icons")]
    [SerializeField] private Texture2D crateIcon;
    [SerializeField] private Texture2D plankIcon;
    [SerializeField] private Texture2D toolkitIcon;

    [Header("Grid Settings")]
    [SerializeField] private int cellsPerRow = 5;
    [SerializeField] private int cargoTotalRows = 7;
    [SerializeField] private int marketTotalRows = 7;

    [Header("Player Stats")]
    [SerializeField] private int initialGold = 1000;

    private SSGridContainer _cargoContainer;
    private SSGridContainer _marketContainer;
    private Label _goldLabel;
    private Button _settleDepartBtn;
    private Button _departBtn;
    private int _currentGold;

    /// <summary>
    /// Event fired when Settle & Depart button is clicked.
    /// </summary>
    public event Action OnSettleAndDepart;

    /// <summary>
    /// Event fired when Depart button is clicked.
    /// </summary>
    public event Action OnDepart;

    void Awake()
    {
        _currentGold = initialGold;
        
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        _cargoContainer = root.Q<SSGridContainer>("cargo-container");
        _marketContainer = root.Q<SSGridContainer>("market-container");
        _goldLabel = root.Q<Label>("gold-label");
        _settleDepartBtn = root.Q<Button>("settle-depart-btn");
        _departBtn = root.Q<Button>("depart-btn");

        InitializeContainers();
        RegisterCallbacks();
        RegisterButtonCallbacks();
        UpdateGoldDisplay();
        
        PopulateInitialItems();
    }

    /// <summary>
    /// Initializes both containers with empty cells.
    /// </summary>
    private void InitializeContainers()
    {
        if (_cargoContainer != null)
        {
            _cargoContainer.CellPerRow = cellsPerRow;
            for (int i = 0; i < cargoTotalRows; i++)
            {
                for (int j = 0; j < cellsPerRow; j++)
                {
                    _cargoContainer.Add(new SSIconGridCell());
                }
            }
        }

        if (_marketContainer != null)
        {
            _marketContainer.CellPerRow = cellsPerRow;
            for (int i = 0; i < marketTotalRows; i++)
            {
                for (int j = 0; j < cellsPerRow; j++)
                {
                    _marketContainer.Add(new SSIconGridCell());
                }
            }
        }
    }

    /// <summary>
    /// Registers click callbacks for both containers.
    /// </summary>
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

    /// <summary>
    /// Registers click callbacks for buttons.
    /// </summary>
    private void RegisterButtonCallbacks()
    {
        if (_settleDepartBtn != null)
        {
            _settleDepartBtn.clicked += OnSettleDepartClicked;
        }

        if (_departBtn != null)
        {
            _departBtn.clicked += OnDepartClicked;
        }
    }

    /// <summary>
    /// Called when Settle & Depart button is clicked.
    /// </summary>
    private void OnSettleDepartClicked()
    {
        OnSettleAndDepart?.Invoke();
    }

    /// <summary>
    /// Called when Depart button is clicked.
    /// </summary>
    private void OnDepartClicked()
    {
        OnDepart?.Invoke();
    }

    /// <summary>
    /// Populates initial items in the cargo container for demonstration.
    /// </summary>
    private void PopulateInitialItems()
    {
        if (_cargoContainer == null) return;

        var icons = new List<Texture2D> { crateIcon, plankIcon, toolkitIcon };
        int cargoCellCount = cellsPerRow * cargoTotalRows;
        
        for (int i = 0; i < 6 && i < cargoCellCount; i++)
        {
            var cell = _cargoContainer.GetCellAt<SSIconGridCell>(i);
            if (cell != null && icons.Count > 0)
            {
                cell.SetIcon(icons[i % icons.Count]);
            }
        }
    }

    /// <summary>
    /// Called when a cell in the cargo container is clicked.
    /// Moves the item to the first empty slot in the market.
    /// </summary>
    private void OnCargoContainerClicked(int index, SSIconGridCell cell)
    {
        if (cell == null || cell.iconTexture == null) return;

        var emptyMarketCell = FindFirstEmptyCell(_marketContainer);
        if (emptyMarketCell != null)
        {
            TransferItem(cell, emptyMarketCell);
        }
    }

    /// <summary>
    /// Called when a cell in the market container is clicked.
    /// Moves the item to the first empty slot in the cargo.
    /// </summary>
    private void OnMarketContainerClicked(int index, SSIconGridCell cell)
    {
        if (cell == null || cell.iconTexture == null) return;

        var emptyCargoCell = FindFirstEmptyCell(_cargoContainer);
        if (emptyCargoCell != null)
        {
            TransferItem(cell, emptyCargoCell);
        }
    }

    /// <summary>
    /// Finds the first empty cell in the given container.
    /// </summary>
    /// <param name="container">The container to search.</param>
    /// <returns>The first empty cell, or null if all cells are occupied.</returns>
    private SSIconGridCell FindFirstEmptyCell(SSGridContainer container)
    {
        if (container == null) return null;

        for (int i = 0; i < container.childCount; i++)
        {
            var cell = container.GetCellAt<SSIconGridCell>(i);
            if (cell != null && cell.iconTexture == null)
            {
                return cell;
            }
        }
        return null;
    }

    /// <summary>
    /// Transfers an item from the source cell to the target cell.
    /// </summary>
    /// <param name="source">The source cell containing the item.</param>
    /// <param name="target">The target cell to receive the item.</param>
    private void TransferItem(SSIconGridCell source, SSIconGridCell target)
    {
        if (source == null || target == null) return;

        target.SetIcon(source.iconTexture);
        source.SetIcon(null);
    }

    /// <summary>
    /// Updates the gold display label.
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (_goldLabel != null)
        {
            _goldLabel.text = $"Gold:{_currentGold}";
        }
    }

    /// <summary>
    /// Gets or sets the current gold amount.
    /// </summary>
    public int Gold
    {
        get => _currentGold;
        set
        {
            _currentGold = value;
            UpdateGoldDisplay();
        }
    }
}
