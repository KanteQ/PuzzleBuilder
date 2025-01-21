using System.Linq;
using Game.Resources.Level;
using Godot;




namespace Game.Autoload;

public partial class LevelManager : Node 


{

	public static LevelManager instance;

	[Export]

	private LevelDefinitionResource[] levelDefinitions;

	private static int currentLevelIndex;

	public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
		{
			instance = this;
		}

    }

	public static LevelDefinitionResource[] GetLevelDefinitions()
	{
		return instance.levelDefinitions.ToArray();
	}

	


	public static void ChangeToLevel(int levelIndex)
	{

		

		if (levelIndex >= instance.levelDefinitions.Length || levelIndex < 0) return;

		currentLevelIndex = levelIndex;
		
		var levelDefinition = instance.levelDefinitions[currentLevelIndex];

		instance.GetTree().ChangeSceneToFile(levelDefinition.levelScenePath);
	}

	public void ChangeToNextLevel()
	{
		ChangeToLevel(currentLevelIndex + 1);
	}

	public static bool IstLastLevel()
	{
		return currentLevelIndex == instance.levelDefinitions.Length -1;
	}

	
}
