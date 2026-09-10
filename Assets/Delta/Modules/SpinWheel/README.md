# SpinWheel Package

A project-independent spin wheel system for Unity with an intuitive setup wizard.

## Features

- 🎯 **Singleton Pattern**: Easy access via `SpinWheel.Instance`
- 🎨 **Visual Setup Wizard**: Configure wheel pieces without manual inspector editing
- 🎁 **Flexible Reward System**: Simple, serializable reward data structure
- ⏰ **Daily Spin Limits**: Built-in daily spin counter with reset
- 🎭 **Gradient Colors**: Beautiful gradient backgrounds for wheel pieces
- 📊 **Validation Tools**: Auto-normalize chances and validate configurations

## Quick Start

### 1. Create a SpinWheel Configuration

**Option A: Using the Setup Wizard (Recommended)**
1. Go to `Tools > SpinWheel > Setup Wizard`
2. Click "Create New" to create a SpinWheelConfig asset
3. Use quick presets (4, 6, 8, or 12 pieces) or manually add pieces
4. Configure each piece:
   - Set reward icon, label, and amount
   - Adjust chance percentage
   - Customize gradient colors
5. Use helper buttons:
   - **Normalize Chances**: Auto-adjust to total 100%
   - **Set Equal Chances**: Distribute chances evenly
   - **Randomize Colors**: Generate random color schemes

**Option B: Using the Inspector**
1. Right-click in Project window
2. Select `Create > SpinWheel > Config`
3. Configure the asset in the inspector (or click "Open in Setup Wizard" button)

### 2. Setup the SpinWheel in Your Scene

**Option A: Using SpinWheelConfig Asset (Recommended)**

```csharp
using Delta.Modules.SpinWheel;

public class MyGameManager : MonoBehaviour
{
    [SerializeField] private SpinWheelConfig spinWheelConfig;
    
    void Start()
    {
        // Load configuration from asset
        SpinWheel.Instance.LoadFromConfig(spinWheelConfig);
        
        // Subscribe to reward events
        SpinWheel.Instance.OnRewardWon.AddListener(OnRewardReceived);
        
        // Show the wheel (calls Initialize automatically)
        ShowWheel();
    }
    
    void OnRewardReceived(SpinWheelReward reward)
    {
        Debug.Log($"Won: {reward.label} x{reward.amount}");
        // Handle the reward in your game
        
        if (reward.rewardId == 1)
        {
            // Give coins
        }
        else if (reward.rewardId == 2)
        {
            // Give gems
        }
    }
    
    public void ShowWheel()
    {
        SpinWheel.Instance.Show();
    }
}
```

**Option B: Using Direct Array Assignment**

```csharp
// For programmatic setup without ScriptableObject
SpinWheel.Instance.wheelPieces = myWheelPiecesArray;
SpinWheel.Instance.Show();
```

### 3. Display the Wheel UI

The SpinWheelUI prefab is located at `Resources/Dialogs/SpinWheelUI.prefab`. It will be automatically instantiated when you call `SpinWheel.Instance.Show()`.

## Setup Wizard Features

### Config Management
- Select existing configs or create new ones
- Visual feedback showing config path
- Auto-save on changes

### Wheel Configuration
- Set unlock level requirement
- Quick presets for common wheel sizes (4, 6, 8, 12 pieces)
- Visual piece count display

### Wheel Pieces Editor
- Expandable/collapsible piece list
- Inline editing for all piece properties
- Drag & drop sprite assignment
- Visual icon preview
- Duplicate and delete buttons

### Validation & Helpers
- Real-time total chance calculation
- Visual warning when total ≠ 100%
- Error detection for invalid configurations
- One-click normalization
- Visual chance distribution bar

### Quality of Life
- Full Undo/Redo support
- Scrollable interface for many pieces
- Color-coded validation messages
- Quick action buttons

## Architecture

### Core Classes

**SpinWheel** - Main singleton controller
- Manages spin logic and wheel state
- Handles daily spin limits
- Fires reward events

**SpinWheelUI** - UI controller (extends UIController)
- Manages wheel visuals and animations
- Shows remaining spins
- Displays light effects

**SpinWheelReward** - Simple reward data structure
```csharp
public class SpinWheelReward
{
    public int rewardId;      // Unique identifier
    public int amount;        // Quantity
    public Sprite icon;       // Visual representation
    public string label;      // Display name
}
```

**WheelPiece** - Individual wheel segment configuration
```csharp
public class WheelPiece
{
    public int id;
    public SpinWheelReward reward;
    public float Chance;      // Probability (0-100%)
    public Color CenterColor; // Gradient center
    public Color EdgeColor;   // Gradient edge
}
```

