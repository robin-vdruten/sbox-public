using Sandbox.Diagnostics;
using System.Collections.Generic;

namespace Sandbox;

/// <summary>
/// Builds the demo game scene: a lit environment with a mesh model, physics, input
/// and a HUD overlay.  Replace or extend this class to build your own game.
/// </summary>
internal static class GameScene
{
	static CameraComponent Camera;
	static GameObject PhysicsBoxObject;

	// Ring buffer of recent engine log messages (errors and warnings) for on-screen display.
	static readonly Queue<LogEvent> _recentLogs = new();
	const int MaxLogLines = 8;

	internal static void Setup()
	{
		// Subscribe to engine log events so we can display errors and warnings on screen.
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

		// ── HUD overlay ───────────────────────────────────────────────────────
		Camera.SceneCamera.OnRenderOverlay = DrawHud;
	}

	// ── HUD drawn every frame via the camera overlay callback ─────────────────

	static void OnEngineLog( LogEvent e )
	{
		// Only surface warnings and errors in the on-screen console.
		if ( e.Level != LogLevel.Warn && e.Level != LogLevel.Error )
			return;

		_recentLogs.Enqueue( e );
		while ( _recentLogs.Count > MaxLogLines )
			_recentLogs.Dequeue();
	}

	static void DrawHud()
	{
		float fps = Time.Delta > 0f ? 1f / Time.Delta : 0f;

		// FPS counter – top-left corner
		Graphics.DrawText(
			new Rect( 10, 10, 400, 28 ),
			$"FPS: {fps:F0}",
			Color.White,
			fontSize: 20
		);

		// Controls help text
		Graphics.DrawText(
			new Rect( 10, 42, 500, 20 ),
			"Hold RMB + drag to orbit  |  Scroll wheel to zoom",
			new Color( 0.85f, 0.85f, 0.85f ),
			fontSize: 14
		);
		Graphics.DrawText(
			new Rect( 10, 62, 500, 20 ),
			"Press  R  to drop a new physics box  |  Esc  to quit",
			new Color( 0.85f, 0.85f, 0.85f ),
			fontSize: 14
		);

		// On-screen console: display recent engine warnings and errors at the bottom.
		if ( _recentLogs.Count > 0 )
		{
			float screenHeight = Screen.Height;
			float lineHeight = 16f;
			float y = screenHeight - ( _recentLogs.Count * lineHeight ) - 10f;

			foreach ( var entry in _recentLogs )
			{
				var color = entry.Level == LogLevel.Error
					? new Color( 1f, 0.3f, 0.3f )
					: new Color( 1f, 0.85f, 0.2f );

				var prefix = entry.Level == LogLevel.Error ? "[ERROR]" : "[WARN]";
				var logger = string.IsNullOrEmpty( entry.Logger ) ? string.Empty : $"[{entry.Logger}] ";

				Graphics.DrawText(
					new Rect( 10, y, Screen.Width - 20f, lineHeight ),
					$"{prefix} {logger}{entry.Message}",
					color,
					fontSize: 12
				);

				y += lineHeight;
			}
		}
	}
}
