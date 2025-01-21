using Godot;


using System.Linq;


namespace Game;


public static class NodeExtentions
{
	public static T GetFirstNodeOfType<T>(this Node node) where T : Node
    {
        var children = node.GetChildren();
        var firstNode = children.FirstOrDefault((child) => child is T);
        return firstNode as T;

    }

}