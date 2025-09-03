# Floating Text System Setup Guide

This guide will help you set up the floating text system in your Unity project.

## Quick Setup

### 1. Create the Floating Text Prefab

1. **Create a new GameObject** in your scene:

   - Right-click in Hierarchy → Create Empty
   - Name it "FloatingText"

2. **Add required components**:

   - Add `Canvas` component
   - Add `CanvasGroup` component
   - Add `FloatingText` script

3. **Configure the Canvas**:

   - Set `Render Mode` to `World Space`
   - Set `Sort Order` to a high value (e.g., 100) to appear above other UI
   - Set `Scale Factor` to 0.01 (for world space text)

4. **Create the text child object**:

   - Right-click on FloatingText → UI → Text - TextMeshPro
   - Name it "Text"
   - Position it at (0, 0, 0)
   - Set the text size to something visible (e.g., 0.5)

5. **Configure the FloatingText script**:

   - Drag the TextMeshPro component to the `Text` field
   - Adjust animation settings as needed

6. **Save as Prefab**:
   - Drag the FloatingText GameObject to your Prefabs folder
   - Delete the instance from the scene

### 2. Set Up the FloatingTextManager

1. **Create the Manager**:

   - Right-click in Hierarchy → Create Empty
   - Name it "FloatingTextManager"
   - Add the `FloatingTextManager` script

2. **Configure the Manager**:

   - Drag your FloatingText prefab to the `Floating Text Prefab` field
   - Adjust pool settings as needed

3. **Make it Persistent** (Optional):
   - Add `DontDestroyOnLoad` script or the manager will handle this automatically

### 3. Test the System

1. **Add the Example Script**:

   - Create an empty GameObject
   - Add the `FloatingTextExample` script
   - Optionally assign a target transform

2. **Test in Play Mode**:
   - Press Space for basic floating text
   - Press 1-5 for different text types
   - Press C to clear all texts
   - Press T for text above a target
   - Press S for screen-space text

## Usage Examples

### Basic Usage

```csharp
// Simple floating text at a position
FloatingTextManager.SpawnText(transform.position, "Hello World!");

// Floating text with custom animation and duration
FloatingTextManager.SpawnText(transform.position, "Custom Text",
    FloatingText.AnimationType.BounceAndFade, 3f, Color.red);

// Floating text above a target
FloatingTextManager.SpawnText(playerTransform, "Above Player!", Vector3.up * 2f);
```

### Convenience Methods

```csharp
// Damage text (red, rises and fades)
FloatingTextManager.ShowDamage(transform.position, 25);

// Heal text (green, rises and fades)
FloatingTextManager.ShowHeal(transform.position, 10);

// Score text (yellow, bounces and fades)
FloatingTextManager.ShowScore(transform.position, 100);

// Critical hit (red, scales and fades)
FloatingTextManager.ShowCritical(transform.position, "CRITICAL!");

// General message (white, floats up)
FloatingTextManager.ShowMessage(transform.position, "Level Up!");
```

### Screen Space Text

```csharp
// Text at screen position (for UI elements)
Vector2 screenPos = new Vector2(Screen.width / 2, Screen.height / 2);
FloatingTextManager.SpawnScreenText(screenPos, "Screen Text!");
```

## Animation Types

- **RiseAndFade**: Text rises up while fading (default for damage/heal)
- **BounceAndFade**: Text bounces up and down while fading (default for score)
- **ScaleAndFade**: Text pulses in scale while fading (default for critical)
- **StaticFade**: Text stays in place, only fades in/out
- **FloatUp**: Text smoothly floats up with slight wave motion (default for messages)

## Customization

### Per-Instance Customization

You can customize individual floating text instances by modifying the FloatingText component after spawning:

```csharp
FloatingText text = FloatingTextManager.SpawnText(transform.position, "Custom");
if (text != null)
{
    // Access the TMP_Text component for advanced customization
    TMP_Text tmpText = text.GetComponentInChildren<TMP_Text>();
    if (tmpText != null)
    {
        tmpText.fontSize = 2f;
        tmpText.fontStyle = FontStyles.Bold;
    }
}
```

### Manager Settings

Adjust these settings in the FloatingTextManager:

- **Initial Pool Size**: Number of text instances created at start
- **Max Pool Size**: Maximum number of instances (older ones get reused)
- **Default Animation Type**: Default animation for spawned text
- **Default Duration**: Default duration for text display
- **Default Color**: Default color for spawned text

## Performance Tips

1. **Use the pool**: The system automatically pools text instances for performance
2. **Limit active texts**: The manager has a max pool size to prevent memory issues
3. **Clear when needed**: Use `FloatingTextManager.ClearAll()` to clear all texts
4. **Reasonable durations**: Don't set extremely long durations as it keeps objects active

## Troubleshooting

### Text not appearing

- Check that the Canvas Render Mode is set to World Space
- Verify the Canvas Scale Factor is appropriate (0.01 for world space)
- Ensure the text size is large enough to be visible
- Check that the FloatingTextManager has the correct prefab assigned

### Text appearing too small/large

- Adjust the Canvas Scale Factor
- Modify the TextMeshPro font size
- Check the transform scale of the FloatingText GameObject

### Performance issues

- Reduce the initial pool size
- Lower the max pool size
- Use shorter durations for floating text
- Clear texts more frequently with `ClearAll()`

## Integration with Existing Systems

The floating text system is designed to work alongside your existing UI systems:

- It uses its own Canvas with high sort order to appear above other UI
- It doesn't interfere with your HUD, menus, or other UI elements
- It can be used from any script without dependencies
- It automatically handles cleanup and pooling

## Advanced Usage

For more advanced usage, you can:

1. **Extend FloatingText**: Create custom animation types by extending the FloatingText class
2. **Custom Manager**: Create specialized managers for different text types
3. **Event Integration**: Hook into game events to automatically show floating text
4. **Localization**: Integrate with your localization system for multi-language support
