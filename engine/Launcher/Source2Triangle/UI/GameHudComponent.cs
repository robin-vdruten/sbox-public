using System.Collections.Generic;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Sandbox;

/// <summary>
/// Razor-style HUD and entity-inspector component for the Source 2 Triangle demo.
///
/// This component is implemented in plain C# because the Source2Triangle project
/// is a standalone .NET launcher that does not go through the sbox Razor-compiler
/// pipeline.  In a real sbox game addon (any project compiled by the sbox
/// toolchain) you would create this as a .razor file:
///
/// <code>
/// @using Sandbox;
/// @using Sandbox.UI;
/// @inherits PanelComponent
/// @namespace Sandbox
///
/// <root style="position:absolute;width:100%;height:100%;pointer-events:none">
///
///   <!-- Left side: FPS + hints + log console -->
///   <div style="position:absolute;left:12px;top:12px;pointer-events:none">
///     <label style="color:white;font-size:22px;font-weight:700">FPS: @Fps</label>
///     <label style="color:#ccc;font-size:14px">Hold RMB + drag to orbit   |   Scroll wheel to zoom</label>
///     <label style="color:#ccc;font-size:14px">Press R to drop a physics box   |   Esc to quit</label>
///   </div>
///
///   <!-- Log console -->
///   <div style="position:absolute;left:12px;bottom:10px;width:700px;pointer-events:none">
///     @foreach ( var entry in RecentLogs )
///     {
///       <label style="color:@LogColor(entry);font-size:12px">@LogText(entry)</label>
///     }
///   </div>
///
///   <!-- Right side: Entity inspector -->
///   <div style="position:absolute;right:12px;top:12px;width:290px;
///               background-color:rgba(13,13,26,0.85);border-radius:8px;
///               padding:12px 14px;pointer-events:all">
///     <label style="color:white;font-size:16px;font-weight:700">Entity Inspector</label>
///
///     @foreach ( var (name, go) in TrackedEntities )
///     {
///       <div style="padding:5px 8px;margin-bottom:3px;border-radius:4px;
///                   background-color:@(Selected==go ? "#406db3" : "#2e2e47")"
///            @onclick="() => SelectEntity(go)">
///         <label style="color:white;font-size:13px">@name</label>
///       </div>
///     }
///
///     @if ( Selected != null )
///     {
///       <label style="color:#e6e6e6;font-size:14px;font-weight:700">@Selected.Name</label>
///       <label style="color:#b3b3cc;font-size:12px;font-weight:700">Position</label>
///       <SliderControl Min="-500" Max="500" Value:bind="@PosX" />
///       <SliderControl Min="-500" Max="500" Value:bind="@PosY" />
///       <SliderControl Min="0"    Max="500" Value:bind="@PosZ" />
///     }
///   </div>
///
/// </root>
///
/// @code {
///   float Fps => Time.Delta > 0 ? 1f / Time.Delta : 0;
///   float PosX { get => Selected?.WorldPosition.x ?? 0; set => MoveSelected(x: value); }
///   float PosY { get => Selected?.WorldPosition.y ?? 0; set => MoveSelected(y: value); }
///   float PosZ { get => Selected?.WorldPosition.z ?? 0; set => MoveSelected(z: value); }
///   protected override int BuildHash() =>
///       System.HashCode.Combine(Fps, Selected, Selected?.WorldPosition);
/// }
/// </code>
/// </summary>
internal sealed class GameHudComponent : PanelComponent , Sandbox.Internal.IUpdateSubscriber
{
	/// <summary>Set by <see cref="GameScene"/> before the first update.</summary>
	internal IReadOnlyList<(string Name, GameObject Object)> TrackedEntities { get; set; }

	/// <summary>Shared ring-buffer of recent log messages from <see cref="GameScene"/>.</summary>
	internal Queue<LogEvent> RecentLogs { get; set; }

	// ── Panel references (set in OnStart) ────────────────────────────────────────
	Label _fpsLabel;
	Panel _logContainer;
	Panel _entityListContainer;
	Panel _inspectorContent;
	Label _selectedName;
	//AxisSlider _xSlider;
	//AxisSlider _ySlider;
	//AxisSlider _zSlider;

	// ── Runtime state ───────────────────────────────────────────────────────────
	GameObject _selected;
	Panel _activeEntityBtn;
	bool _entitiesBuilt;

