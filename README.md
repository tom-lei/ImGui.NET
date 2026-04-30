# TomLei.ImGui.NET

> **Unofficial fork** — This is an unofficial fork of [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) intended to track recent [Dear ImGui](https://github.com/ocornut/imgui) releases and expose newer ImGui APIs for .NET. It is not affiliated with or maintained by the original ImGui.NET maintainers.

**NuGet package ID: `TomLei.ImGui.NET`**

[![NuGet](https://img.shields.io/nuget/v/TomLei.ImGui.NET.svg)](https://www.nuget.org/packages/TomLei.ImGui.NET)
[![CI](https://github.com/tom-lei/ImGui.NET/actions/workflows/build.yml/badge.svg)](https://github.com/tom-lei/ImGui.NET/actions/workflows/build.yml)

Install via NuGet:

```
dotnet add package TomLei.ImGui.NET
```

---

## Dear ImGui Version

This fork currently targets **Dear ImGui 1.92.7** (cimgui bindings regenerated from the 1.92.7 definitions).

### What's new in 1.92.x

Dear ImGui 1.92.x includes a significant rework of the font/texture atlas system:

- **New embedded/vector font support** — `ImFontAtlas` now supports pluggable font loaders (`ImFontLoader`) and vector/SVG fonts via `AddFontDefaultVector()`.
- **New texture management** — `ImTextureData`, `ImTextureRef`, `ImTextureRect`, `ImTextureFormat`, `ImTextureStatus` replace the old `GetTexDataAsRGBA32`/`GetTexDataAsAlpha8` API.
- **`ImFontAtlasRect`** replaces `ImFontAtlasCustomRect`.
- **`ImFontBaked`** exposes per-size baked font data.
- **`ImFontFlags`** — new per-font configuration flags (`NoLoadError`, `NoLoadGlyphs`, `LockBakedSizes`).
- **`ImGuiFreeTypeLoaderFlags`** — flags for the built-in FreeType font loader.
- **`ImGuiListClipperFlags`** — new flags for `ImGuiListClipper`.
- New `ImDrawTextFlags` enum for internal draw-text control.
- Various additions to `ImGuiIO`, `ImGuiStyle`, tables, multi-select, and platform APIs.

### Migration notes from 1.91.x

- `ImFontAtlasCustomRect` has been removed; use `ImFontAtlasRect` instead.
- `ImFontAtlas.GetTexDataAsAlpha8` / `GetTexDataAsRGBA32` are gone; use the new `ImTextureData`/`ImTextureRef` workflow.
- `ImGuiFreeTypeBuilderFlags` is now `ImGuiFreeTypeLoaderFlags`.

---

This is a .NET wrapper for the immediate mode GUI library, Dear ImGui (https://github.com/ocornut/imgui). ImGui.NET lets you build graphical interfaces using a simple immediate-mode style. ImGui.NET is a .NET Standard library, and can be used on all major .NET runtimes and operating systems.

Included is a basic sample program that shows how to use the library, and renders the UI using [Veldrid](https://github.com/veldrid/veldrid), a portable graphics library for .NET. By itself, Dear ImGui does not care what technology you use for rendering; it simply outputs textured triangles. Example renderers also exist for MonoGame and OpenTK (OpenGL).

This wrapper is built on top of [cimgui](https://github.com/cimgui/cimgui), which exposes a plain C API for Dear ImGui. CI builds and packs native binaries into the NuGet package for these runtime identifiers:

- `win-x86`
- `win-x64`
- `win-arm64`
- `linux-x64`
- `linux-arm64`
- `osx-x64`
- `osx-arm64`

# Building

ImGui.NET can be built in Visual Studio or on the command line. The .NET Core SDK is needed to build on the command line, and it can be downloaded [here](https://www.microsoft.com/net/core). Visual Studio 2017 is the minimum VS version supported for building.

# Usage

ImGui.NET currently provides a raw wrapper around the ImGui native API, and also provides a very thin safe, managed API for convenience. It is currently very much like using the native library, which is very simple, flexible, and robust. The easiest way to figure out how to use the library is to read the documentation of imgui itself, mostly in the imgui.cpp, and imgui.h files, as well as the exported functions in cimgui.h. Looking at the [sample program code](https://github.com/ImGuiNET/ImGui.NET/tree/master/src) will also give some indication about basic usage.

# Debugging native code

ImGui.NET is a wrapper over native code. By default, this native code is packaged and released in an optimized form, making debugging difficult.

Older versions used the separate [ImGui.NET-nativebuild](https://github.com/ImGuiNET/ImGui.NET-nativebuild) release flow. Current versions in this fork build `cimgui` directly in CI (see `.github/workflows/build.yml`) and pack the resulting binaries into the NuGet package.

To obtain a debuggable version of the native code for current versions:

1. Build `cimgui` locally using the same CMake configure/build steps used in `.github/workflows/build.yml`.
2. Copy the produced binaries (`cimgui.dll`, `libcimgui.so`, or `libcimgui.dylib`) into your application, or into `deps/cimgui/<rid>/` for local packaging.
3. Run the program under a native debugger, or enable mixed-mode debugging in Visual Studio.

`download-native-deps.ps1` downloads from the legacy nativebuild releases only when the requested tag exists there, and now fails fast with an explicit message when it does not.

# See Also

https://github.com/ocornut/imgui
> Dear ImGui is a bloat-free graphical user interface library for C++. It outputs optimized vertex buffers that you can render anytime in your 3D-pipeline enabled application. It is fast, portable, renderer agnostic and self-contained (no external dependencies).

> Dear ImGui is designed to enable fast iterations and to empower programmers to create content creation tools and visualization / debug tools (as opposed to UI for the average end-user). It favors simplicity and productivity toward this goal, and lacks certain features normally found in more high-level libraries.

> Dear ImGui is particularly suited to integration in games engine (for tooling), real-time 3D applications, fullscreen applications, embedded applications, or any applications on consoles platforms where operating system features are non-standard.

See the [official screenshot thread](https://github.com/ocornut/imgui/issues/123) for examples of many different kinds of interfaces created with Dear ImGui.

https://github.com/cimgui/cimgui
> This is a thin c-api wrapper for the excellent C++ intermediate gui imgui. This library is intended as a intermediate layer to be able to use imgui from other languages that can interface with C .

# Publishing (Fork Maintainer Guide)

## Package Identity

This fork publishes under the NuGet package ID **`TomLei.ImGui.NET`**.

## Configuring the `NUGET_API_KEY` Secret

1. Log in to [nuget.org](https://www.nuget.org/) and go to **Account settings → API Keys**.
2. Create a new key:
   - **Key name:** `TomLei.ImGui.NET GitHub Actions`
   - **Scopes:** `Push new packages and package versions`
   - **Glob pattern:** `TomLei.ImGui.NET*`
3. Copy the generated key once.
4. In the GitHub repository, go to **Settings → Secrets and variables → Actions → New repository secret**:
   - **Name:** `NUGET_API_KEY`
   - **Value:** *(paste your NuGet API key)*

## Triggering a Package Publish

Publishing to nuget.org happens automatically when you push a version tag matching `v*`:

```bash
git tag v1.92.7.1
git push origin v1.92.7.1
```

The GitHub Actions CI workflow will then:
1. Build native `cimgui` binaries in a RID matrix.
2. Collect those native artifacts into `deps/cimgui/<rid>/`.
3. Restore and build the solution.
4. Pack one NuGet package containing all RID native assets.
5. Upload the `.nupkg` as a workflow artifact.
6. Publish to [nuget.org](https://www.nuget.org/) using the `NUGET_API_KEY` secret.

## Inspecting CI-Generated Artifacts Before Publishing

Every CI run (including branch pushes and pull requests) uploads the packed `.nupkg` file as a workflow artifact named **`nuget-packages`**. To inspect a package before it is published:

1. Go to the [Actions tab](https://github.com/tom-lei/ImGui.NET/actions) of this repository.
2. Open the workflow run you want to inspect.
3. Download the **`nuget-packages`** artifact from the **Artifacts** section.
4. Inspect the `.nupkg` file (it is a ZIP archive) to verify contents before publishing.
