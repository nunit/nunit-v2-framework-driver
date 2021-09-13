// This file contains both constants and static readonly values, which
// are used as constants. The latter must not depend in any way on the
// contents of other cake files, which are loaded after this one.

// Files
const string SOLUTION_FILE = "nunit.v2.driver.sln";
const string UNIT_TEST_ASSEMBLY = "nunit.v2.driver.tests.dll";

// Packaging
const string NUGET_ID = "NUnit.Extension.NUnitV2Driver";
const string CHOCO_ID = "nunit-extension-nunit-v2-driver";

// Package sources for nuget restore - used in build.cake
static readonly string[] PACKAGE_SOURCES = new string[]
{
	"https://www.nuget.org/api/v2",
	"https://www.myget.org/F/nunit/api/v2"
};

// The following items are only used in parameters.cake

// URLs for uploading packages
private const string MYGET_PUSH_URL = "https://www.myget.org/F/nunit/api/v2";
private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

// Environment Variable names holding API keys
private const string MYGET_API_KEY = "MYGET_API_KEY";
private const string NUGET_API_KEY = "NUGET_API_KEY";
private const string CHOCO_API_KEY = "CHOCO_API_KEY";
private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

// Pre-release labels that we publish
private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev", "pre" };
private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_RELEASE_ON_GITHUB = { "alpha", "beta", "rc" };
