// This file contains both constants and static readonly values, which
// are used as constants. The latter must not depend in any way on the
// contents of other cake files, which are loaded after this one.

// Files
const string SOLUTION_FILE = "nunit.v2.driver.sln";
const string UNIT_TEST_ASSEMBLY = "nunit.v2.driver.tests.dll";
const string INTEGRATION_TEST_ASSEMBLY = "v2-tests/v2-test-assembly.dll";

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var OUTPUT_DIR = PROJECT_DIR + "output/";
var TOOLS_DIR = PROJECT_DIR + "tools/";

// Console Runner
var CONSOLE_EXE = TOOLS_DIR + "NUnit.ConsoleRunner/tools/nunit3-console.exe";

// Packaging
const string NUGET_ID = "NUnit.Extension.NUnitV2Driver";
const string CHOCO_ID = "nunit-extension-nunit-v2-driver";
//const string GITHUB_SITE = "https://github.com/nunit/nunit-v2-framework-driver";
//const string WIKI_PAGE = "https://github.com/nunit/docs/wiki/Console-Command-Line";
var NUGET_PACKAGE_NAME = NUGET_ID + "." + DEFAULT_VERSION + ".nupkg";
var CHOCO_PACKAGE_NAME = CHOCO_ID + "." + DEFAULT_VERSION + ".nupkg";
var NUGET_PACKAGE = OUTPUT_DIR + NUGET_PACKAGE_NAME;
var CHOCO_PACKAGE = OUTPUT_DIR + CHOCO_PACKAGE_NAME;

// Package sources for nuget restore
static readonly string[] PACKAGE_SOURCES = new string[]
{
	"https://www.nuget.org/api/v2",
	"https://www.myget.org/F/nunit/api/v2"
};

// We don't support running tests built with .net core yet
// var TEST_TARGET_FRAMEWORKS = TARGET_FRAMEWORKS
var TEST_TARGET_FRAMEWORKS = new[] { "net20" };

// URLs for uploading packages
private const string MYGET_PUSH_URL = "https://www.myget.org/F/nunit/api/v2";
private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

// Environment Variable names holding API keys
private const string MYGET_API_KEY = "MYGET_API_KEY";
private const string NUGET_API_KEY = "NUGET_API_KEY";
private const string CHOCO_API_KEY = "CHOCO_API_KEY";

// Environment Variable names holding GitHub identity of user
private const string GITHUB_OWNER = "NUnit";
private const string GITHUB_REPO = "nunit-project-loader";
// Access token is used by GitReleaseManager
private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

// Pre-release labels that we publish
private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev", "pre" };
private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_RELEASE_ON_GITHUB = { "alpha", "beta", "rc" };