	// Build the panel tree once on startup
	protected override void OnStart()
	{
		var root = Panel;
		root.Style.Position = PositionMode.Absolute;
		root.Style.Left = 0;
		root.Style.Top = 0;
		root.Style.Width = Length.Percent( 100 );
		root.Style.Height = Length.Percent( 100 );
		root.Style.PointerEvents = PointerEvents.None;

		BuildLeftHud( root );
		BuildInspectorPanel( root );
	}

	void BuildLeftHud( Panel root )
	{
		// Top-left column: FPS + control hints
		var left = root.AddChild<Panel>();
		left.Style.Position = PositionMode.Absolute;
		left.Style.Left = 12;
		left.Style.Top = 12;
		left.Style.PointerEvents = PointerEvents.None;

		_fpsLabel = left.Add.Label( "FPS: \u2013" );

		_fpsLabel.Style.FontColor = Color.White;
		_fpsLabel.Style.FontSize = 22;
		_fpsLabel.Style.FontWeight = 700;

		AddHint( left, "Hold  RMB + drag  to orbit   |   Scroll wheel to zoom" );
		AddHint( left, "Press  R  to drop a physics box   |   Esc  to quit" );
		AddHint( left, "Animated model: citizen_human_male with SkinnedModelRenderer" );

		// Log console – anchored to the bottom-left
		_logContainer = root.AddChild<Panel>();
		_logContainer.Style.Position = PositionMode.Absolute;
		_logContainer.Style.Left = 12;
		_logContainer.Style.Bottom = 10;
		_logContainer.Style.Width = 700;
		_logContainer.Style.PointerEvents = PointerEvents.None;
	}

	static void AddHint( Panel parent, string text )
	{
		var lbl = parent.Add.Label( text );
		lbl.Style.FontColor = new Color( 0.78f, 0.78f, 0.78f );
		lbl.Style.FontSize = 14;
		lbl.Style.MarginTop = 2;
	}

	void BuildInspectorPanel( Panel root )
	{
		var panel = root.AddChild<Panel>();
		panel.Style.Position = PositionMode.Absolute;
		panel.Style.Right = 12;
		panel.Style.Top = 12;
		panel.Style.Width = 290;
		panel.Style.BackgroundColor = new Color( 0.05f, 0.05f, 0.10f, 0.88f );
		panel.Style.PaddingLeft = 14;
		panel.Style.PaddingRight = 14;
		panel.Style.PaddingTop = 12;
		panel.Style.PaddingBottom = 14;
		panel.Style.BorderTopRightRadius = 8;
		panel.Style.PointerEvents = PointerEvents.All;

		// Title
		var title = panel.Add.Label( "Entity Inspector" );
		title.Style.FontColor = Color.White;
		title.Style.FontSize = 16;
		title.Style.FontWeight = 700;
		title.Style.MarginBottom = 10;

		// Entity list
		_entityListContainer = panel.AddChild<Panel>();
		_entityListContainer.Style.MarginBottom = 10;

		// ── Detail section (hidden until an entity is selected) ───────────────────
		_inspectorContent = panel.AddChild<Panel>();
		_inspectorContent.Style.Display = DisplayMode.None;

		_selectedName = _inspectorContent.Add.Label( string.Empty );
		_selectedName.Style.FontColor = new Color( 0.92f, 0.92f, 0.92f );
		_selectedName.Style.FontSize = 14;
		_selectedName.Style.FontWeight = 700;
		_selectedName.Style.MarginBottom = 8;

		// Divider
		var sep = _inspectorContent.AddChild<Panel>();
		sep.Style.Height = 1;
		sep.Style.BackgroundColor = new Color( 1f, 1f, 1f, 0.12f );
		sep.Style.MarginBottom = 8;

		// "Position" heading
		var posHeading = _inspectorContent.Add.Label( "Position" );
		posHeading.Style.FontColor = new Color( 0.68f, 0.68f, 0.82f );
		posHeading.Style.FontSize = 12;
		posHeading.Style.FontWeight = 700;
		posHeading.Style.MarginBottom = 6;

		//// X slider
		//_xSlider = _inspectorContent.AddChild<AxisSlider>();
		//_xSlider.Setup( "X", new Color( 1f, 0.35f, 0.35f ), -500f, 500f );
		//_xSlider.OnValueChanged = v =>
		//{
		//	if ( _selected.IsValid() )
		//		_selected.WorldPosition = _selected.WorldPosition.WithX( v );
		//};

		//// Y slider
		//_ySlider = _inspectorContent.AddChild<AxisSlider>();
		//_ySlider.Setup( "Y", new Color( 0.35f, 1f, 0.35f ), -500f, 500f );
		//_ySlider.OnValueChanged = v =>
		//{
		//	if ( _selected.IsValid() )
		//		_selected.WorldPosition = _selected.WorldPosition.WithY( v );
		//};

		//// Z slider
		//_zSlider = _inspectorContent.AddChild<AxisSlider>();
		//_zSlider.Setup( "Z", new Color( 0.35f, 0.55f, 1f ), 0f, 500f );
		//_zSlider.OnValueChanged = v =>
		//{
		//	if ( _selected.IsValid() )
		//		_selected.WorldPosition = _selected.WorldPosition.WithZ( v );
		//};
	}

