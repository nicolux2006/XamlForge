# XamlForge

A Windows desktop designer for WPF interfaces, with integrated XAML and PowerShell editing. Built with WPF and .NET.
Developed by [@nicolux2006](https://github.com/nicolux2006).
This is an independent project, not an official Microsoft product.

## Features

- Visual WPF design with a toolbox, element hierarchy and live preview.
- Move, resize, copy and delete controls, with undo and redo.
- Context-sensitive properties with dropdowns, checkboxes, numeric inputs and color selection.
- Design, XAML and split views, with syntax highlighting and validation.
- PowerShell event handlers and standalone script export with embedded XAML.
- English and German interface, configurable editor fonts and light/dark appearance.

## Install the application

Download the **Windows x64 MSI installer** from this repository's **GitHub Releases**, run it and follow the setup instructions. Release installers are created with **Advanced Installer**. You do not need Advanced Installer or the .NET SDK to use XamlForge.

The application package includes the .NET/WPF runtime. No separate .NET runtime installation is needed. If an MSI has not been published yet, build the application from source using the instructions below.

XamlForge uses Windows PowerShell for previews. PowerShell itself is not bundled. Exported scripts can also run with PowerShell 7 on Windows using `pwsh -STA -File .\MyWindow.ps1`.

Settings and error logs are stored in `%LOCALAPPDATA%\XamlForge`. Only open or execute trusted XAML, external assemblies and PowerShell code; previews are not sandboxed.

## Build from source

Use Windows x64 with the **.NET 10 SDK** and **PowerShell 7**. A compatible Visual Studio installation can be used to open `XamlForge.slnx`.

From a PowerShell 7 terminal in the repository root:

```powershell
.\build.ps1
```

The first build restores NuGet packages, so initial setup requires internet access or a populated package cache. Subsequent builds reuse the local caches. No separate runtime acquisition scripts are needed.

The self-contained application is published to `artifacts/app/XamlForge.exe`. Close any running instance from that folder before rebuilding. To compile a development build without publishing:

```powershell
.\build.ps1 -BuildOnly -Configuration Debug
.\run.ps1 -NoBuild
```

Use the complete contents of `artifacts/app` as the application payload in **Advanced Installer**, preserving its files, subfolders and third-party notices. Configure an x64 MSI with `XamlForge.exe` as the executable and `Assets/XamlForge.ico` as its icon. Do not include personal settings, logs or debug symbols. The Advanced Installer project and MSI build are maintained separately; the repository scripts do not generate an MSI.

Publish the finished MSI in **GitHub Releases** rather than committing packaged binaries. To create source and application-payload ZIPs with SHA-256 checksums:

```powershell
.\package.ps1
```

The archives are written to `dist`. Use `-SkipPublish` to package an existing build. The application ZIP is a payload for installer preparation; the MSI is the user-facing download.

## Source layout

| Project | Purpose |
| --- | --- |
| `XamlForge.App` | Main window, settings, dialogs and application assets |
| `XamlForge.Core` | Documents, undo/redo, settings and PowerShell export |
| `XamlForge.Designer` | Visual designer, properties, code editor and shared UI styles |

Projects live under `src`. `examples/Greeting.xfg` provides a sample design with event handlers; the XAML variant contains only the layout. Save designs as `.xfg` to preserve events or export them as `.ps1` for standalone execution.

`build-icon.ps1` regenerates the Windows icon from `src/XamlForge.App/Assets/XamlForgeIcon.xaml`. `run.ps1` builds and starts the development application. `clean.ps1` removes intermediate build files while preserving the published application and distribution archives.

Local IDE state and generated build outputs are excluded by `.gitignore`. They can be recreated using the build commands above.

## Dependencies

XamlForge uses .NET 10 / WPF and AvalonEdit 6.3.1.120. NuGet versions are specified in the project files.

Third-party license and notice files are retained in `THIRD-PARTY-NOTICES.md` and `third-party-licenses`, and included in application packages. Preserve these files in the MSI when redistributing the application or its dependencies.

No license has been selected for XamlForge's own source code yet. The dependency license texts apply to their respective components.
