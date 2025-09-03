using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example script showing how to use the FloatingText system.
/// Attach this to any GameObject to test different floating text animations.
/// </summary>
public class FloatingTextExample : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool _enableInputTesting = true;
    [SerializeField] private Transform _testTarget; // Optional target transform
    
    [Header("Test Messages")]
    [SerializeField] private string[] _testMessages = {
        "Hello World!",
        "Damage: -25",
        "Heal: +10",
        "Critical Hit!",
        "Score: +100",
        "Level Up!",
        "Game Over",
        "Victory!"
    };
    
    [Header("Test Colors")]
    [SerializeField] private Color[] _testColors = {
        Color.white,
        Color.red,
        Color.green,
        Color.yellow,
        Color.blue,
        Color.magenta,
        Color.cyan
    };
    
    private int _messageIndex = 0;
    private int _colorIndex = 0;
    private int _animationIndex = 0;
    
    private FloatingText.AnimationType[] _animationTypes = {
        FloatingText.AnimationType.RiseAndFade,
        FloatingText.AnimationType.BounceAndFade,
        FloatingText.AnimationType.ScaleAndFade,
        FloatingText.AnimationType.StaticFade,
        FloatingText.AnimationType.FloatUp
    };
    
    private void Update()
    {
        if (!_enableInputTesting) return;
        
        // Test different floating text types with keyboard input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TestBasicFloatingText();
            }
            
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                TestDamageText();
            }
            
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                TestHealText();
            }
            
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                TestScoreText();
            }
            
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                TestCriticalText();
            }
            
            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                TestMessageText();
            }
            
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                FloatingTextManager.ClearAll();
            }
            
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                TestTargetText();
            }
            
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                TestScreenText();
            }
        }
    }
    
    private void TestBasicFloatingText()
    {
        Vector3 position = GetTestPosition();
        string message = _testMessages[_messageIndex];
        Color color = _testColors[_colorIndex];
        FloatingText.AnimationType animationType = _animationTypes[_animationIndex];
        
        FloatingTextManager.SpawnText(position, message, animationType, 2f, color);
        
        // Cycle through options
        _messageIndex = (_messageIndex + 1) % _testMessages.Length;
        _colorIndex = (_colorIndex + 1) % _testColors.Length;
        _animationIndex = (_animationIndex + 1) % _animationTypes.Length;
    }
    
    private void TestDamageText()
    {
        Vector3 position = GetTestPosition();
        int damage = Random.Range(10, 100);
        FloatingTextManager.ShowDamage(position, damage);
    }
    
    private void TestHealText()
    {
        Vector3 position = GetTestPosition();
        int heal = Random.Range(5, 50);
        FloatingTextManager.ShowHeal(position, heal);
    }
    
    private void TestScoreText()
    {
        Vector3 position = GetTestPosition();
        int score = Random.Range(50, 500);
        FloatingTextManager.ShowScore(position, score);
    }
    
    private void TestCriticalText()
    {
        Vector3 position = GetTestPosition();
        FloatingTextManager.ShowCritical(position, "CRITICAL HIT!");
    }
    
    private void TestMessageText()
    {
        Vector3 position = GetTestPosition();
        string message = _testMessages[Random.Range(0, _testMessages.Length)];
        FloatingTextManager.ShowMessage(position, message);
    }
    
    private void TestTargetText()
    {
        if (_testTarget != null)
        {
            Vector3 offset = new Vector3(0, 2f, 0); // 2 units above target
            FloatingTextManager.SpawnText(_testTarget, "Above Target!", offset);
        }
        else
        {
            Debug.LogWarning("[FloatingTextExample] No test target assigned!");
        }
    }
    
    private void TestScreenText()
    {
        Vector2 screenPos = new Vector2(
            Random.Range(100, Screen.width - 100),
            Random.Range(100, Screen.height - 100)
        );
        FloatingTextManager.SpawnScreenText(screenPos, "Screen Text!");
    }
    
    private Vector3 GetTestPosition()
    {
        if (_testTarget != null)
        {
            return _testTarget.position + Random.insideUnitSphere * 2f;
        }
        else
        {
            return transform.position + Random.insideUnitSphere * 2f;
        }
    }
    
    private void OnGUI()
    {
        if (!_enableInputTesting) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Floating Text Test Controls:");
        GUILayout.Label("Space - Basic floating text");
        GUILayout.Label("1 - Damage text");
        GUILayout.Label("2 - Heal text");
        GUILayout.Label("3 - Score text");
        GUILayout.Label("4 - Critical text");
        GUILayout.Label("5 - Message text");
        GUILayout.Label("T - Text above target");
        GUILayout.Label("S - Screen text");
        GUILayout.Label("C - Clear all texts");
        GUILayout.EndArea();
    }
}