	// Per-frame updates
	protected override void OnUpdate()
	{
		UpdateFps();

		// Build the entity list on the first frame it becomes available
		if ( !_entitiesBuilt && TrackedEntities is { Count: > 0 } )
		{
			PopulateEntityList();
			_entitiesBuilt = true;
		}

		// Keep position sliders in sync with the live scene transform
		if ( _selected.IsValid() )
		{
			var pos = _selected.WorldPosition;
			//_xSlider?.SetValueWithoutCallback( pos.x );
			//_ySlider?.SetValueWithoutCallback( pos.y );
			//_zSlider?.SetValueWithoutCallback( pos.z );
		}

		UpdateLogConsole();
	}

	void UpdateFps()
	{
		if ( _fpsLabel is null ) return;
		float fps = Time.Delta > 0f ? 1f / Time.Delta : 0f;
		_fpsLabel.Text = $"FPS: {fps:F0}";
	}

	void PopulateEntityList()
	{
		if ( _entityListContainer is null ) return;

		foreach ( var (name, go) in TrackedEntities )
		{
			var btn = _entityListContainer.AddChild<Panel>();
			btn.Style.PaddingLeft = 8;
			btn.Style.PaddingRight = 8;
			btn.Style.PaddingTop = 5;
			btn.Style.PaddingBottom = 5;
			btn.Style.MarginBottom = 3;
			btn.Style.BackgroundColor = new Color( 0.18f, 0.18f, 0.28f );
			btn.Style.BorderTopRightRadius = 4;
			btn.Style.Cursor = "pointer";

			// Forward button input to the game so clicks don't _block_ in-game mouse actions.
			// This lets the game still receive button events (e.g., for camera orbit) while the UI also receives them.
			btn.ButtonInput = PanelInputType.Game;

			var lbl = btn.Add.Label( name );
			lbl.Style.FontColor = Color.White;
			lbl.Style.FontSize = 13;
			lbl.Style.PointerEvents = PointerEvents.None;

			// Capture loop variables for the closure
			var capturedGo = go;
			var capturedBtn = btn;
			btn.AddEventListener( "onclick", () => SelectEntity( capturedGo, capturedBtn ) );
		}
	}

	void SelectEntity( GameObject go, Panel btn )
	{
		// Reset the previously highlighted button
		if ( _activeEntityBtn is not null )
			_activeEntityBtn.Style.BackgroundColor = new Color( 0.18f, 0.18f, 0.28f );

		_selected = go;
		_activeEntityBtn = btn;
		btn.Style.BackgroundColor = new Color( 0.25f, 0.40f, 0.71f );

		_selectedName.Text = go.Name;
		_inspectorContent.Style.Display = DisplayMode.Flex;

		// Immediately sync sliders to the entity's current position
		var pos = go.WorldPosition;
		//_xSlider?.SetValueWithoutCallback( pos.x );
		//_ySlider?.SetValueWithoutCallback( pos.y );
		//_zSlider?.SetValueWithoutCallback( pos.z );
	}

	void UpdateLogConsole()
	{
		if ( _logContainer is null ) return;

		_logContainer.DeleteChildren( false );

		if ( RecentLogs is null || RecentLogs.Count == 0 ) return;

		foreach ( var entry in RecentLogs )
		{
			bool isError = entry.Level == LogLevel.Error;
			var color = isError
				? new Color( 1f, 0.30f, 0.30f )
				: new Color( 1f, 0.87f, 0.22f );

			var prefix = isError ? "[ERR]" : "[WRN]";
			var logger = string.IsNullOrEmpty( entry.Logger ) ? string.Empty : $"[{entry.Logger}] ";

			var lbl = _logContainer.Add.Label( $"{prefix} {logger}{entry.Message}" );
			lbl.Style.FontColor = color;
			lbl.Style.FontSize = 12;
			lbl.Style.MarginBottom = 2;
		}
	}
}
