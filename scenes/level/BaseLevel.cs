using Game.Manager;
using Godot;
using Game.UI;
using Game.Resources.Level;
using Game.Autoload;







namespace Game;

public partial class BaseLevel : Node
{

	private readonly StringName ESCAPE_ACTION = "escape";

	[Export]
    private PackedScene levelCompleteScreenScene;

	[Export]
	private PackedScene escapeMenuScene;

	[Export]
	private LevelDefinitionResource levelDefinitionResource;

	


	private GridManager gridManager;
	private GoldMine goldMine;
	private GameCamera gameCamera;
	private Node2D baseBuilding;
	private TileMapLayer baseTerrainTilemapLayer;
	private GameUI gameUI;
	private BuildingManager buildingManager;
	private bool IsComplete;
	//private Joystic
	

	





	


	
	public override void _Ready()
	{
		gridManager = GetNode<GridManager>("GridManager");
		goldMine = GetNode<GoldMine>("%GoldMine");
		gameCamera = GetNode<GameCamera>("GameCamera");
		baseTerrainTilemapLayer = GetNode<TileMapLayer>("%BaseTerrainTileMapLayer");
		baseBuilding = GetNode<Node2D>("%Base");
		gameUI = GetNode<GameUI>("GameUI");
		buildingManager = GetNode<BuildingManager>("BuildingManager");

		buildingManager.SetStartingResourceCount(levelDefinitionResource.StartingResourceCount);

		gameCamera.SetBoundingRect(baseTerrainTilemapLayer.GetUsedRect());
		gameCamera.CenterOnPosition(baseBuilding.GlobalPosition);

		gridManager.SetGoldMinePosition(gridManager.ConvertWorldPositionToTilePosition(goldMine.GlobalPosition));

		gridManager.GridStateUpdated += OnGridStateUpdated;
		
	}


    public override void _UnhandledInput(InputEvent evt)
    {
        
		if (evt.IsActionPressed(ESCAPE_ACTION))
		{
			var escapeMenu = escapeMenuScene.Instantiate<EscapeMenu>();
			AddChild(escapeMenu);
			GetViewport().SetInputAsHandled();

		}
    }

    private void ShowLevelComplete()
	{
		IsComplete = true;
		SaveManager.SaveLevelCompletion(levelDefinitionResource);


		var levelCompleteScreen = levelCompleteScreenScene.Instantiate<LevelCompleteScreen>();
		AddChild(levelCompleteScreen);
		goldMine.SetActive();
		gameUI.HideUI();

	}

	private void OnGridStateUpdated()
	{

		if (IsComplete) return;
		
		var goldMineTilePosition = gridManager.ConvertWorldPositionToTilePosition(goldMine.GlobalPosition);
		if (gridManager.IsTilePositionInAnyBuildingRadius(goldMineTilePosition))
		{
			ShowLevelComplete();

			
		}
	}

    
   
	

	


	
	


	
}
