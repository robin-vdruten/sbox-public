HEADER
{
	Description = "Custom shader for the Source 2 Triangle Demo. Renders vertex-coloured geometry with a smooth brightness pulse.";
	DevShader = true;
	Version = 1;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
MODES
{
	Default();
	Forward();
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
FEATURES
{
	#include "ui/features.hlsl"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
COMMON
{
	#include "ui/common.hlsl"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
VS
{
	#include "ui/vertex.hlsl"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
PS
{
	#include "ui/pixel.hlsl"

	float4 g_vViewport < Source( Viewport ); >;
	float4 g_vInvTextureDim < Source( InvTextureDim ); SourceArg( g_tColor ); >;
	Texture2D g_tColor < Attribute( "Texture" ); SrgbRead( true ); >;

	// Time value supplied each frame so the shader can animate.
	float g_flTime < Source( Time ); >;

	RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );
	RenderState( CullMode, NONE );

	// No depth
	RenderState( DepthWriteEnable, false );

	#define SUBPIXEL_AA_MAGIC 0.5

	PS_OUTPUT MainPs( PS_INPUT i )
	{
		PS_OUTPUT o;
		UI_CommonProcessing_Pre( i );

		float4 vImage = g_tColor.Sample( g_sAniso, i.vTexCoord.xy );

		// Apply a gentle sine-wave brightness pulse to the vertex colour so the
		// triangle visually stands out as using a custom shader.
		float pulse = 0.75f + 0.25f * sin( g_flTime * 3.0f );
		float4 vTinted = vImage * i.vColor.rgba;
		vTinted.rgb *= pulse;

		o.vColor = vTinted;
		return UI_CommonProcessing_Post( i, o );
	}
}
