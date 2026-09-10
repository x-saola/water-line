# Save System Tutorial

This tutorial explains how to use the save system in the Atom framework. The save system allows you to easily save and restore the state of game objects and components.

## Table of Contents
- [Overview](#overview)
- [Implementation Guide](#implementation-guide)
- [Usage Examples](#usage-examples)
- [Advanced Features](#advanced-features)

## Overview

The save system consists of two main components:
- `ISaveable` interface - Implemented by objects that need save/load functionality
- `SaveManager` - Manages saving and loading of all saveable objects

Key features:
- JSON-based serialization using Newtonsoft.Json
- Automatic type handling for saved data
- Centralized save file management
- Quick state access without full restoration
- Ability to clear specific or all save data

## Implementation Guide

### 1. Implement ISaveable

To make an object saveable, implement the `ISaveable` interface:

```csharp
using Delta.Modules.SaveSystem;

public class PlayerStats : MonoBehaviour, ISaveable
{
    public int health = 100;
    public int coins = 0;
    
    // Unique identifier for this saveable object
    public string SaveId => "player_stats";
    
    // Capture current state
    public object CaptureState()
    {
        return new SaveData {
            health = this.health,
            coins = this.coins
        };
    }
    
    // Restore from saved state
    public void RestoreState(object state)
    {
        var saveData = (SaveData)state;
        health = saveData.health;
        coins = saveData.coins;
    }
    
    // Define data structure for serialization
    [System.Serializable]
    private class SaveData
    {
        public int health;
        public int coins;
    }
}
```

### 2. Register with SaveManager

Objects must be registered to be included in save/load operations. Register in `OnEnable` and unregister in `OnDisable`:

```csharp
private void OnEnable()
{
    SaveManager.Register(this);
}

private void OnDisable()
{
    SaveManager.Unregister(this);
}
```

### 3. Setup SaveManager

Add the `SaveManager` component to a GameObject in your scene:

```csharp
// In your game initialization
public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Ensure SaveManager exists
        if (FindObjectOfType<SaveManager>() == null)
        {
            var saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
        }
    }
}
```

## Usage Examples

### Basic Save/Load Operations

```csharp
// Save all registered objects
SaveManager.SaveAllState();

// Load all registered objects
SaveManager.LoadAllState();
```

### Checking Specific Save Data

```csharp
// Try to get saved player stats
if (SaveManager.TryGetState<PlayerStats.SaveData>("player_stats", out var savedStats))
{
    Debug.Log($"Found saved health: {savedStats.health}");
}

// Get state with default fallback
var stats = SaveManager.GetState<PlayerStats.SaveData>("player_stats");
```

### Complex Object Example

```csharp
public class Inventory : MonoBehaviour, ISaveable
{
    public List<InventoryItem> items = new List<InventoryItem>();
    
    public string SaveId => "player_inventory";
    
    public object CaptureState()
    {
        return new SaveData {
            serializedItems = items.Select(item => new SerializedItem {
                id = item.id,
                quantity = item.quantity
            }).ToList()
        };
    }
    
    public void RestoreState(object state)
    {
        var saveData = (SaveData)state;
        items.Clear();
        
        foreach (var serializedItem in saveData.serializedItems)
        {
            items.Add(new InventoryItem {
                id = serializedItem.id,
                quantity = serializedItem.quantity
            });
        }
    }
    
    [System.Serializable]
    private class SaveData
    {
        public List<SerializedItem> serializedItems = new List<SerializedItem>();
    }
    
    [System.Serializable]
    private class SerializedItem
    {
        public string id;
        public int quantity;
    }
}
```

## Advanced Features

### Clearing Save Data

```csharp
// Clear specific save entry
SaveManager.Instance.ClearEntry("player_stats");

// Clear all save data
SaveManager.Instance.ClearAll();
```

### Save File Location

The save file is stored in the persistent data path:
- Windows: `%userprofile%/AppData/LocalLow/<CompanyName>/<ProductName>/savefile.json`
- macOS: `~/Library/Application Support/Unity.<CompanyName>.<ProductName>/savefile.json`
- Android: `/storage/emulated/0/Android/data/<packagename>/files/savefile.json`
- iOS: `/var/mobile/Containers/Data/Application/<guid>/Documents/savefile.json`

### Best Practices

1. Always use unique SaveId values
2. Keep saved data structures simple and serializable
3. Handle missing save data gracefully
4. Test both saving and loading with various data states
5. Consider backing up save data for important games
6. Implement error handling for corrupt save data

### Performance Considerations

- Save data is cached after first load
- Avoid saving unnecessarily large objects
- Consider batching save operations
- Profile save/load operations with large datasets
