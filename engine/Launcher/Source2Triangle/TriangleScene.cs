namespace Sandbox;

/// <summary>
/// Creates the minimal scene that the Source 2 Triangle Demo uses.
/// The scene contains a single camera whose overlay render-hook draws a
/// rainbow-coloured triangle every frame using the engine's managed
/// <see cref="Graphics"/> API.
/// </summary>
internal static class TriangleScene
{
	/// <summary>
	/// Called once after the engine is fully initialised.
	/// Sets up a <see cref="Scene"/> with a <see cref="CameraComponent"/> and
	/// registers <see cref="DrawTriangle"/> as the overlay render callback.
	/// </summary>
	internal static void Setup()
	{
		var scene = new Scene();
		scene.Name = "Triangle Demo";

		// Make this the active scene so GameInstanceDll.Tick() updates it
		// and GameInstanceDll.OnRender() renders it every frame.
		Game.ActiveScene = scene;
		Game.IsPlaying = true;

		// All scene-object creation must happen inside the scene scope so
		// that OnAwake() asserts (Game.ActiveScene == Scene) pass.
		using var _ = scene.Push();

		// Camera ──────────────────────────────────────────────────────────────
		var cameraGo = scene.CreateObject( true );
		cameraGo.Name = "Camera";
		var cam = cameraGo.AddComponent<CameraComponent>();

		cam.BackgroundColor = new Color( 0.06f, 0.06f, 0.12f );
		cam.FieldOfView = 70f;
		cam.ZNear = 5f;
		cam.ZFar = 50_000f;

		// Position the camera slightly back so we have a clear view of the origin.
		cameraGo.WorldPosition = new Vector3( 0f, -300f, 80f );
		cameraGo.WorldRotation = Rotation.LookAt( Vector3.Zero - cameraGo.WorldPosition );

		// Register the overlay render callback on the underlying SceneCamera.
		// OnRenderOverlay fires after post-process, still inside the render context,
		// so Graphics.Draw calls here appear on top of everything else.
		cam.SceneCamera.OnRenderOverlay = DrawTriangle;

	}

	// -------------------------------------------------------------------------
	// 2-D screen-space triangle (overlay)
	// -------------------------------------------------------------------------

	/// <summary>
	/// Draws a rainbow equilateral triangle centred in the viewport.
	/// Called by the camera's <see cref="SceneCamera.OnRenderOverlay"/> hook.
	/// </summary>
	static void DrawTriangle()
	{
		var vp = Graphics.Viewport;
		float cx = vp.Width * 0.5f;
		float cy = vp.Height * 0.5f;
		float size = MathF.Min( vp.Width, vp.Height ) * 0.28f;

		// Equilateral triangle vertices, pointing upward.
		const float sin60 = 0.866025f; // sin(60°)
		const float cos60 = 0.5f;      // cos(60°)

		Vertex[] verts =
		[
			// Top vertex – red
			new Vertex( new Vector3( cx, cy - size, 0f ), new Color32( 255, 50, 50 ) ),
			// Bottom-right vertex – green
			new Vertex( new Vector3( cx + sin60 * size, cy + cos60 * size, 0f ), new Color32( 50, 255, 50 ) ),
			// Bottom-left vertex – blue
			new Vertex( new Vector3( cx - sin60 * size, cy + cos60 * size, 0f ), new Color32( 50, 100, 255 ) ),
		];

		Graphics.Draw( verts, 3, Material.UI.Basic );

		// Label
		var labelRect = new Rect( 10, 10, vp.Width - 20, 40 );
		Graphics.DrawText( labelRect, "Source 2 Triangle Demo", Color.White.WithAlpha( 0.8f ), fontSize: 22f );
	}
}
