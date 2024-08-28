using Godot;
using System;

public partial class GameState : Node
{
	public Vector3 PlayerPosition { get; set; }
	public string TargetPortalName { get; set; }
	public bool IsComingFromPortal { get; set; } = false;

	public void StorePlayerData(Player player, string targetPortalName)
	{
		PlayerPosition = player.GlobalTransform.Origin;
		TargetPortalName = targetPortalName;
	}
}
