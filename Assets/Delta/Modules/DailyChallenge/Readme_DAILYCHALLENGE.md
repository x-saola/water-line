# Daily Challenge Module


## Usage

Add this Function to a file to create DailyChallenge

```csharp
 private DailySelectUI DailyModeUI;
         
 public void ShowDailyModeUI()
 {
         DailyModeUI = UIManager.Instance.ShowUIOnTop<DailySelectUI>("DailySelectUI");
         DailyModeUI.Setup(System.DateTime.Now);
         DailyModeUI.ActionPlayDate = PlayDailyMode;
 }
 private void PlayDailyMode(DateTime date)
 {
         //Change DateTime -> seed
         int seed = int.Parse(date.ToString("ddMMyyyy"));
         System.Random random = new System.Random(seed);
             
         int level = random.Next(0,yourHighestLevel);
         //Play your own level by seed
 }
     
```


## Call Example
```csharp
ShowDailyModeUI();
```