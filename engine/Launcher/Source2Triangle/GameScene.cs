using Sandbox.Diagnostics;
using System.Collections.Generic;

namespace Sandbox;

/// <summary>
/// Builds the demo game scene: a lit environment with a mesh model, physics, input,
/// a Razor-style UI HUD with entity inspector, and an animated skinned model.
/// Replace or extend this class to build your own game.
/// </summary>
internal static class GameScene
{
	static CameraComponent Camera;
	static GameObject PhysicsBoxObject;

	// Ring buffer of recent engine log messages (errors and warnings) shown in the HUD.
	static readonly Queue<LogEvent> _recentLogs = new();
	const int MaxLogLines = 8;

	internal static void Setup()
	{
		// Subscribe to engine log events so we can surface errors and warnings in the HUD.
		Logging.OnMessage += OnEngineLog;

		var scene = new Scene();
		scene.Name = "Game Demo";

		Game.ActiveScene = scene;
		Game.IsPlaying = true;

		using var _ = scene.Push();

		// ── Camera ───────────────────────────────────────────────────────────
		var cameraGo = scene.CreateObject( true );
		cameraGo.Name = "Camera";
		Camera = cameraGo.AddComponent<CameraComponent>();
		Camera.BackgroundColor = new Color( 0.06f, 0.06f, 0.12f );
		Camera.FieldOfView = 70f;
		cameraGo.WorldPosition = new Vector3( 0f, -300f, 80f );
		cameraGo.WorldRotation = Rotation.LookAt( Vector3.Zero - cameraGo.WorldPosition );
		// Orbit camera: hold RMB and drag to rotate, scroll to zoom
		cameraGo.AddComponent<OrbitCameraController>();

		// ── Lighting ─────────────────────────────────────────────────────────
		var ambGo = scene.CreateObject( true );
		ambGo.Name = "Ambient Light";
		var amb = ambGo.AddComponent<AmbientLight>();
		amb.Color = Color.White * 0.3f;

		var sunGo = scene.CreateObject( true );
		sunGo.Name = "Sun";
		var sun = sunGo.AddComponent<DirectionalLight>();
		sun.LightColor = Color.White;
		sun.Shadows = true;
		sunGo.WorldRotation = new Angles( 60f, 45f, 0f );

		// ── Floor ─────────────────────────────────────────────────────────────
		// A large flat static surface for physics objects to rest on.
		var floorGo = scene.CreateObject( true );
		floorGo.Name = "Floor";
		floorGo.WorldPosition = Vector3.Zero;
		var floorRenderer = floorGo.AddComponent<ModelRenderer>();
		floorRenderer.Model = Model.Load( "models/dev/plane.vmdl" );
		var floorCol = floorGo.AddComponent<BoxCollider>();
		// Match the plane model footprint (~512 units) and give it a thin depth.
		floorCol.Scale = new Vector3( 1024f, 1024f, 8f );
		floorCol.Center = new Vector3( 0f, 0f, -4f );

		// ── Main scene model with meshes ───────────────────────────────────────
		// This is the model the demo loads and displays.  Swap the path for any
		// .vmdl that lives under your game/core folder.
		var modelGo = scene.CreateObject( true );
		modelGo.Name = "Scene Model";
		modelGo.WorldPosition = new Vector3( 0f, 0f, 40f );
		var renderer = modelGo.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "models/dev/box.vmdl" );
		// Slow auto-rotation so the model is clearly visible
		modelGo.AddComponent<ModelRotator>();

		// ── Animated skinned model (citizen) ─────────────────────────────────
		// SkinnedModelRenderer drives bone animations.  The model's built-in
		// animation graph plays automatically; swap UseAnimGraph = false and set
		// Sequence.Name to play a specific clip by name instead.
		//
		// Equivalent in a .razor PanelComponent or in scene setup code:
		//   var smr = citizenGo.AddComponent<SkinnedModelRenderer>();
		//   smr.Model = Model.Load("models/citizen_human/citizen_human_male.vmdl");
		//   smr.UseAnimGraph = true;   // let the built-in animgraph run
		//   // – or –
		//   smr.UseAnimGraph = false;
		//   smr.Sequence.Name = "idle";  // play a named sequence directly
		var citizenGo = scene.CreateObject( true );
		citizenGo.Name = "Animated Citizen";
		citizenGo.WorldPosition = new Vector3( -120f, 0f, 0f );
		citizenGo.WorldRotation = new Angles( 0f, 90f, 0f );
		var smr = citizenGo.AddComponent<SkinnedModelRenderer>();
		smr.Model = Model.Load( "models/citizen_human/citizen_human_male.vmdl" );
		// The citizen model ships with an animation graph; enable it so the idle
		// animation plays automatically via the built-in animgraph.
		smr.UseAnimGraph = true;

		// ── Physics box (dynamic – falls and bounces on the floor) ────────────
		PhysicsBoxObject = scene.CreateObject( true );
		PhysicsBoxObject.Name = "Physics Box";
		PhysicsBoxObject.WorldPosition = new Vector3( 80f, 0f, 300f );
		var physRenderer = PhysicsBoxObject.AddComponent<ModelRenderer>();
		physRenderer.Model = Model.Load( "models/dev/box.vmdl" );
		physRenderer.Tint = new Color( 1f, 0.4f, 0.1f );
		var physCol = PhysicsBoxObject.AddComponent<BoxCollider>();
		physCol.Scale = new Vector3( 50f, 50f, 50f );
		PhysicsBoxObject.AddComponent<Rigidbody>();
		// Reset spawner component handles the "R" key
		PhysicsBoxObject.AddComponent<PhysicsBoxReset>();

		// ── Razor-style HUD & entity inspector ───────────────────────────────
		// ScreenPanel acts as the UI root (renders all child PanelComponents to
		// the screen).  GameHudComponent is the PanelComponent that builds the
		// FPS counter, control hints, log console, and entity inspector.
		//
		// In a sbox game addon you would simply add a .razor file that
		// @inherits PanelComponent and the toolchain compiles it for you.
		var uiGo = scene.CreateObject( true );
		uiGo.Name = "HUD";
		uiGo.AddComponent<ScreenPanel>();
		var hud = uiGo.AddComponent<GameHudComponent>();

		// Pass shared state to the HUD so it can populate the entity list and
		// show log messages without needing a static reference to GameScene.
		hud.TrackedEntities = new (string, GameObject)[]
		{
			("Scene Model",       modelGo),
			("Animated Citizen",  citizenGo),
			("Physics Box",       PhysicsBoxObject),
			("Floor",             floorGo),
		};
		hud.RecentLogs = _recentLogs;
	}

	// ── Engine log subscription ───────────────────────────────────────────────

	static void OnEngineLog( LogEvent e )
	{
		// Only surface warnings and errors in the on-screen console.
		if ( e.Level != LogLevel.Warn && e.Level != LogLevel.Error )
			return;

		_recentLogs.Enqueue( e );
		while ( _recentLogs.Count > MaxLogLines )
			_recentLogs.Dequeue();
	}
}
