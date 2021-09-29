#tool nuget:?package=GitVersion.CommandLine&version=5.0.0
#tool nuget:?package=GitReleaseManager&version=0.11.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.12.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.11.1
#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0

////////////////////////////////////////////////////////////////////
// PROJECT-SPECIFIC CONSTANTS
//////////////////////////////////////////////////////////////////////

const string SOLUTION_FILE = "nunit.v2.driver.sln";
const string NUGET_ID = "NUnit.Extension.NUnitV2Driver";
const string CHOCO_ID = "nunit-extension-nunit-v2-driver";
const string GITHUB_OWNER = "nunit";
const string GITHUB_REPO = "nunit-v2-framework-driver";
const string DEFAULT_VERSION = "3.9.0";
const string DEFAULT_CONFIGURATION = "Release";

// Load scripts after defining constants
#load cake/parameters.cake

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

// Additional arguments defined in the cake scripts:
//   --configuration
//   --version

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN
//////////////////////////////////////////////////////////////////////

Setup<BuildParameters>((context) =>
{
    var parameters = BuildParameters.Create(context);

    if (BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(parameters.PackageVersion + "-" + AppVeyor.Environment.Build.Number);

    Information("Building {0} version {1} of NUnit Project Loader.", parameters.Configuration, parameters.PackageVersion);

    return parameters;
});

//////////////////////////////////////////////////////////////////////
// DUMP SETTINGS
//////////////////////////////////////////////////////////////////////

Task("DumpSettings")
    .Does<BuildParameters>((parameters) =>
    {
        parameters.DumpSettings();
    });

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does<BuildParameters>((parameters) =>
    {
        Information("Cleaning " + parameters.OutputDirectory);
        CleanDirectory(parameters.OutputDirectory);
    });

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("NuGetRestore")
    .Does(() =>
    {
        NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings()
        {
            Source = new string[]
            {
                "https://www.nuget.org/api/v2",
                "https://www.myget.org/F/nunit/api/v2"
            }
        });
    });

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("NuGetRestore")
    .Does<BuildParameters>((parameters) =>
    {
        if(IsRunningOnWindows())
        {
            MSBuild(SOLUTION_FILE, new MSBuildSettings()
                .SetConfiguration(parameters.Configuration)
                .SetMSBuildPlatform(MSBuildPlatform.Automatic)
                .SetVerbosity(Verbosity.Minimal)
                .SetNodeReuse(false)
                .SetPlatformTarget(PlatformTarget.MSIL)
            );
        }
        else
        {
            XBuild(SOLUTION_FILE, new XBuildSettings()
                .WithTarget("Build")
                .WithProperty("Configuration", parameters.Configuration)
                .SetVerbosity(Verbosity.Minimal)
            );
        }
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("Test")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
    {
        NUnit3(parameters.OutputDirectory + "nunit.v2.driver.tests.dll");
    });

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("BuildNuGetPackage")
    .Does<BuildParameters>((parameters) => 
    {
        CreateDirectory(parameters.PackageDirectory);

        BuildNuGetPackage(parameters);
    });

Task("InstallNuGetPackage")
    .Does<BuildParameters>((parameters) =>
    {
        // Ensure we aren't inadvertently using the chocolatey install
        if (DirectoryExists(parameters.ChocolateyInstallDirectory))
            DeleteDirectory(parameters.ChocolateyInstallDirectory, new DeleteDirectorySettings() { Recursive = true });

        CleanDirectory(parameters.NuGetInstallDirectory);
        Unzip(parameters.NuGetPackage, parameters.NuGetInstallDirectory);

        Information($"Unzipped {parameters.NuGetPackageName} to { parameters.NuGetInstallDirectory}");
    });

Task("VerifyNuGetPackage")
    .IsDependentOn("InstallNuGetPackage")
    .Does<BuildParameters>((parameters) =>
    {
        Check.That(parameters.NuGetInstallDirectory,
            HasFiles("CHANGES.txt", "LICENSE.txt"),
            HasDirectory("tools")
                .WithFiles("nunit.v2.driver.dll", "nunit.core.dll", "nunit.core.interfaces.dll", "nunit.v2.driver.addins"));
        Information("Verification was successful!");
    });

Task("TestNuGetPackage")
    .IsDependentOn("InstallNuGetPackage")
    .Does<BuildParameters>((parameters) =>
    {
        new NuGetPackageTester(parameters).RunPackageTests(PackageTests);
    });

Task("BuildChocolateyPackage")
    .Does<BuildParameters>((parameters) =>
    {
        CreateDirectory(parameters.PackageDirectory);

        BuildChocolateyPackage(parameters);
    });

Task("InstallChocolateyPackage")
    .Does<BuildParameters>((parameters) =>
    {
        // Ensure we aren't inadvertently using the nuget install
        if (DirectoryExists(parameters.NuGetInstallDirectory))
            DeleteDirectory(parameters.NuGetInstallDirectory, new DeleteDirectorySettings() { Recursive = true });

        CleanDirectory(parameters.ChocolateyInstallDirectory);
        Unzip(parameters.ChocolateyPackage, parameters.ChocolateyInstallDirectory);

        Information($"Unzipped {parameters.ChocolateyPackageName} to { parameters.ChocolateyInstallDirectory}");
    });

Task("VerifyChocolateyPackage")
    .IsDependentOn("InstallChocolateyPackage")
    .Does<BuildParameters>((parameters) =>
    {
        Check.That(parameters.ChocolateyInstallDirectory,
            HasDirectory("tools").WithFiles(
                "CHANGES.txt", "LICENSE.txt", "VERIFICATION.txt",
                "nunit.v2.driver.dll", "nunit.core.dll",
                "nunit.core.interfaces.dll", "nunit.v2.driver.addins"));
        Information("Verification was successful!");
    });

Task("TestChocolateyPackage")
    .IsDependentOn("InstallChocolateyPackage")
    .Does<BuildParameters>((parameters) =>
    {
        //// We are using nuget packages for the runner, so add an extra
        //// addins file to allow detecting chocolatey packages
        //string runnerDir = parameters.ToolsDirectory + "NUnit.ConsoleRunner.3.11.1/tools";
        //using (var writer = new StreamWriter(runnerDir + "/choco.engine.addins"))
        //    writer.WriteLine("../../nunit-extension-*/tools/");

        new ChocolateyPackageTester(parameters).RunPackageTests(PackageTests);
    });

PackageTest[] PackageTests = new PackageTest[]
{
    new PackageTest()
    {
        Description = "Integration Tests using NUnit V2",
        Arguments = "bin/Release/v2-tests/v2-test-assembly.dll",
        TestConsoleVersions = new string[] { "3.12.0", "3.11.1", "3.10.0" },
        ExpectedResult = new ExpectedResult("Passed")
        {
            Total = 76,
            Passed = 76,
            Failed = 0,
            Warnings = 0,
            Inconclusive = 0,
            Skipped = 0,
            Assemblies = new[] { new ExpectedAssemblyResult("v2-test-assembly.dll", "net-2.0") }
        }
    }
};

//////////////////////////////////////////////////////////////////////
// PUBLISH
//////////////////////////////////////////////////////////////////////

static bool hadPublishingErrors = false;

Task("PublishPackages")
    .Description("Publish nuget and chocolatey packages according to the current settings")
    .IsDependentOn("PublishToMyGet")
    .IsDependentOn("PublishToNuGet")
    .IsDependentOn("PublishToChocolatey")
    .Does(() =>
    {
        if (hadPublishingErrors)
            throw new Exception("One of the publishing steps failed.");
    });

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToMyGet")
    .Description("Publish packages to MyGet")
    .Does<BuildParameters>((parameters) =>
    {
        if (!parameters.ShouldPublishToMyGet)
            Information("Nothing to publish to MyGet from this run.");
        else
            try
            {
                PushNuGetPackage(parameters.NuGetPackage, parameters.MyGetApiKey, parameters.MyGetPushUrl);
                PushChocolateyPackage(parameters.ChocolateyPackage, parameters.MyGetApiKey, parameters.MyGetPushUrl);
            }
            catch (Exception)
            {
                hadPublishingErrors = true;
            }
    });

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToNuGet")
    .Description("Publish packages to NuGet")
    .Does<BuildParameters>((parameters) =>
    {
        if (!parameters.ShouldPublishToNuGet)
            Information("Nothing to publish to NuGet from this run.");
        else
            try
            {
                PushNuGetPackage(parameters.NuGetPackage, parameters.NuGetApiKey, parameters.NuGetPushUrl);
            }
            catch (Exception)
            {
                hadPublishingErrors = true;
            }
    });

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToChocolatey")
    .Description("Publish packages to Chocolatey")
    .Does<BuildParameters>((parameters) =>
    {
        if (!parameters.ShouldPublishToChocolatey)
            Information("Nothing to publish to Chocolatey from this run.");
        else
            try
            {
                PushChocolateyPackage(parameters.ChocolateyPackage, parameters.ChocolateyApiKey, parameters.ChocolateyPushUrl);
            }
            catch (Exception)
            {
                hadPublishingErrors = true;
            }
    });

//////////////////////////////////////////////////////////////////////
// CREATE A DRAFT RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateDraftRelease")
    .Does<BuildParameters>((parameters) =>
    {
        if (parameters.IsReleaseBranch)
        {
            // NOTE: Since this is a release branch, the pre-release label
            // is "pre", which we don't want to use for the draft release.
            // The branch name contains the full information to be used
            // for both the name of the draft release and the milestone,
            // i.e. release-2.0.0, release-2.0.0-beta2, etc.
            string milestone = parameters.BranchName.Substring(8);
            string releaseName = $"NUnit Vw Framework Driver Extension {milestone}";

            Information($"Creating draft release...");

            try
            {
                GitReleaseManagerCreate(parameters.GitHubAccessToken, GITHUB_OWNER, GITHUB_REPO, new GitReleaseManagerCreateSettings()
                {
                    Name = releaseName,
                    Milestone = milestone
                });
            }
            catch
            {
                Error($"Unable to create draft release for {releaseName}.");
                Error($"Check that there is a {milestone} milestone with at least one closed issue.");
                Error("");
                throw;
            }
        }
        else
        {
            Information("Skipping Release creation because this is not a release branch");
        }
    });

//////////////////////////////////////////////////////////////////////
// CREATE A PRODUCTION RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateProductionRelease")
    .Does<BuildParameters>((parameters) =>
    {
        if (parameters.IsProductionRelease)
        {
            string token = parameters.GitHubAccessToken;
            string tagName = parameters.PackageVersion;
            string assets = $"\"{parameters.NuGetPackage},{parameters.ChocolateyPackage}\"";

            Information($"Publishing release {tagName} to GitHub");

            GitReleaseManagerAddAssets(token, GITHUB_OWNER, GITHUB_REPO, tagName, assets);
            GitReleaseManagerClose(token, GITHUB_OWNER, GITHUB_REPO, tagName);
        }
        else
        {
            Information("Skipping CreateProductionRelease because this is not a production release");
        }
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("PackageNuGet")
    .IsDependentOn("PackageChocolatey");

Task("PackageNuGet")
    .IsDependentOn("BuildNuGetPackage")
    .IsDependentOn("VerifyNuGetPackage")
    .IsDependentOn("TestNuGetPackage");

Task("PackageChocolatey")
    .IsDependentOn("BuildChocolateyPackage")
    .IsDependentOn("VerifyChocolateyPackage")
    .IsDependentOn("TestChocolateyPackage");

Task("Appveyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("PublishPackages")
    .IsDependentOn("CreateDraftRelease")
    .IsDependentOn("CreateProductionRelease");

Task("Travis")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Full")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
