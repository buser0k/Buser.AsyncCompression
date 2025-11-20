#tool "nuget:?package=ReportGenerator&version=5.1.10"
#tool "nuget:?package=coverlet.console&version=3.2.0"

#addin "nuget:?package=Cake.FileHelpers&version=6.0.0"

using System.Xml.Linq;
using System.Linq;

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var solutionPath = "./Buser.AsyncCompression.sln";
var projectPath = "./Buser.AsyncCompression/Buser.AsyncCompression.csproj";
var testProjectPath = "./Buser.AsyncCompression.Tests/Buser.AsyncCompression.Tests.csproj";
var outputDir = "./artifacts";
var testResultsDir = "./test-results";
var coverageDir = "./coverage";
var versionBump = Argument("versionBump", "patch"); // major, minor, or patch

// Ensure we're using .NET 8
var dotnetVersion = "8.0.414";

// Read current version from .csproj
var projectXml = XDocument.Load(projectPath);
var ns = projectXml.Root?.GetDefaultNamespace() ?? XNamespace.None;
var versionElement = projectXml.Descendants(ns + "Version").FirstOrDefault() 
    ?? projectXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "Version");
var currentVersion = versionElement?.Value ?? "1.0.0";
Information("Current version: {0}", currentVersion);

// Increment version
Task("IncrementVersion")
    .Does(() =>
    {
        Information("Incrementing version ({0})...", versionBump);
        
        var version = System.Version.Parse(currentVersion);
        System.Version newVersion;
        
        switch (versionBump.ToLower())
        {
            case "major":
                newVersion = new System.Version(version.Major + 1, 0, 0);
                break;
            case "minor":
                newVersion = new System.Version(version.Major, version.Minor + 1, 0);
                break;
            case "patch":
            default:
                newVersion = new System.Version(version.Major, version.Minor, version.Build + 1);
                break;
        }
        
        var newVersionString = newVersion.ToString();
        Information("New version: {0}", newVersionString);
        
        // Update version in .csproj
        if (versionElement != null)
        {
            versionElement.Value = newVersionString;
            
            // Also update AssemblyVersion and FileVersion if they exist
            var assemblyVersionElement = projectXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "AssemblyVersion");
            if (assemblyVersionElement != null)
            {
                assemblyVersionElement.Value = newVersionString;
            }
            
            var fileVersionElement = projectXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "FileVersion");
            if (fileVersionElement != null)
            {
                fileVersionElement.Value = newVersionString;
            }
            
            projectXml.Save(projectPath);
            Information("Version updated in {0} to {1}", projectPath, newVersionString);
        }
        else
        {
            throw new Exception("Version element not found in project file");
        }
    });

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
        
        // Generate coverage report (only on Windows to avoid cross-platform issues)
        if (IsRunningOnWindows())
        {
            var coverageFiles = GetFiles($"{testResultsDir}/**/coverage.cobertura.xml");
            if (coverageFiles.Any())
            {
                ReportGenerator(coverageFiles, coverageDir, new ReportGeneratorSettings
                {
                    ReportTypes = new[] { ReportGeneratorReportType.Html, ReportGeneratorReportType.Cobertura }
                });
            }
        }
        else
        {
            Information("Skipping ReportGenerator on non-Windows platform to avoid cross-platform compatibility issues.");
        }
        
        // Copy test results (skip if source and destination are the same)
        if (!DirectoryExists(testResultsDir))
        {
            CreateDirectory(testResultsDir);
        }
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
            SelfContained = true,
            PublishSingleFile = true,
            PublishTrimmed = true
        };

        DotNetPublish(projectPath, publishSettings);
        
        // Create zip package
        Zip($"{outputDir}/publish", $"{outputDir}/Buser.AsyncCompression-{configuration}.zip");
    });

// Publish to NuGet (if API key is provided)
Task("Publish")
    .IsDependentOn("IncrementVersion")
    .IsDependentOn("Package")
    .WithCriteria(() => HasArgument("nugetApiKey"))
    .Does(() =>
    {
        Information("Publishing to NuGet...");
        
        var nugetApiKey = Argument<string>("nugetApiKey");
        
        // Re-read version after increment
        var updatedProjectXml = XDocument.Load(projectPath);
        var updatedNs = updatedProjectXml.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var updatedVersionElement = updatedProjectXml.Descendants(updatedNs + "Version").FirstOrDefault()
            ?? updatedProjectXml.Descendants().FirstOrDefault(x => x.Name.LocalName == "Version");
        var packageVersion = updatedVersionElement?.Value ?? currentVersion;
        Information("Publishing version: {0}", packageVersion);
        
        // Pack the project
        DotNetPack(projectPath, new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = outputDir,
            NoBuild = false, // Need to rebuild with new version
            NoRestore = true,
            MSBuildSettings = new DotNetMSBuildSettings
            {
                Version = packageVersion
            }
        });
        
        // Push to NuGet
        var packages = GetFiles($"{outputDir}/*.nupkg");
        foreach (var package in packages)
        {
            Information("Pushing package: {0}", package.FullPath);
            DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
            {
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = nugetApiKey,
                SkipDuplicate = true // Skip if version already exists
            });
        }
        
        Information("Package published successfully!");
    });

// Default task
Task("Default")
    .IsDependentOn("TestResults");

// Run specific task
RunTarget(target);
