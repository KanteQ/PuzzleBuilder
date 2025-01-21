using System.Collections.Generic;

namespace Game;

public class SaveData
{
    public Dictionary<string, LevelCompletionData> LevelCompletionStatus { get; private set;} = new();

    public void SaveLevelCompletion(string id, bool completed)
    {
        if (!LevelCompletionStatus.ContainsKey(id)) 
        {
            LevelCompletionStatus[id] = new LevelCompletionData();
        
        
        }
        LevelCompletionStatus[id].IsCompleted = completed;
    }
}

// Zapis poziomu po przejsciu go gdzie w stringu podawane jest Id a w boolu kompletacja