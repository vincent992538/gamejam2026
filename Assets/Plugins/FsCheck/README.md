# FsCheck for Unity

Property-based testing library for the Horse Betting Simulator project.

## Included DLLs

- **FsCheck.dll** - v2.16.6 (netstandard2.0)
- **FsCheck.NUnit.dll** - v2.16.6 (netstandard2.0)
- **FSharp.Core.dll** - v6.0.7 (netstandard2.1)

## Configuration

These DLLs are configured as Editor-only plugins via their `.meta` files.
They are referenced by the `HorseBetting.Tests.EditMode` assembly definition
as precompiled references.

## Usage

FsCheck property tests run via NUnit in Unity's Test Framework (Edit Mode tests).

Example:
```csharp
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

[TestFixture]
public class MyPropertyTests
{
    [FsCheck.NUnit.Property]
    public void MyProperty(int x)
    {
        Assert.That(x + 0, Is.EqualTo(x));
    }
}
```

## Source

Downloaded from NuGet:
- https://www.nuget.org/packages/FsCheck/2.16.6
- https://www.nuget.org/packages/FsCheck.NUnit/2.16.6
- https://www.nuget.org/packages/FSharp.Core/6.0.7
