// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "TestCentric Internal Trace",
	solutionFile: "TestCentric.InternalTrace.sln",
	githubRepository: "TestCentric.InternalTrace",
	unitTests: "**/*.Tests.exe");

BuildSettings.Packages.Add(new NuGetPackage(
	id: "TestCentric.InternalTrace",
	source: "src/TestCentric.InternalTrace/TestCentric.InternalTrace.csproj",
	checks: new PackageCheck[] {
		HasFiles(
			"LICENSE.txt", "README.md", "testcentric.png",
			"lib/net20/TestCentric.InternalTrace.dll",
			"lib/net462/TestCentric.InternalTrace.dll",
			"lib/netstandard2.0/TestCentric.InternalTrace.dll") },
	symbols: new PackageCheck[] {
		HasFiles(
			"lib/net20/TestCentric.InternalTrace.pdb",
			"lib/net462/TestCentric.InternalTrace.pdb",
			"lib/netstandard2.0/TestCentric.InternalTrace.pdb") }));

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
