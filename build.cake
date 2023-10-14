#tool NuGet.CommandLine&version=6.0.0

// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0-adev00049
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

var NUGET_ID = "TestCentric.InternalT race";

string Configuration = Argument("configuration", Argument("c", "Release"));

string PackageVersion;
string PackageName;
bool IsProductionRelease;
bool IsDevelopmentRelease;

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "TestCentric Internal Trace",
	solutionFile: "TestCentric.InternalTrace.sln",
	githubRepository: "TestCentric.InternalTrace");

BuildSettings.Packages.Add(new NuGetPackage(
	id: "TestCentric.InternalTrace",
	source: "nuget/TestCentric.InternalTrace.nuspec",
	checks: new PackageCheck[] {
		HasFiles(
			"LICENSE.txt", "README.md", "testcentric.png",
			"lib/net20/TestCentric.InternalTrace.dll",
			"lib/net462/TestCentric.InternalTrace.dll",
			"lib/netstandard2.0/TestCentric.InternalTrace.dll") }));

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.IsDependentOn("BuildTestAndPackage")
	.IsDependentOn("PublishToMyGet")
	.IsDependentOn("CreateProductionRelease");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", Argument("t", "Default")));
