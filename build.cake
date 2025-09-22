#tool "nuget:?package=ReportGenerator&version=5.1.10"
#tool "nuget:?package=coverlet.console&version=3.2.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var solutionPath = "./Booser.AsyncCompression.sln";
var projectPath = "./Booser.AsyncCompression/Booser.AsyncCompression.csproj";
var testProjectPath = "./Booser.AsyncCompression.Tests/Booser.AsyncCompression.Tests.csproj";
var outputDir = "./artifacts";
var testResultsDir = "./test-results";
var coverageDir = "./coverage";

// Ensure we're using .NET 8
var dotnetVersion = "8.0.414";

// Cleanup
Task("Clean")
    .Does(() =>
    {
        Information("Cleaning directories...");
        CleanDirectories(new[] { outputDir, testResultsDir, coverageDir });
        CleanDirectories("./**/bin");
        CleanDirectories("./**/obj");
    });

// Restore NuGet packages
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        Information("Restoring NuGet packages...");
        DotNetRestore(solutionPath);
    });

// Build solution
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        Information("Building solution...");
        DotNetBuild(solutionPath, new DotNetBuildSettings
        {
            Configuration = configuration,
            NoRestore = true,
            Verbosity = DotNetVerbosity.Minimal
        });
    });

// Run tests
Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        Information("Running tests...");
        
        var testSettings = new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
            NoRestore = true,
            Loggers = new[] { "trx;LogFileName=test-results.trx" },
            ResultsDirectory = testResultsDir,
            Collectors = new[] { "XPlat Code Coverage" },
            Settings = "./coverlet.runsettings"
        };

        DotNetTest(testProjectPath, testSettings);
    });

// Generate test results and coverage report
Task("TestResults")
    .IsDependentOn("Test")
    .Does(() =>
    {
        Information("Generating test results and coverage report...");
        
        // Generate coverage report
        var coverageFiles = GetFiles($"{testResultsDir}/**/coverage.cobertura.xml");
        if (coverageFiles.Any())
        {
            ReportGenerator(coverageFiles, coverageDir, new ReportGeneratorSettings
            {
                ReportTypes = new[] { ReportGeneratorReportType.Html, ReportGeneratorReportType.Cobertura }
            });
        }
        
        // Copy test results
        CopyFiles($"{testResultsDir}/**/*", testResultsDir);
    });

// Package application
Task("Package")
    .IsDependentOn("Test")
    .Does(() =>
    {
        Information("Packaging application...");
        
        var publishSettings = new DotNetPublishSettings
        {
            Configuration = configuration,
            OutputDirectory = $"{outputDir}/publish",
            SelfContained = false,
            PublishSingleFile = true,
            PublishTrimmed = true
        };

        DotNetPublish(projectPath, publishSettings);
        
        // Create zip package
        Zip($"{outputDir}/publish", $"{outputDir}/Booser.AsyncCompression-{configuration}.zip");
    });

// Publish to NuGet (if API key is provided)
Task("Publish")
    .IsDependentOn("Package")
    .WithCriteria(() => HasArgument("nugetApiKey"))
    .Does(() =>
    {
        Information("Publishing to NuGet...");
        
        var nugetApiKey = Argument<string>("nugetApiKey");
        
        // Pack the project
        DotNetPack(projectPath, new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = outputDir,
            NoBuild = true,
            NoRestore = true
        });
        
        // Push to NuGet
        var packages = GetFiles($"{outputDir}/*.nupkg");
        foreach (var package in packages)
        {
            NuGetPush(package, new NuGetPushSettings
            {
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = nugetApiKey
            });
        }
    });

// Default task
Task("Default")
    .IsDependentOn("TestResults");

// Run specific task
RunTarget(target);
