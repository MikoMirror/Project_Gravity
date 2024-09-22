using Godot;
using System;

public partial class MemoryPlatform : Node3D
{
	[Export] public NodePath AreaPath { get; set; } = "Area3D";
	[Export] public NodePath NeonPath { get; set; } = "NeonMesh";
	[Export] public bool IsActive { get; set; }

	private Area3D _area;
	private MeshInstance3D _neonMesh;
	private ShaderMaterial _neonMaterial;
	private bool _hasBeenActivated;
	private bool _isInteractive = true;
	private bool _isInRedState;
	private bool _isNeutral;
	private MemoryPuzle _memoryPuzzle;

	// Change this line to allow setting the property
	public bool HasBeenActivated 
	{ 
		get => _hasBeenActivated;
		set
		{
			_hasBeenActivated = value;
			UpdateNeonColor();
		}
	}

	private const string ActivateSound = "res://assets/Sounds/Platform_Activate.mp3";
	private SoundManager _soundManager;

	public override void _Ready()
	{
		InitializeComponents();
		ConnectSignals();
		UpdateNeonColor();
		_soundManager = GetNode<SoundManager>("/root/SoundManager");
	}

	private void InitializeComponents()
	{
		_area = GetNode<Area3D>(AreaPath);
		_neonMesh = GetNode<MeshInstance3D>(NeonPath);
		_memoryPuzzle = GetParent() as MemoryPuzle;

		if (_neonMesh != null)
		{
			_neonMaterial = (_neonMesh.GetActiveMaterial(0) as ShaderMaterial).Duplicate() as ShaderMaterial;
			 _neonMesh.SetSurfaceOverrideMaterial(0, _neonMaterial);
		}
	}

	private void ConnectSignals()
	{
		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
			_area.BodyExited += OnBodyExited;
		}
	}

	public void SetInteractive(bool interactive) => _isInteractive = interactive;
	public void ResetActivation() { HasBeenActivated = false; UpdateNeonColor(); }
	public void SetRedState() { _isInRedState = true; UpdateNeonColor(); }
	public void ResetColor() { _isInRedState = false; UpdateNeonColor(); }
	public void SetNeutral(bool neutral) { _isNeutral = neutral; UpdateNeonColor(); }

	private void OnBodyEntered(Node body)
	{
		if (!_isInteractive || !(body is CollisionObject3D) || _memoryPuzzle == null) return;

		if (IsActive && !HasBeenActivated && !_isInRedState)
		{
			PermanentlyActivate();
			_memoryPuzzle.UpdatePlatformState((int)(Position.Z / _memoryPuzzle.Spacing.Z), 
											  (int)(Position.X / _memoryPuzzle.Spacing.X), 
											  true);
			_soundManager.PlaySound(ActivateSound);
		}
		else if (!IsActive && !_isNeutral && !_memoryPuzzle.AllActiveSteppedOn)
		{
			_memoryPuzzle.UpdatePlatformState((int)(Position.Z / _memoryPuzzle.Spacing.Z), 
											  (int)(Position.X / _memoryPuzzle.Spacing.X), 
											  false);
		}

		UpdateNeonColor(true);
	}

	private void OnBodyExited(Node body)
	{
		if (body is CollisionObject3D)
		{
			UpdateNeonColor(false);
		}
	}

	private void PermanentlyActivate()
	{
		HasBeenActivated = true;
		UpdateNeonColor();
	}

	private void UpdateNeonColor(bool objectInside = false)
	{
		if (_neonMaterial == null) return;

		Vector3 color = GetColorForState(objectInside);
		_neonMaterial.SetShaderParameter("emission_color", color);
	}

	private Vector3 GetColorForState(bool objectInside)
	{
		if (!_isInteractive || _isNeutral) return new Vector3(0.5f, 0.5f, 0.5f);
		if (_isInRedState) return new Vector3(1.0f, 0.0f, 0.0f);
		if (HasBeenActivated) return new Vector3(0.0f, 0.5f, 1.0f);
		if (objectInside) return IsActive ? new Vector3(0.0f, 1.0f, 0.0f) : new Vector3(1.0f, 0.0f, 0.0f);
		return new Vector3(1.0f, 1.0f, 1.0f);
	}

	public void UpdateVisuals() => UpdateNeonColor();
}
