using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIMainMenu : MonoBehaviour
{
    private const string LevelWhiteboxScenePath = "Scenes/TestScenes/LevelWhitebox";

    [SerializeField] private UIDocument uiDocument;
    private Button _startButton;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
    }

    private void OnEnable()
    {
        var root = uiDocument?.rootVisualElement;
        if (root == null) return;

        _startButton = root.Q<Button>("start-btn");
        if (_startButton != null)
        {
            _startButton.clicked += OnStartClicked;
        }
    }

    private void OnDisable()
    {
        if (_startButton != null)
        {
            _startButton.clicked -= OnStartClicked;
        }
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene(LevelWhiteboxScenePath);
    }
}
