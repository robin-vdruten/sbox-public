using System;
using Sandbox.UI;
using Sandbox.UI.Construct;


namespace Sandbox;

/// <summary>
/// A single-axis float slider panel, built imperatively from <see cref="Panel"/>.
/// Drag the track to change the value; the thumb and fill update in real time.
///
/// In a sbox game-addon (compiled by the sbox Razor toolchain) you would write
/// the same thing as a .razor file, e.g.:
///
///   @inherits Panel
///   &lt;root class="axis-slider"&gt;
///     &lt;label class="axis-name" style="color:@ColorHex"&gt;@AxisName&lt;/label&gt;
///     &lt;div class="track" @onmousedown=@OnTrackDown @onmousemove=@OnTrackMove&gt;
///       &lt;div class="fill"  style="width:@(Normalized*100)%"&gt;&lt;/div&gt;
///       &lt;div class="thumb" style="left:@(Normalized*100)%"&gt;&lt;/div&gt;
///     &lt;/div&gt;
///     &lt;label class="value-readout"&gt;@Value.ToString("F1")&lt;/label&gt;
///   &lt;/root&gt;
/// </summary>
internal sealed class AxisSlider : Panel
{
	public Action<float> OnValueChanged { get; set; }

	public float Min { get; private set; }
	public float Max { get; private set; }

	float _value;
	bool _dragging;

	Panel _track;
	Panel _fill;
	Panel _thumb;
	Label _nameLabel;
	Label _valueLabel;

	/// <summary>
	/// Configure the slider after it has been added as a child panel.
	/// </summary>
	public void Setup( string axisName, Color color, float min, float max )
	{
		Min = min;
		Max = max;
		_value = MathX.LerpTo( min, max, 0.5f );

		Style.FlexDirection = FlexDirection.Row;
		Style.AlignItems = Align.Center;
		Style.Height = 24;
		Style.MarginBottom = 4;

		// Axis name badge
		_nameLabel = Add.Label( axisName );
		_nameLabel.Style.Width = 16;
		_nameLabel.Style.FontColor = color;
		_nameLabel.Style.FontSize = 13;
		_nameLabel.Style.FontWeight = 700;
		_nameLabel.Style.MarginRight = 6;

		// Track background
		_track = AddChild<Panel>();
		_track.Style.FlexGrow = 1;
		_track.Style.Height = 8;
		_track.Style.BackgroundColor = new Color( 0.12f, 0.12f, 0.18f );
		_track.Style.BorderTopRightRadius = 4;
		_track.Style.Position = PositionMode.Relative;
		_track.Style.Overflow = OverflowMode.Visible;
		_track.Style.Cursor = "pointer";

		// Coloured fill
		_fill = _track.AddChild<Panel>();
		_fill.Style.Position = PositionMode.Absolute;
		_fill.Style.Left = 0;
		_fill.Style.Top = 0;
		_fill.Style.Bottom = 0;
		_fill.Style.BackgroundColor = color.WithAlpha( 0.75f );
		_fill.Style.BorderTopRightRadius = 4;
		_fill.Style.PointerEvents = PointerEvents.None;

		// Thumb
		_thumb = _track.AddChild<Panel>();
		_thumb.Style.Position = PositionMode.Absolute;
		_thumb.Style.Width = 10;
		_thumb.Style.Height = 18;
		_thumb.Style.BackgroundColor = Color.White;
		_thumb.Style.BorderTopRightRadius = 3;
		_thumb.Style.Top = -5;
		_thumb.Style.MarginLeft = -5;
		_thumb.Style.ZIndex = 1;
		_thumb.Style.PointerEvents = PointerEvents.None;

		// Numeric readout
		_valueLabel = Add.Label( "0" );
		_valueLabel.Style.Width = 52;
		_valueLabel.Style.FontColor = Color.White;
		_valueLabel.Style.FontSize = 11;
		_valueLabel.Style.TextAlign = TextAlign.Right;
		_valueLabel.Style.MarginLeft = 6;
		_valueLabel.Style.PointerEvents = PointerEvents.None;

		RefreshVisuals();


	}

	/// <summary>
	/// Update the displayed value without firing <see cref="OnValueChanged"/>.
	/// Use this to keep the slider in sync with the scene without creating a feedback loop.
	/// </summary>
	public void SetValueWithoutCallback( float value )
	{
		_value = value.Clamp( Min, Max );
		RefreshVisuals();
	}

	// ── Internal ──────────────────────────────────────────────────────────────

	float Normalized => MathX.LerpInverse( _value, Min, Max, true );

	void RefreshVisuals()
	{
		if ( _fill == null ) return;

		float pct = Normalized * 100f;
		_fill.Style.Width = Length.Percent( pct );
		_thumb.Style.Left = Length.Percent( pct );
		_valueLabel.Text = _value.ToString( "F1" );
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		_dragging = true;
		ApplyMousePosition();
		e.StopPropagation();
	}

	protected override void OnMouseMove( MousePanelEvent e )
	{
		if ( !_dragging ) return;
		ApplyMousePosition();
		e.StopPropagation();
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		_dragging = false;
		e.StopPropagation();
	}

	void ApplyMousePosition()
	{
		if ( _track?.Box == null ) return;

		float trackLeft = _track.Box.Left;
		float trackRight = _track.Box.Right;
		float trackWidth = trackRight - trackLeft;

		if ( trackWidth <= 0f ) return;

		float normalized = ( Mouse.Position.x - trackLeft ) / trackWidth;
		normalized = normalized.Clamp( 0f, 1f );

		_value = MathX.LerpTo( Min, Max, normalized );
		RefreshVisuals();
		OnValueChanged?.Invoke( _value );
	}
}
