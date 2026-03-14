namespace Sandbox;

/// <summary>
/// Keyboard-driven debug view controller for the Source2Triangle standalone app.
/// Attach this component to the camera <see cref="GameObject"/>.
/// <list type="bullet">
///   <item><b>F1</b> – cycle through render modes (Lit → Full Bright → Albedo → Normals → Roughness → Wireframe).</item>
///   <item><b>F2</b> – toggle physics-collider debug overlay (<c>physics_debug_draw</c>).</item>
///   <item><b>F3</b> – toggle hitbox debug overlay (<c>debug_hitbox</c>).</item>
/// </list>
/// </summary>
internal sealed class DebugViewController : Component
{
	/// <summary>Ordered list of render modes that F1 cycles through.</summary>
	static readonly (SceneCameraDebugMode Mode, bool Wireframe, string Label)[] RenderModes =
	[
		( SceneCameraDebugMode.Normal,     false, "Lit"            ),
		( SceneCameraDebugMode.FullBright,  false, "Full Bright"    ),
		( SceneCameraDebugMode.Albedo,      false, "Albedo"         ),
		( SceneCameraDebugMode.NormalMap,   false, "World Normals"  ),
		( SceneCameraDebugMode.Roughness,   false, "Roughness"      ),
		( SceneCameraDebugMode.Normal,      true,  "Wireframe"      ),
	];

	int _renderModeIndex;
	bool _physicsDebug;
	bool _hitboxDebug;

	/// <summary>
	/// Returns a reference to the sibling <see cref="CameraComponent"/>, or null.
	/// </summary>
	CameraComponent Cam => Components.Get<CameraComponent>( FindMode.InSelf );

	protected override void OnUpdate()
	{
		// F1 – cycle render mode
		if ( Input.Keyboard.Pressed( "f1" ) )
		{
			_renderModeIndex = ( _renderModeIndex + 1 ) % RenderModes.Length;
			ApplyRenderMode();
		}

		// F2 – toggle physics collider debug
		if ( Input.Keyboard.Pressed( "f2" ) )
		{
			_physicsDebug = !_physicsDebug;
			ConVarSystem.Run( $"physics_debug_draw {( _physicsDebug ? 1 : 0 )}" );
		}

		// F3 – toggle hitbox debug
		if ( Input.Keyboard.Pressed( "f3" ) )
		{
			_hitboxDebug = !_hitboxDebug;
			// debug_hitbox is a Protected ConVar; use internal ConVarSystem.Run to bypass
			// the ConsoleSystem.CanRunCommand guard (source2-triangle is in InternalsVisibleTo).
			ConVarSystem.Run( $"debug_hitbox {( _hitboxDebug ? 1 : 0 )}" );
		}
	}

	void ApplyRenderMode()
	{
		var cam = Cam;
		if ( cam is null ) return;

		var (mode, wire, _) = RenderModes[_renderModeIndex];
		cam.DebugMode = mode;
		cam.SceneCamera.WireframeMode = wire;
	}

	/// <summary>
	/// Draw the current debug state into the HUD overlay.
	/// Call this from the camera's <c>OnRenderOverlay</c> callback.
	/// </summary>
	internal void DrawHud()
	{
		var (_, _, label) = RenderModes[_renderModeIndex];

		float y = 88f;
		float lineH = 20f;

		Graphics.DrawText(
			new Rect( 10, y, 600, lineH ),
			$"[F1] Render mode: {label}  |  [F2] Physics: {On( _physicsDebug )}  |  [F3] Hitboxes: {On( _hitboxDebug )}",
			new Color( 0.7f, 1f, 0.7f ),
			fontSize: 14
		);
	}

	static string On( bool v ) => v ? "ON" : "off";
}
