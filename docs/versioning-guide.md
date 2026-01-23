# Khaos.Flow – Versioning Guide

This document describes how versions are managed for the Flow package.

## Version Strategy

This package uses **MinVer** for automatic semantic versioning based on Git tags.

### Configuration

In `Directory.Build.props`:

```xml
<MinVerTagPrefix>Khaos.Flow/v</MinVerTagPrefix>
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
```

### Tag Format

Tags follow the pattern: `Khaos.Flow/vX.Y.Z`

Examples:
- `Khaos.Flow/v1.0.0` → Version 1.0.0
- `Khaos.Flow/v1.1.0` → Version 1.1.0

## Semantic Versioning

### Major Version (X.0.0)
- Breaking changes to public API
- Removing public types or members
- Changing behavior in incompatible ways

### Minor Version (0.X.0)
- New features (new step types, adapters)
- New builder methods
- Performance improvements

### Patch Version (0.0.X)
- Bug fixes
- Documentation updates
- Internal refactoring

## Release Workflow

### 1. Check Current Version

```powershell
cd Khaos.Flow
.\scripts\Get-Version.ps1
```

### 2. Ensure Tests Pass

```powershell
.\scripts\Test.ps1
```

### 3. Create Release Tag

```powershell
git tag Khaos.Flow/v1.0.0
git push origin Khaos.Flow/v1.0.0
```

### 4. Build and Pack

```powershell
.\scripts\Build.ps1
.\scripts\Pack.ps1
```

### 5. Publish

```powershell
dotnet nuget push artifacts/*.nupkg --source nuget.org --api-key YOUR_KEY
```

## Dependency Coordination

This package depends on:
- `KhaosCode.Flow.Abstractions`
- `KhaosCode.Pipeline.Abstractions`

When updating:
1. Update abstractions first if needed.
2. Update this package's dependency references.
3. Tag and release this package.

## Pre-release Versions

Between tags, MinVer generates:
- After `v1.0.0`: `1.0.1-alpha.0.{commits}`

## Guidelines

1. **Never manually set Version** in project files
2. **Keep abstraction versions aligned** where possible
3. **Test with consumers** before major releases
4. **Document breaking changes** in release notes
