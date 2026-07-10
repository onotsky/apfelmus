# Architektur & Entwicklungshinweise

Diese Datei fasst Aufbau, Konventionen und Fallstricke des Repositories fuer die
Weiterentwicklung zusammen.

## Repository Overview

Apfelmus is a WPF desktop client for the **appleJuice network** (an eDonkey2000-like P2P file-sharing protocol). The GUI itself has no networking logic beyond talking to a separately running **appleJuice core** process (a different program, not part of this repo) over a plain XML/HTTP API.

Sibling project (not in this repo, referenced for context only): `../gui-java` — the official, cross-platform appleJuice client (Java/Swing). It is **GPLv2-licensed**; see "License" and "Partlist rendering" below for why that matters.

## Commands

```bash
# Restore + build (Windows + Visual Studio 2022+ with the ".NET Desktop Development" workload required)
dotnet restore
dotnet build

# One-time migration of a pre-existing installation's Config.dat (BinaryFormatter) to Config.xml (XmlSerializer)
dotnet run --project ConfigMigrator
```

There is no test project and no lint step configured in this repo.

**This solution cannot be built or run outside Windows.** WPF has no cross-platform runtime — this is true regardless of .NET Framework vs. modern .NET, and applies equally to `dotnet build` in a terminal and to VS Code's C# Dev Kit. If working from a non-Windows environment, changes can only be reviewed statically (reading diffs, grepping for known-bad patterns); always tell the user a build was not verified.

## Architecture

### Projects (`Apfelmus.sln`)

| Project | Contents |
|---|---|
| `Apfelmus` | The WPF GUI itself: `MainWindow`, `StartWindow` (login), `SettingsWindow`, `OpenFolderWindow`, `RenameDownloadWindow`, splash screen. Also owns **all WPF-specific presentation code**: the `IValueConverter`s (`Converters/`), `ThemeManager`/`LanguageDictionary`/`SingleInstance` (`Logic/`), and the image resources (`Images/`) — these were moved here out of `ApfelmusFramework` so the framework can stay platform-neutral. Note: the moved files intentionally **keep their original `ApfelmusFramework.Classes.Converter` / `ApfelmusFramework.Classes.Logic` namespaces** (only their assembly changed), so the `ApfelmusFramework.Classes.Logic` namespace now legitimately spans two assemblies (framework: `WebConnect` etc.; app: `ThemeManager` etc.) |
| `ApfelmusFramework` | **Platform-neutral core library (`net10.0`, no WPF/Windows):** core HTTP/XML communication (`WebConnect`), data models/DTOs, config (de)serialization, MD5, and helper logic. Deliberately UI-framework-agnostic so both the WPF client and a future Avalonia port can share it |
| `WpfCustomControlLibrary1` (assembly `CloseableTab`) | `CloseableTabItem` control used for dynamic search-result tabs |
| `ConfigMigrator` | Standalone console tool, see "Config persistence" below |

`Apfelmus`, `WpfCustomControlLibrary1` and `ConfigMigrator` target `net10.0-windows7.0` (`Apfelmus`/`WpfCustomControlLibrary1` with `UseWPF=true`); **`ApfelmusFramework` targets plain `net10.0`** (no `UseWPF`, no `System.Drawing`/`System.Windows`) so it stays platform-neutral. `GenerateAssemblyInfo=false` in every csproj — each project keeps a legacy hand-written `Properties/AssemblyInfo.cs` instead of the SDK-generated one; when adding platform-specific APIs, the `[assembly: SupportedOSPlatform("windows7.0")]` attribute has to be added there manually (it does **not** get auto-generated as it would in a normal SDK-style project), or CA1416 warnings appear even though the project is Windows-only.

`ApfelmusFramework.csproj` has an explicit `<Compile Remove>` block for ~14 files/folders (`Classes/Dir`, `Classes/DownloadPartlist`, `Classes/GetObject`, `Classes/UserPartList`, `Classes/GetSession/GetSession.cs`, `Classes/Information/{FileSystem,Information}.cs`, `Classes/Logic/AppleJuice.cs`, `Classes/Modified/{Informations,Modified}.cs`, `Classes/Share/MyDirectories*.cs`). These are dead scaffolding from an earlier refactor (some reference a namespace, `Apfelmus.Interfaces`, that no longer exists) that were never part of the old non-SDK project's explicit `<Compile>` list. SDK-style implicit globbing would otherwise sweep them back into the build. **Do not delete the exclusion block**, and don't be surprised these files exist on disk but aren't compiled. Note there are two different `AppleJuice.cs` files: the active DTO is `Classes/Allgemein/AppleJuice.cs`; `Classes/Logic/AppleJuice.cs` is the dead/excluded one.

### appleJuice core protocol (`WebConnect.cs`)

The core exposes plain HTTP endpoints, hand-parsed (no `HttpClient`/XML library abstraction over the wire format itself — raw socket GET requests in `WebConnect.GetHttpResult`/`StartXMLFunction`). Two families:
- `/xml/*.xml` (e.g. `information.xml`, `modified.xml?filter=...`, `share.xml`, `settings.xml`, `downloadpartlist.xml`, `userpartlist.xml`, `directory.xml`) — polled/queried state, deserialized via `IXmlSerializer` implementations per DTO.
- `/function/*` (e.g. `search`, `canceldownload`, `setpriority`, `serverlogin`, `setpassword`, `exitcore`) — one-shot commands.

