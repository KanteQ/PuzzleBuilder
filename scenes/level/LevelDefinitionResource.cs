using Godot;


namespace Game.Resources.Level;


[GlobalClass]

public partial class LevelDefinitionResource : Resource
{

	[Export]
	public string Id { get; private set; }

	[Export]
    public int StartingResourceCount { get; private set; } = 10;
	[Export(PropertyHint.File, "*.tscn")]
	public string levelScenePath { get; private set; }

}
