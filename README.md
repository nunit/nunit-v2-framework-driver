# NUnit V2 Framework Driver

The NUnit V2 Framework Driver is an engine extension for NUnit 3, allowing it to load and execute tests written against the NUnit V2 test framework. Without the extension, an NUnit 3 installation cannot run V2 tests. The driver supports all versions of NUNit V2, including the latest updates from the [NUnit legaacy project](https://github.com/nunit-legacy/nunitv2).

## Scope

NUnit V2 is a legacy project, which will eventually be discontinued. The goal of this driver is to permit users to __continue to run existing NUnit V2 tests under NUnit 3__ for as long as necessary. In keeping with that goal, we will accept certain issues and PRs and reject others. In particular, we won't use the driver to add new features on top of the V2 framework.

We __will__ do a mix of the following:

1. Fix outstanding bugs in the driver itself.

2. Enhance the driver to meet any new requirements introduced by the engine.

3. Provide occasional new features designed to help users move toward an eventual NUnit 3 conversion.

## LICENSE

The NUnit V2 Framework Driver is licensed under the MIT license. See LICENSE.txt in the root of this distribution.