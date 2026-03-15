using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Sandbox.Generator;

/// <summary>
/// Roslyn incremental source generator that compiles <c>.razor</c> files into C#
/// at <c>dotnet build</c> time, bridging the gap between the sbox runtime Razor
/// pipeline and plain .NET SDK projects.
///
/// <para>
/// Usage in a <c>.csproj</c>:
/// <code>
/// &lt;ItemGroup&gt;
///   &lt;!-- Reference this project as a Roslyn analyzer --&gt;
///   &lt;ProjectReference Include="..\RazorGen\RazorGen.csproj"
///                     OutputItemType="Analyzer"
///                     ReferenceOutputAssembly="false" /&gt;
///   &lt;!-- Declare each .razor file as additional input to the generator --&gt;
///   &lt;AdditionalFiles Include="UI\*.razor" /&gt;
/// &lt;/ItemGroup&gt;
/// </code>
/// </para>
///
/// <para>
/// The generator calls <see cref="Sandbox.Razor.RazorProcessor.GenerateFromSource"/>
/// (from the <c>Sandbox.Razor</c> project) for every <c>.razor</c> additional file.
/// Because Roslyn loads analyzers in an isolated context, the static constructor
/// registers an <see cref="AppDomain.AssemblyResolve"/> handler that probes the
/// same directory as this DLL so that <c>Sandbox.Razor.dll</c> can be resolved.
/// </para>
/// </summary>
[Generator]
public sealed class RazorSourceGenerator : IIncrementalGenerator
{
	// -------------------------------------------------------------------------
	// Dependency resolution
	// -------------------------------------------------------------------------

	/// <summary>
	/// Register a fallback assembly resolver so that <c>Sandbox.Razor.dll</c>
	/// (which is copied alongside this DLL by MSBuild) can be found when Roslyn
	/// loads this generator in its isolated analyzer context.
	/// </summary>
	static RazorSourceGenerator()
	{
		AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
	}

	static Assembly ResolveAssembly( object sender, ResolveEventArgs args )
	{
		var asmName = new AssemblyName( args.Name ).Name;
		var dir = Path.GetDirectoryName( typeof( RazorSourceGenerator ).Assembly.Location );
		if ( dir is null ) return null;

		var candidate = Path.Combine( dir, asmName + ".dll" );
		return File.Exists( candidate ) ? Assembly.LoadFrom( candidate ) : null;
	}

	// -------------------------------------------------------------------------
	// IIncrementalGenerator
	// -------------------------------------------------------------------------

	/// <inheritdoc/>
	public void Initialize( IncrementalGeneratorInitializationContext context )
	{
		// Select only *.razor additional files (ignore *.razor.scss etc.)
		var razorFiles = context.AdditionalTextsProvider
			.Where( static t =>
				t.Path.EndsWith( ".razor", StringComparison.OrdinalIgnoreCase ) );

		context.RegisterSourceOutput( razorFiles, GenerateSource );
	}

	// -------------------------------------------------------------------------
	// Per-file generation
	// -------------------------------------------------------------------------

	static void GenerateSource( SourceProductionContext ctx, AdditionalText file )
	{
		var text = file.GetText( ctx.CancellationToken )?.ToString();
		if ( string.IsNullOrEmpty( text ) )
			return;

		try
		{
			// Delegate to the engine's own Razor-to-C# compiler.
			var generated = Sandbox.Razor.RazorProcessor.GenerateFromSource(
				text, file.Path, rootNamespace: "Sandbox", useFolderNamespacing: true );

			// Make a stable, collision-free hint name from the file path.
			var filename = Path.GetFileNameWithoutExtension( file.Path );
			uint hash = (uint)file.Path.GetHashCode();
			ctx.AddSource( $"_razorgen_{filename}_{hash:x8}.cs",
				SourceText.From( generated, Encoding.UTF8 ) );
		}
		catch ( Exception ex )
		{
			var descriptor = new DiagnosticDescriptor(
				id: "RAZORGEN001",
				title: "Razor generator error",
				messageFormat: "Error processing '{0}': {1}",
				category: "RazorGen",
				defaultSeverity: DiagnosticSeverity.Error,
				isEnabledByDefault: true );

			ctx.ReportDiagnostic( Diagnostic.Create(
				descriptor, location: null,
				Path.GetFileName( file.Path ), ex.Message ) );
		}
	}
}
