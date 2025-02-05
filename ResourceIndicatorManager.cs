using System.Collections.Generic;
using System.Linq;
using Game.Manager;
using Game.UI;
using Godot;


public partial class ResourceIndicatorManager : Node
{
	[Export]

	private GridManager gridManager;

	[Export]

	private PackedScene resourceIndicatorScene;

	
	private AudioStreamPlayer audioStreamPlayer;
	
	
	private HashSet<Vector2I> indicatedTiles = new();

	private Dictionary<Vector2I, ResourceIndicator> tileToResourceIndicator = new();

	public override void _Ready()

	{
		audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer"); 

		gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
	}

	// Funkcja wyrównująca siatkę po aktualizacji jej w kodzie obejmującą GridManager



	private void UpdateIndicators(IEnumerable<Vector2I> newlyIndicatedTiles, IEnumerable<Vector2I> toRemoveTiles)
	{
		if (newlyIndicatedTiles.Any())
		{
			audioStreamPlayer.Play();
		}
		foreach (var newTile in newlyIndicatedTiles)
		{
			var indicator = resourceIndicatorScene.Instantiate<ResourceIndicator>();
			AddChild(indicator);
			indicator.GlobalPosition = newTile * 64;
			tileToResourceIndicator[newTile] = indicator;


		}

		foreach (var removeTile in toRemoveTiles)
		{
			tileToResourceIndicator.TryGetValue(removeTile, out var indicator);
			if (IsInstanceValid(indicator))
			{
				indicator.Destroy();
			}
			tileToResourceIndicator.Remove(removeTile);
		}

	}

	

	private void HandleResourceTilesUpdate()
	{
		
		var currentResourceTiles = gridManager.GetCollectedResourceTiles();
		var newlyIndicatedTiles = currentResourceTiles.Except(indicatedTiles);
		var toRemoveTiles = indicatedTiles.Except(currentResourceTiles);
		indicatedTiles = currentResourceTiles;
		UpdateIndicators(newlyIndicatedTiles, toRemoveTiles);


	}

	private void OnResourceTilesUpdated(int _)
	{
		
		Callable.From(HandleResourceTilesUpdate).CallDeferred();
		
	}
}
	
