using System.Collections.Generic;
using System.Linq;
using Game.Building;
using Game.Component;
using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{

	private readonly StringName ACTION_LEFT_CLICK = "left_click";
	private readonly StringName ACTION_CANCEL = "cancel";
	private readonly StringName ACTION_RIGHT_CLICK = "right_click";


	[Signal]

	public delegate void AvailableResourceCountChangedEventHandler(int AvailableResourceCount);

	
   

	[Export]
	private GridManager gridManager;
	[Export]
	private GameUI GameUI;
	[Export]
	private Node2D ySortRoot;
	[Export]
	private PackedScene buildingGhostScene;

	private enum State
	{
		Normal,
		PlaceingBuilding

	}

	private int currentResourceCount;
	
	private int currentlyUsedResourceAccount;
	private BuildingResource toPlaceBuildingResource;
	private Rect2I hoveredGridArea = new(Vector2I.Zero, Vector2I.One);
	private BuildingGhost buildingGhost;
	private Vector2 buildingGhostDimensions;
	private State currentState;
	private int startingResourceCount;

	private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResourceAccount;
	
	public override void _Ready()
	{

		gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
		GameUI.BuildingResourceSelected += OnBuildingResourceSelected;

		Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();	
	}

	public override void _UnhandledInput(InputEvent evt)
    {
		switch (currentState)
		{
			case State.Normal:
			    if (evt.IsActionPressed(ACTION_RIGHT_CLICK))
				{
					DestroyBuildingAtHoveredCellPosition();
					GetViewport().SetInputAsHandled();
				}
				
			    break;
			case State.PlaceingBuilding:
			if (evt.IsActionPressed(ACTION_CANCEL))
		{
			ChangeState(State.Normal);
			GetViewport().SetInputAsHandled();
		}
		else if (toPlaceBuildingResource != null && evt.IsActionPressed(ACTION_LEFT_CLICK))
		
		{
			PlaceBuildingAtHoveredCellPosition();
			GetViewport().SetInputAsHandled();
			

		}
			    break;
			default:
			    break;

			

		}
		
        
    }

	// Funkcja mechanizmu położenia obiektu z funkcją anulowania, położenia na mapie i cofnięcia

	public void SetStartingResourceCount(int count)
	{
		startingResourceCount = count;
	}

	// Ustawienie początkowej liczby zasobów

    
    public override void _Process(double delta)
	{   
		Vector2I mouseGridPosition = Vector2I.Zero;
		
		
		
		
		
		switch (currentState)
		{


			case State.Normal:
			    mouseGridPosition = gridManager.GetMouseGridCellPosition();
			    break;
			case State.PlaceingBuilding:
			    mouseGridPosition = gridManager.GetMouseGridCellPositionWithDimensionOffset(buildingGhostDimensions);
			    buildingGhost.GlobalPosition = mouseGridPosition * 64;
				break;
		}



		
		
		var rootCell = hoveredGridArea.Position;

		if (rootCell != mouseGridPosition)
		{
			hoveredGridArea.Position = mouseGridPosition;
			UpdateHoveredGridArea();

			

		}
			
			
			

			
			

			
		

		
	}


	

	
			
	

	private void UpdateGridDisplay()
	{
		
		

			gridManager.ClearHighlightedTiles();

			if (toPlaceBuildingResource.IsAttackBuilding())
			{

				gridManager.HighlightDangerOccupiedTiles();
				gridManager.HighlightBuildableTiles(true);



			}
			else {
				gridManager.HighlightBuildableTiles();
			    gridManager.HighlightDangerOccupiedTiles();

			}
			
			if (IsBuildingPlaceableAtArea(hoveredGridArea))
			{
				if (toPlaceBuildingResource.IsAttackBuilding())
			{
				gridManager.HighlightAttackTiles(hoveredGridArea, toPlaceBuildingResource.AttackRadius);
			}
			else
			{
				gridManager.HighlightExpandedBuildableTiles(hoveredGridArea , toPlaceBuildingResource.BuildableRadius);

			}
		
			
			gridManager.HighlightResourceTiles(hoveredGridArea, toPlaceBuildingResource.ResourceRadius);
			
			
			buildingGhost.SetValid();
			}
		
	
		else 
		{
			buildingGhost.SetInvalid();
		}
		buildingGhost.DoHoverAnimation();

	}
	
	private void PlaceBuildingAtHoveredCellPosition()
	{
		
		if (!CanAffordBuilding())
		{
			FloatingTextManager.ShowMessage("Can't afford!");
			return;
		}
		if (!IsBuildingPlaceableAtArea(hoveredGridArea))
		{
			FloatingTextManager.ShowMessage("Invalid placement!");
			return;

		}

		
		var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
		building.GlobalPosition = hoveredGridArea.Position * 64;
		var BuildingAnimator = building.GetFirstNodeOfType<BuildingAnimatorComponent>();
		if (BuildingAnimator != null){
		
			BuildingAnimator.PlayAnimation();
		}
		building.GetFirstNodeOfType<BuildingAnimatorComponent>()?.PlayAnimation();
		
		ySortRoot.AddChild(building);
		

	
		
		

		

		currentlyUsedResourceAccount += toPlaceBuildingResource.ResourceCost;

		ChangeState(State.Normal);
		EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
		
		
		
	}

	

	




	private void DestroyBuildingAtHoveredCellPosition()
	{
		var rootCell = hoveredGridArea.Position;
		var buildingComponent = BuildingComponent.GetValidBuildingComponents(this)
		    .FirstOrDefault((buildingComponent) =>
		    {
			    return buildingComponent.BuildingResource.IsDeletable && buildingComponent.IsTileInBuildingArea(rootCell);
		    });
		if (buildingComponent == null) return;
		if (!gridManager.CanDestroyBuilding(buildingComponent)) 
		{	
			FloatingTextManager.ShowMessage("Can't destroy!");
			return;
		}
		
		
		
		currentlyUsedResourceAccount -= buildingComponent.BuildingResource.ResourceCost;
		buildingComponent.Destroy();
		EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);

	}

	private void ClearBuildingGhost()
	{
		
		gridManager.ClearHighlightedTiles();
        if (IsInstanceValid(buildingGhost))
		{

            buildingGhost.QueueFree();
		}
		buildingGhost = null; 

	}

	private bool CanAffordBuilding()
	{
		return AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;
	}

	private bool IsBuildingPlaceableAtArea(Rect2I tileArea)
	{

		var isAttackTiles = toPlaceBuildingResource.AttackRadius > 0;
		var allTilesBuildable = gridManager.IsTileAreaBuildable(tileArea);
		return allTilesBuildable && CanAffordBuilding();
		

	}

	private List<Vector2I> GetTilePositionsInTileArea(Rect2I tileArea)
	{



		var result = new List<Vector2I>();
		for (int x = tileArea.Position.X; x < tileArea.End.X; x++ )
		{

			for (int y = tileArea.Position.Y; y < tileArea.End.Y; y++)
			{
				result.Add(new Vector2I(x, y));
			}
		}
		return result;
	}
	



	private void UpdateHoveredGridArea()
	{
		switch(currentState)
		{
			case State.Normal:
			    break;
			case State.PlaceingBuilding:
			    UpdateGridDisplay();
			    break;
		}
	}
	
	
	

	
	

	private void ChangeState(State toState)
	{
		switch(currentState)
		{
		    case State.Normal:
			    break;
			case State.PlaceingBuilding:
			    ClearBuildingGhost();
				toPlaceBuildingResource = null;
			    break;
		}

		currentState = toState;

		switch(currentState)
		{
			case State.Normal:
			    break;
			case State.PlaceingBuilding:
			    buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
		        ySortRoot.AddChild(buildingGhost);
			    break;
		}

	}

	private void OnResourceTilesUpdated(int resourceCount)
	{
		currentResourceCount = resourceCount;
		EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);

	}

	private void OnBuildingResourceSelected(BuildingResource buildingResource)
	{
		ChangeState(State.PlaceingBuilding);
		
		hoveredGridArea.Size = buildingResource.Dimensions;

		var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
		buildingGhost.AddSpriteNode(buildingSprite);
		buildingGhost.SetDimensions(buildingResource.Dimensions);
		buildingGhostDimensions = buildingResource.Dimensions;
		toPlaceBuildingResource = buildingResource;
		
		
		UpdateGridDisplay();
		
		
	}

	
}  // Ogólna klasa dla całości gry kładzenia obiektu na mapie