**SpinWheelConfig** - ScriptableObject for configuration
- Stores unlock level
- Contains array of wheel pieces
- Can be created via context menu or wizard
- Load into SpinWheel using `LoadFromConfig()` method

## Daily Spin System

The package includes a built-in daily spin limit system:

```csharp
// Check remaining spins
int remaining = SpinWheel.Instance.RemainingSpins;

// Check if limit reached
bool limitReached = SpinWheel.Instance.IsDailySpinLimitReached();

// Reset daily spins (admin/testing)
SpinWheel.Instance.ResetDailySpins();
```

The system automatically resets at midnight and stores data in PlayerPrefs.

## Dependencies

- **DOTween**: For wheel rotation animations
- **Athena.Common.UI**: UIManager and UIController (included in project)
- **Unity UI**: Standard Unity UI components

## File Structure

```
Assets/ProjectSpace/MenuSpinWheel/
├── Editor/
│   ├── SpinWheelSetupWizard.cs      # Main setup wizard
│   └── SpinWheelConfigEditor.cs     # Custom inspector
├── Scripts/
│   ├── SpinWheel.cs                 # Main controller
│   ├── SpinWheelUI.cs               # UI controller
│   ├── SpinWheelPiece.cs            # Visual piece component
│   ├── SpinWheelReward.cs           # Reward data structure
│   ├── WheelPiece.cs                # Piece configuration
│   ├── SpinWheelConfig.cs           # ScriptableObject config
│   ├── SpinWheelEntry.cs            # Entry button component
│   └── RewardRateItem.cs            # Rate display component
├── Resources/
│   ├── Dialogs/
│   │   └── SpinWheelUI.prefab       # Main UI prefab
│   └── SpinWheel/
│       ├── Piece.prefab             # Wheel piece prefab
│       └── Line.prefab              # Separator line prefab
├── Prefabs/
├── Sprites/
├── Materials/
└── Sounds/
```

## Tips & Best Practices

1. **Always normalize chances** to ensure they total 100%
2. **Use the wizard** for faster configuration and validation
3. **Test daily limits** using the reset function in SpinWheelUI
4. **Subscribe to OnRewardWon** before calling Show()
5. **Use distinct reward IDs** for easier handling
6. **Provide visual icons** for better user experience

## Example: Quick Setup

```csharp
// In your game initialization
void SetupSpinWheel()
{
    var wheel = SpinWheel.Instance;
    
    // Option 1: Use SpinWheelConfig asset (Recommended)
    wheel.LoadFromConfig(mySpinWheelConfig);
    
    // Option 2: Create programmatically
    wheel.wheelPieces = new WheelPiece[]
    {
        new WheelPiece
        {
            id = 1,
            Chance = 40f,
            reward = new SpinWheelReward(1, 100, coinSprite, "Coins"),
            CenterColor = Color.yellow,
            EdgeColor = new Color(0.8f, 0.6f, 0f)
        },
        new WheelPiece
        {
            id = 2,
            Chance = 30f,
            reward = new SpinWheelReward(2, 50, gemSprite, "Gems"),
            CenterColor = Color.cyan,
            EdgeColor = Color.blue
        },
        new WheelPiece
        {
            id = 3,
            Chance = 20f,
            reward = new SpinWheelReward(3, 10, starSprite, "Stars"),
            CenterColor = Color.magenta,
            EdgeColor = Color.red
        },
        new WheelPiece
        {
            id = 4,
            Chance = 10f,
            reward = new SpinWheelReward(4, 1, jackpotSprite, "Jackpot!"),
            CenterColor = Color.white,
            EdgeColor = new Color(1f, 0.8f, 0f)
        }
    };
    
    wheel.OnRewardWon.AddListener(reward => {
        Debug.Log($"Player won: {reward.label} x{reward.amount}");
    });
    
    wheel.Initialize();
}
```

## Troubleshooting

**Wheel pieces not showing?**
- Make sure Resources/SpinWheel/Piece.prefab exists
- Check that wheel pieces array is not empty
- Verify Initialize() was called

**Chances don't add up to 100%?**
- Use the "Normalize Chances" button in the wizard
- The wheel will still work but may have bias

**Daily spins not resetting?**
- Check system date/time is correct
- Manually reset via SpinWheelUI context menu (testing only)

**UI not appearing?**
- Ensure UIManager exists in scene
- Check Resources/Dialogs/SpinWheelUI.prefab exists
- Verify UIManager.Instance.ShowUIOnTop works

## Support

This package is fully standalone and project-independent. It requires only:
- DOTween (for animations)
- Your project's UIManager system

For questions or issues, refer to the source code documentation.

