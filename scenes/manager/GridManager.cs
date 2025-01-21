using Game.Autoload;
using Game.Component;
using Game.Level.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;





namespace Game.Manager;

public partial class GridManager : Node
{
	private const string IS_BUILDABLE = "is_buildable";
	private const string IS_WOOD = "is_wood";
	private const string IS_IGNORED = "is_ignored";


	[Signal]

	public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);

	[Signal]

	public delegate void GridStateUpdatedEventHandler();

	

	

	

	private HashSet<Vector2I> validBuildableTiles = new();

	private HashSet<Vector2I> validBuildableAttackTiles = new();

	private HashSet<Vector2I> allTilesInBuildingRadius = new();

	private HashSet<Vector2I> collectedResourceTiles = new();

	private HashSet<Vector2I> occupiedTiles = new();

	private HashSet<Vector2I> goblinOccupiedTiles = new();

	private HashSet<Vector2I> attackTiles = new();




    [Export]

	private TileMapLayer highlightTilemapLayer;

	

    [Export]

	private TileMapLayer baseTerrainTilemapLayer;

	private List<TileMapLayer> allTilemapLayers = new();
	private Dictionary<TileMapLayer, ElevationLayer> tileMapLayerToElevationLayer = new();
	private Dictionary<BuildingComponent, HashSet<Vector2I>> buildingToBuildableTiles = new();
	private Dictionary<BuildingComponent, HashSet<Vector2I>> dangerBuildingToTiles = new();
	private Dictionary<BuildingComponent, HashSet<Vector2I>> attackBuildingToTiles = new();

	private Vector2I goldMinePosition;

	
	
	public override void _Ready()
	{
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced, Callable.From<BuildingComponent>(OnBuildingPlaced));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed, Callable.From<BuildingComponent>(OnBuildingDestroyed));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingEnabled, Callable.From<BuildingComponent>(OnBuildingEnabled));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingEnabled, Callable.From<BuildingComponent>(OnBuildingDisabled));
		
		allTilemapLayers = GetAllTilemapLayers(baseTerrainTilemapLayer);
		MapTileMapLayersToElevationLayers();
	}

	// Łączenie wszystkich funkcji w grze zaczynając od baseTerrainTilemapLayer
    public void SetGoldMinePosition(Vector2I position)
	{
		goldMinePosition = position;
	}
		

	public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)
	{

		foreach (var layer in allTilemapLayers)
		{
            var customData = layer.GetCellTileData(tilePosition);
			if (customData == null || (bool)customData.GetCustomData(IS_IGNORED)) continue;
			return (layer, (bool)customData.GetCustomData(dataName));
        }
		return (null, false);
		
		
		
	}




	public void HighlightExpandedBuildableTiles(Rect2I tileArea, int radius)
	{


		
		


		var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(occupiedTiles);
		
		
		
	
		var atlasCoords = new Vector2I(1, 0);
		foreach (var tilePosition in expandedTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);

		}

	}

	// budowa siatki


	public void HighlightAttackTiles(Rect2I tileArea, int radius)
	{

		var buildingAreaTiles = tileArea.ToTiles();
		var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet().Except(validBuildableAttackTiles).Except(buildingAreaTiles);
		var atlasCoords = new Vector2I(1, 0);
		foreach (var tilePosition in validTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);

		}

	}

	public void HighlightResourceTiles(Rect2I tileArea, int radius)
	{
		var resourceTiles = GetResourceTilesInRadius(tileArea, radius);
		var atlasCoords = new Vector2I(1, 0);
		foreach (var tilePosition in resourceTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);

		}



	}
		
	

	

	public bool IsTilePositionInAnyBuildingRadius(Vector2I tilePosition)
	{
		return allTilesInBuildingRadius.Contains(tilePosition);
	}

	// siatka dla każdej budowli po dodaniu liczby w edytorze.


	public bool IsTileAreaBuildable(Rect2I tileArea, bool isAttackTiles = false)
	{
		var tiles = new List<Vector2I>();
		for (int x = tileArea.Position.X; x < tileArea.End.X; x++ )
		{

			for (int y = tileArea.Position.Y; y < tileArea.End.Y; y++)
			{
				tiles.Add(new Vector2I(x, y));
			}
		}

		if (tiles.Count == 0) return false;

		(TileMapLayer firstTileMapLayer, _) = GetTileCustomData(tiles[0], IS_BUILDABLE);

		var targetElevationLayer = firstTileMapLayer != null ? tileMapLayerToElevationLayer[firstTileMapLayer] : null;

		var tileSetToCheck = GetBuildableTileSet(isAttackTiles);
		if (isAttackTiles)
		{
			
			tileSetToCheck = tileSetToCheck.Except(occupiedTiles).ToHashSet();
		}

		return tiles.All((tilePosition) => {
			(TileMapLayer tileMapLayer, bool isBuildable) = GetTileCustomData(tilePosition, IS_BUILDABLE);
			var elevationLayer =  tileMapLayer != null ? tileMapLayerToElevationLayer[tileMapLayer] : null;
			return isBuildable && tileSetToCheck.Contains(tilePosition) && elevationLayer == targetElevationLayer;

		});

		
	}


	public void HighlightDangerOccupiedTiles()
	{
		var atlasCoords = new Vector2I(2,0);
		foreach (var tilePosition in goblinOccupiedTiles) 
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, new Vector2I(2, 0));
		}
	}
	
	


	
	public void HighlightBuildableTiles(bool isAttackTiles = false)
	{
		foreach (var tilePosition in GetBuildableTileSet(isAttackTiles))
		{
			highlightTilemapLayer.SetCell(tilePosition, 0 , Vector2I.Zero);
		}
	}
	
	

		

		
		

	

	public void ClearHighlightedTiles()
	{
		highlightTilemapLayer.Clear();
	}

	// Funkcja usuwania siatek 

	public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions)
	{

		var mouseGridPosition = highlightTilemapLayer.GetGlobalMousePosition() / 64;
		mouseGridPosition -= dimensions / 2;
		mouseGridPosition = mouseGridPosition.Round();
		return new Vector2I((int)mouseGridPosition.X, (int)mouseGridPosition.Y);

    }

	 

	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTilemapLayer.GetGlobalMousePosition();
		return ConvertWorldPositionToTilePosition(mousePosition);

		// Położenie myszki na siatce mapy
		

	}

	public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
	{
		var tilePosition = worldPosition / 64;
		tilePosition = tilePosition.Floor();
		return new Vector2I((int)tilePosition.X, (int)tilePosition.Y);

	}

	//

	 

	

	public bool CanDestroyBuilding(BuildingComponent toDestroyBuildingComponent)
	{
		if (toDestroyBuildingComponent.BuildingResource.BuildableRadius > 0)
		{

			return !WillBuildingDestructionCreateOrphanBuildings(toDestroyBuildingComponent) && IsBuildingNetworkConnected(toDestroyBuildingComponent);

		}

		

		
			
		else if (toDestroyBuildingComponent.BuildingResource.IsAttackBuilding())
		{
			return CanDestroyBarracks(toDestroyBuildingComponent);
		}

		
		return true;
	    



	    

	}

	// Niszczenie budynku, razem z funkcją połączenia obiektów położonych na mapie 

	public HashSet<Vector2I> GetCollectedResourceTiles()
	{
		return collectedResourceTiles.ToHashSet();
	}


	private bool CanDestroyBarracks(BuildingComponent toDestroyBuildingComponent)
	{
	
		var disabledDangerBuildings = BuildingComponent.GetDangerBuildingComponents(this)
		.Where((buildingComponent) => buildingComponent.GetOccupiedCellPositions().Any((tilePosition) =>
		{
			return attackBuildingToTiles[toDestroyBuildingComponent].Contains(tilePosition);
		}));

		if (disabledDangerBuildings.Count() == 0) return true;

		var allDangerBuildingsStillDisabled = disabledDangerBuildings.All((dangerBuilding) => {
		return dangerBuilding.GetOccupiedCellPositions().Any((tilePosition) => {
			return attackBuildingToTiles.Keys.Where((attackBuilding) => attackBuilding != toDestroyBuildingComponent)
			    .Any((attackBuilding) => attackBuildingToTiles[attackBuilding].Contains(tilePosition));
		});

		});

		if (allDangerBuildingsStillDisabled) return true;
		
        var nonDangerBuildings = BuildingComponent.GetNodeDangerBuildingComponents(this).Where((nonDangerBuilding) => {
			return nonDangerBuilding != toDestroyBuildingComponent;
		});
		var anyDangerBuildingContainsPlayerBuilding = disabledDangerBuildings.Any((dangerBuilding) => {

			var dangerTiles = dangerBuildingToTiles[dangerBuilding];
			return nonDangerBuildings.Any((nonDangerBuilding) => {
				return nonDangerBuilding.GetOccupiedCellPositions().Any((tilePosition) => dangerTiles.Contains(tilePosition));


			});
		

		});

		return !anyDangerBuildingContainsPlayerBuilding;

	}

	// Możliwość zniszczenia baraku po dodaniu obok baraku innych obiektów

	private bool WillBuildingDestructionCreateOrphanBuildings(BuildingComponent toDestroyBuildingComponent)
	{
		var dependentBuildings = BuildingComponent.GetNodeDangerBuildingComponents(this)
	            .Where((buildingComponent) => 
		    {

				if (buildingComponent == toDestroyBuildingComponent) return false;
				if (buildingComponent.BuildingResource.IsBase) return false;
			    var anyTilesInRadius = buildingComponent.GetOccupiedCellPositions().Any((tilePosition) => buildingToBuildableTiles[toDestroyBuildingComponent].Contains(tilePosition));
			    return anyTilesInRadius;
			});

        var allBuildingsStillValid = dependentBuildings.All((dependentBuilding) => 
		{
				var tilesForBuilding = dependentBuilding.GetOccupiedCellPositions();
				var buildingsToCheck = buildingToBuildableTiles.Keys
					.Where((key) => key != toDestroyBuildingComponent && key != dependentBuilding);
				
				
			return tilesForBuilding.All((tilePosition) => 
			    {
					var tileIsInSet = buildingsToCheck
					    .Any((buildingComponent) => buildingToBuildableTiles[buildingComponent].Contains(tilePosition));
                    return tileIsInSet;
				});

			});

			if (!allBuildingsStillValid)
			{
				return false;
			}

			return false;
		

	}

	// niszczenie ostatnio położonej wieży, która nie tworzy tzw. mostu do gold mine 

	


	private bool IsBuildingNetworkConnected(BuildingComponent toDestroyBuildingComponent)
	{
		var baseBuilding = BuildingComponent.GetValidBuildingComponents(this)
		    .First((buildingComponent) => buildingComponent.BuildingResource.IsBase);
		

        var visitedBuildings = new HashSet<BuildingComponent>();
		VisitAllConnectBuildings(baseBuilding, toDestroyBuildingComponent, visitedBuildings);

		var totalBuildingsToVisit = BuildingComponent.GetValidBuildingComponents(this)
		    .Count((buildingComponent) => { return buildingComponent != toDestroyBuildingComponent && buildingComponent.BuildingResource.BuildableRadius > 0;
			});
		
		
		

		return totalBuildingsToVisit == visitedBuildings.Count;
	}

	// Ogólna prywatna funkcja z połączeniem wszystkich obiektów 

	private void VisitAllConnectBuildings(
	   BuildingComponent rootBuilding, 
	   BuildingComponent excludeBuilding, 
	   HashSet<BuildingComponent> visitedBuildings
	   )
	{
		var dependentBuildings = BuildingComponent.GetValidBuildingComponents(this)
	            .Where((buildingComponent) => 
		    {
				
				if (buildingComponent.BuildingResource.BuildableRadius == 0) return false;
				if (visitedBuildings.Contains(buildingComponent)) return false;
			    var anyTilesInRadius = buildingComponent.GetOccupiedCellPositions()
				.All((tilePosition) => buildingToBuildableTiles[rootBuilding].Contains(tilePosition));
			    return buildingComponent != excludeBuilding && anyTilesInRadius;
			}).ToList();
		
		
		visitedBuildings.UnionWith(dependentBuildings);

		
		foreach (var dependentBuilding in dependentBuildings)
		{
			VisitAllConnectBuildings(dependentBuilding, excludeBuilding, visitedBuildings);
		}
		

	}

	// Funkcja po której wieża po połączeniach nie niszczy się.

	private HashSet<Vector2I> GetBuildableTileSet(bool isAttackTiles = false)
	{
		return isAttackTiles ? validBuildableAttackTiles : validBuildableTiles;

		
	}

	// położenie obiektu/obiektów na siatce

	private List<TileMapLayer> GetAllTilemapLayers(Node2D rootNode)
	{
		var result = new List<TileMapLayer>();
		var children = rootNode.GetChildren();
		children.Reverse();
		foreach (var child in children)
		{
			if (child is Node2D childNode)
			{
				result.AddRange(GetAllTilemapLayers(childNode));

			}
		}

		if (rootNode is TileMapLayer tileMapLayer)
		{
			result.Add(tileMapLayer);

		}

		
		return result;

	}

	// lista "obiektów" położonych na mapie (np: GrassLayer, ShadowLayer)

	private void MapTileMapLayersToElevationLayers()
	{
		foreach (var layer in allTilemapLayers)
		{
			ElevationLayer elevationLayer;
			Node startNode = layer;
			do {
				var parent = startNode.GetParent();
				elevationLayer = parent as ElevationLayer;
				startNode = parent;
			} while (elevationLayer == null && startNode != null);

			tileMapLayerToElevationLayer[layer] = elevationLayer;
		}
	}

	// dodanie Tilemapów do Elevationlayers 


	private void UpdateDangerOccupiedTiles(BuildingComponent buildingComponent)
	{
        occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());

		if (buildingComponent.BuildingResource.IsDangerBuilding())
		{
		    var tileArea = buildingComponent.GetTileArea();
		    var tilesInRadius = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.DangerRadius).ToHashSet();

			dangerBuildingToTiles[buildingComponent] = tilesInRadius.ToHashSet();

		    
		
		
		    if (!buildingComponent.IsDisabled)
		    {
		   
		        tilesInRadius.ExceptWith(occupiedTiles);
		        goblinOccupiedTiles.UnionWith(tilesInRadius);
			}
		}

	}

	// Aktualizacja siatek po dodaniu czerwonego tilesta dla baraku goblinów.





	
	private void  UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		
		occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
		
		var tileArea = buildingComponent.GetTileArea();


		
		if (buildingComponent.BuildingResource.BuildableRadius > 0)
		{
			var allTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius, (tileposition) => true);
		    allTilesInBuildingRadius.UnionWith(allTiles);
		    var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
		    buildingToBuildableTiles[buildingComponent] = validTiles.ToHashSet();
		    validBuildableTiles.UnionWith(validTiles);
		}
		
		validBuildableTiles.ExceptWith(occupiedTiles);
		validBuildableTiles.ExceptWith(goblinOccupiedTiles);
		
		EmitSignal(SignalName.GridStateUpdated);

		

	}

	// aktualizacja błednych siatek na mapie (np: położenie między drzewem a "village")

	

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{

		
		var tileArea = buildingComponent.GetTileArea();
		var resourceTiles = GetResourceTilesInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);

		var oldResourceTileCount = collectedResourceTiles.Count;
		
		collectedResourceTiles.UnionWith(resourceTiles);

		if (oldResourceTileCount != collectedResourceTiles.Count)
		{
		    EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		}
		EmitSignal(SignalName.GridStateUpdated);

	}

	// Funkcja aktualizująca siatke

	private void UpdateAttackTiles(BuildingComponent buildingComponent)
	{
		if (!buildingComponent.BuildingResource.IsAttackBuilding()) return;
		
		
		
		var tileArea = buildingComponent.GetTileArea();

		

		var newAttackTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.AttackRadius, (_) => true)
		    .ToHashSet();
		attackBuildingToTiles[buildingComponent] = newAttackTiles;
		attackTiles.UnionWith(newAttackTiles);
	}

	// siatki atakujące barak goblinów


	private void RecalculateGrid() 
	{
		occupiedTiles.Clear();
		validBuildableTiles.Clear();
		validBuildableAttackTiles.Clear();
		allTilesInBuildingRadius.Clear();
		collectedResourceTiles.Clear();
		goblinOccupiedTiles.Clear();
		attackTiles.Clear();
		buildingToBuildableTiles.Clear();
		dangerBuildingToTiles.Clear();
		attackBuildingToTiles.Clear();
		
		
		var buildingComponents = BuildingComponent.GetValidBuildingComponents(this);
		

		foreach (var buildingComponent in buildingComponents)
		{
			UpdateBuildingComponentGridState(buildingComponent);
			


		}

		CheckDangerBuildingDestruction();

		EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		EmitSignal(SignalName.GridStateUpdated);


	}

	// ogólna rekalkulacja/odjęcie siatek ze wszystkimi obiektami obejmującymi je razem ze zniszczeniem któregoś z nich

	private void RecalculateDangerOccupiedTiles()
	{

        goblinOccupiedTiles.Clear();
		var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);

		foreach (var building in dangerBuildings)
		{
			UpdateDangerOccupiedTiles(building);
		}
		
	}

	// rekalkulacja/odjęcie siatek po położeniu obiektu np. (położenie wieży po odjęciu siatek przy baraku).

	private void CheckDangerBuildingDestruction()
	{
		
		var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
		foreach (var building in dangerBuildings)
		{
			var tileArea = building.GetTileArea();
			var isInsideAttackTile = tileArea.ToTiles().Any((tilePosition) => attackTiles.Contains(tilePosition));
			if (isInsideAttackTile)
			{
			  
               building.Disable();
			}
			else 
			{
				building.Enable();

			}
		}

		
	}

	// Sprawdzanie siatek dla tzw.niebezpiecznych budynków 

	private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
	{
		var distanceX = centerPosition.X - (tilePosition.X + 0.5);
		var distanceY = centerPosition.Y - (tilePosition.Y + 0.5);
		var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
		return distanceSquared <= radius * radius;
	}

	// funkcja opisująca odległość pomiędzy środkiem a wykresem funkcji X i Y.

	private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
	{
		var result = new List<Vector2I>();
		var tileAreaF = tileArea.ToRect2F();
		var tileAreaCenter = tileArea.GetCenter();
		var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;
		for (var x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
		{
			for (var y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x,y);
				if (!IsTileInsideCircle(tileAreaCenter, tilePosition, radius + radiusMod) || !filterFn(tilePosition)) continue;
				result.Add(tilePosition);
			}

		}
		return result;

	}

	// Funkcja położenia następnej siatki po położeniu obiektu dzieląca ją na 2.

	

	private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
	{
		 return GetTilesInRadius(tileArea, radius, (tilePosition) =>
		{
			return GetTileCustomData(tilePosition, IS_BUILDABLE).Item2 || tilePosition == goldMinePosition;
		} );


	}

	// Błędne siatki blokujące położenie obiektu

	private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
	{
		return GetTilesInRadius(tileArea, radius, (tilePosition) =>
		{
			return GetTileCustomData(tilePosition, IS_WOOD).Item2;
		} );

	}

	private void UpdateBuildingComponentGridState(BuildingComponent buildingComponent)
	{
		UpdateDangerOccupiedTiles(buildingComponent);
		UpdateValidBuildableTiles(buildingComponent);
		UpdateCollectedResourceTiles(buildingComponent);
		UpdateAttackTiles(buildingComponent);
	}

	// Ogół łączący wszystkie komponenty na siatce

	
	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateBuildingComponentGridState(buildingComponent);
		CheckDangerBuildingDestruction();
		
		
	}

	// Położenie obiektu razem z funkcją sprawdzającą aktualizowanie budowy na siatce razem z niebezpiecznymi siatkami



	private void OnBuildingDestroyed(BuildingComponent buildingComponent)
	{

		RecalculateGrid();



	}

	// Po zniszczeniu budynku rekalkulacja całej siatki

	private void OnBuildingEnabled(BuildingComponent buildingComponent)
	{
       
        UpdateBuildingComponentGridState(buildingComponent);
	}

	// Aktualizowanie siatki budynku po położeniu go na siatce mapy


	private void OnBuildingDisabled(BuildingComponent buildingComponent)
	{

		RecalculateGrid();


	}

    

};
