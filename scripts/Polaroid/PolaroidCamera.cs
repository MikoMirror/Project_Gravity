using Godot;
using System;

public class PolaroidCamera
{
	private Camera3D _camera;
	private ColorRect _cameraOverlay;
	public bool InCameraMode { get; private set; } = false;

	public PolaroidCamera(Camera3D camera, ColorRect cameraOverlay)
	{
		_camera = camera;
		_cameraOverlay = cameraOverlay;
	}

	public void ToggleCameraMode(Player player)
	{
		InCameraMode = !InCameraMode;
		_cameraOverlay.Visible = InCameraMode;

		if (InCameraMode && player.IsLifting)
		{
			player.StopLifting();
		}
	}
}