Every request carries `password=<md5>` in the query string — **the core itself requires the MD5 hex digest of the plaintext password**, computed by `CreateMd5Hash.GetMD5Hash`. This is a wire-protocol requirement of the external core, not a client-side security choice — do not "improve" it (e.g. to bcrypt), that would break login against any real core.

### Config persistence

`ApfelmusFramework/Classes/Serializer/ConfigSerializer.cs` serializes `Config` (`ApfelmusFramework/Classes/Config/Config.cs`) to `%AppData%\Apfelmus\Config.xml` via `XmlSerializer`. This used to be `BinaryFormatter` writing `Config.dat` (renamed `BinarySerializer` → `ConfigSerializer`); that dependency was deliberately removed from `Apfelmus`/`ApfelmusFramework` entirely so those two projects have no BinaryFormatter dependency (which is fully removed in .NET 9+). The **only** place `BinaryFormatter`/`EnableUnsafeBinaryFormatterSerialization` still exists is the standalone `ConfigMigrator` console project, which reads an old `Config.dat` via a local `LegacyConfig` class + custom `SerializationBinder` and writes it back out through the real `ConfigSerializer` — keeping the legacy-format dependency fully isolated from the shipped app. Since the whole solution now targets .NET 10 (where `BinaryFormatter` is no longer in the framework), `ConfigMigrator` restores it via the unsupported `System.Runtime.Serialization.Formatters` NuGet package — scoped to that one project only; do not add that package (or the `EnableUnsafeBinaryFormatterSerialization` flag) to `Apfelmus`/`ApfelmusFramework`.

**Namespace gotcha**: `ApfelmusFramework.Classes.Serializer` and `ApfelmusFramework.Classes.Logic` are sibling namespaces of `ApfelmusFramework.Classes.Config`. Inside any file in that `ApfelmusFramework.Classes.*` hierarchy, a bare `using ApfelmusFramework.Classes.Config;` + unqualified `Config` reference resolves to the **namespace**, not the class (CS0118) — C#'s "enclosing namespace" lookup wins over a `using` directive here regardless of alias tricks. The established, working pattern (see `DirectoryChildren.cs`, `ConfigSerializer.cs`, `ThemeManager.cs`) is to reference it fully as `Config.Config` and skip the `using` for that namespace entirely. This does NOT apply in the `Apfelmus` project itself (different top-level namespace, no collision) — plain `using ApfelmusFramework.Classes.Config;` + bare `Config` is fine there.

### Theme system

Runtime-switchable Dark/Light theme (`Apfelmus/Logic/ThemeManager.cs`), charcoal+green palette. Named brush tokens (`WindowBackgroundBrush`, `SurfaceBrush`, `AccentBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `ValueBrush`, etc.) are defined identically in `Apfelmus/Resourcen/Theme.Dark.xaml` and `Theme.Light.xaml`; `ThemeManager.Apply(themeName)` swaps the merged dictionary in `Application.Current.Resources` at runtime, `ApplyStartupTheme()` restores the saved choice from `Config.Theme` on launch. `Apfelmus/Resourcen/ControlStyles.xaml` holds implicit (no `x:Key`) styles for standard controls (Button, TextBox, ComboBox, TabControl, DataGrid, TreeView, ScrollBar, etc.) — any new standard WPF control picks up theming automatically; only custom/exotic controls need explicit `DynamicResource` wiring. Icons throughout the GUI are hand-drawn WPF vector primitives (`Viewbox`/`Canvas`/`Polygon`/`Line`/`Ellipse` with `Fill`/`Stroke` bound to the brush tokens above) rather than bitmaps, specifically so they re-tint with the active theme — follow that pattern for any new icon rather than adding a PNG. Exceptions left as real bitmaps on purpose: the language-flag icons (real national flags) and `FilenameToImage`'s converter output (extracts the actual Windows shell icon per file type at runtime).

`MainWindow` uses a fully custom title bar (`WindowStyle="None"` + `WindowChrome`) instead of native OS chrome, specifically so the title bar also follows the app's own theme rather than the OS light/dark setting. `WindowChrome.CaptionHeight` must keep matching the custom title-bar row's height, and any interactive element inside that row needs `WindowChrome.IsHitTestVisibleInChrome="True"` or it becomes unclickable (swallowed by the drag-window hit-test).

### Partlist rendering

`MainWindow.xaml.cs`'s `RenderPartList` (download/upload byte-availability bar) is an **independently designed** implementation (single-row `WriteableBitmap`, column-major two-pointer scan, formula-based source-count coloring) — it used to be a direct port of `gui-java`'s `DownloadPartListPanel`, which made this repo a GPL derivative work. It was deliberately rewritten from scratch (not translated) to remove that dependency. **Do not re-derive this logic by reading `gui-java` source again** — that would reintroduce the exact GPL-derivation problem this rewrite was meant to resolve.

### Known limitations

- Files must stay **under 2GB** — `Part.FromPosition` / `FileInformation.Filesize` are modeled as `int`, not `long`.
- License is **GPL-2.0-or-later** (`LICENSE`) — a deliberate, current choice (not currently forced by any dependency), see README.

### Git conventions

Do not add any AI/tool `Co-Authored-By:` trailer to commit messages in this repo (explicit user preference).
