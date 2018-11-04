//////////////////////////////////////////////////////////////////////
// ARGUMENT DEFAULTS
//////////////////////////////////////////////////////////////////////

var DefaultTarget = Argument("target", "Default");
var Configuration = Argument("configuration", "Debug");
var OutputPath = Argument("outputPath", ".artifacts");

// Unit Tests
var CSharpCoverageThreshold = Argument("coveragePercentThreshold", 5);
var CSharpCoverageExcludePatterns = new List<string>();

//////////////////////////////////////////////////////////////////////
// Helpers
//////////////////////////////////////////////////////////////////////

public void ForEachProject(string globPattern, Action<DirectoryPath, FilePath> projectAction)
{
    var projectFiles = GetFiles($"{globPattern}/*.csproj");
    foreach (var projectFile in projectFiles)
    {
        var projectDir = projectFile.GetDirectory();
        Information($"Using {projectDir}, {projectFile}...");
        projectAction(projectDir, projectFile);
    }
}

//////////////////////////////////////////////////////////////////////
// Tasks
//////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Information($"Branch: {EnvironmentVariable("TRAVIS_BRANCH")}");
    Information($"Tag: {EnvironmentVariable("TRAVIS_TAG")}");
    Information($"Build configuration: {Configuration}");

        CSharpCoverageThreshold = 0;
    // CSharpCoverageExcludePatterns.Add("**/*.Designer.cs");
});

Task("EnsureOutputPathExists")
    .Does(() => EnsureDirectoryExists(OutputPath));

Task("Restore")
    .Does(() =>
{
    DotNetCoreRestore();
});

Task("Build")
    .Does(() =>
{
    DotNetCoreBuild(".", new DotNetCoreBuildSettings { Configuration = Configuration });
});

Task("DotNetTestWithCodeCoverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    RunMiniCover(
        Configuration,
        CSharpCoverageThreshold,
        CSharpCoverageExcludePatterns.ToArray()
    );
});

public void RunMiniCover(
    string configuration,
    int coverageThreshold = 0,
    string[] extraSourceDirs = null,
    string[] excludePatterns = null,
    string[] excludeCategories = null
)
{
    var excludeParams = string.Join(" ", (excludePatterns ?? new string[0]).Select(pattern => $"--exclude-sources {pattern}"));
    var extraSourcesParams = string.Join(" ", (extraSourceDirs ?? new string[0]).Select(pattern => $"--sources {pattern}"));
    DotNetCoreTool(
        "./tools/tools.csproj",
        "minicover",
        $"instrument --workdir ../ --assemblies test/**/bin/**/*.dll --sources src/**/*.cs {extraSourcesParams} {excludeParams}"
    );
    DotNetCoreTool("./tools/tools.csproj", "minicover", "reset");

    var argumentCustomization = string.Join(" ", (excludeCategories ?? new string[0]).Select(category => $"--filter Category!={category}"));
    var testSettings = new DotNetCoreTestSettings
    {
        ArgumentCustomization = args => args.Append(argumentCustomization),
        Configuration = configuration,
        NoBuild = true,
    };
    ForEachProject("./test/**", (projectDir, projectFile) => {
        DotNetCoreTest(projectFile.FullPath, testSettings);
    });

    DotNetCoreTool("./tools/tools.csproj", "minicover", "uninstrument --workdir  ../");
    DotNetCoreTool("./tools/tools.csproj", "minicover", $"htmlreport --output {OutputPath} --workdir ../ --threshold {coverageThreshold}");
    DotNetCoreTool("./tools/tools.csproj", "minicover", $"report --workdir ../ --threshold {coverageThreshold}");
}

Task("Pack")
    .Does(() => ForEachProject("./src/*", (projectDir, projectFile) => {
        var settings = new DotNetCorePackSettings
        {
            Configuration = Configuration,
            OutputDirectory = OutputPath
        };

        var isReleaseBuild = Configuration == "Release";
        if (isReleaseBuild)
        {
            Information($"Release Build");
        }
        else
        {
            var buildNumber = $"t{DateTime.UtcNow.ToString("yyMMddHHmmss")}";

            settings.VersionSuffix = buildNumber;
            Information($"Prerelease Build Number: {buildNumber}");
        }

        DotNetCorePack(projectDir.FullPath, settings);
    })
);

Task("Push")
    .Does(() =>
{
    DotNetCoreNuGetPush(
        OutputPath + "/*.nupkg",
        new DotNetCoreNuGetPushSettings
        {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NUGET_API_KEY")
        }
    );
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("DotNetTestWithCodeCoverage");

Task("Publish")
    .IsDependentOn("Pack")
    .IsDependentOn("Push");

RunTarget(DefaultTarget);
