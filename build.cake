#tool nuget:?package=GitVersion.CommandLine&version=5.0.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.12.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.11.1
#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

// NOTE: These two constants are set here because constants.cake
// isn't loaded until after the arguments are parsed.
//
// Since GitVersion is only used when running under
// Windows, the default version should be updated to the
// next version after each release.
const string DEFAULT_VERSION = "3.9.0";
const string DEFAULT_CONFIGURATION = "Release";

var target = Argument("target", "Default");

// Load additional cake files here since some of them
// depend on the arguments provided.
#load cake/parameters.cake

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
            Source = PACKAGE_SOURCES
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
        NUnit3(parameters.OutputDirectory + UNIT_TEST_ASSEMBLY);
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
        //NUnit3(parameters.OutputDirectory + INTEGRATION_TEST_ASSEMBLY);
        new NuGetPackageTester(parameters).RunPackageTests();
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
            HasFiles("CHANGES.txt", "LICENSE.txt"),
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

        new ChocolateyPackageTester(parameters).RunPackageTests();
//        NUnit3(parameters.OutputDirectory + INTEGRATION_TEST_ASSEMBLY);
    });

//////////////////////////////////////////////////////////////////////
// PACKAGE TESTS
//////////////////////////////////////////////////////////////////////

private void RunPackageTests(BuildParameters parameters, string packageId)
{
    NUnit3(parameters.OutputDirectory + INTEGRATION_TEST_ASSEMBLY);
}

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
    .IsDependentOn("Package");

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
