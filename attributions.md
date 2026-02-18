# Third-Party Attributions

This document lists third-party libraries and bundled assets currently used in this repository, with project and license links.

## NuGet Libraries (Direct Dependencies)

| Library | Version | Website / Package | License |
|---|---:|---|---|
| Appium.WebDriver | 5.0.0 | https://github.com/appium/dotnet-client | https://www.nuget.org/packages/Appium.WebDriver/5.0.0/license |
| Avalonia | 11.3.6 | https://avaloniaui.net/ | https://spdx.org/licenses/MIT.html |
| Avalonia.Desktop | 11.3.6 | https://avaloniaui.net/ | https://spdx.org/licenses/MIT.html |
| Avalonia.Fonts.Inter | 11.3.6 | https://avaloniaui.net/ | https://spdx.org/licenses/MIT.html |
| Avalonia.Headless | 11.3.6 | https://avaloniaui.net/ | https://spdx.org/licenses/MIT.html |
| Avalonia.Headless.XUnit | 11.3.6 | https://avaloniaui.net/ | https://spdx.org/licenses/MIT.html |
| Avalonia.Themes.Fluent | 11.3.6 | https://avaloniaui.net/ | https://spdx.org/licenses/MIT.html |
| coverlet.collector | 6.0.4 | https://github.com/coverlet-coverage/coverlet | https://spdx.org/licenses/MIT.html |
| FluentAssertions | 8.0.0 | https://xceed.com/products/unit-testing/fluent-assertions/ | https://www.nuget.org/packages/FluentAssertions/8.0.0/license |
| FluentAvaloniaUI | 2.3.0 | https://github.com/amwx/FluentAvalonia | https://spdx.org/licenses/MIT.html |
| Google.Protobuf | 3.28.2 | https://github.com/protocolbuffers/protobuf | https://spdx.org/licenses/BSD-3-Clause.html |
| Grpc.AspNetCore | 2.64.0 | https://github.com/grpc/grpc-dotnet | https://spdx.org/licenses/Apache-2.0.html |
| Grpc.Core.Api | 2.64.0 | https://github.com/grpc/grpc-dotnet | https://spdx.org/licenses/Apache-2.0.html |
| Grpc.Net.Client | 2.64.0 | https://github.com/grpc/grpc-dotnet | https://spdx.org/licenses/Apache-2.0.html |
| Handlebars.Net | 2.1.6 | https://github.com/Handlebars-Net | https://spdx.org/licenses/MIT.html |
| LiteDB | 5.0.21 | https://www.litedb.org/ | https://spdx.org/licenses/MIT.html |
| Markdig | 0.37.0 | https://github.com/lunet-io/markdig | https://spdx.org/licenses/BSD-2-Clause.html |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | https://asp.net/ | https://spdx.org/licenses/MIT.html |
| Microsoft.AspNetCore.TestHost | 10.0.0 | https://asp.net/ | https://spdx.org/licenses/MIT.html |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | https://dot.net/ | https://spdx.org/licenses/MIT.html |
| Microsoft.Extensions.Logging.Debug | 10.0.0 | https://dot.net/ | https://spdx.org/licenses/MIT.html |
| Microsoft.Maui.Controls | SDK-managed (`$(MauiVersion)`) | https://github.com/dotnet/maui | https://github.com/dotnet/maui/blob/main/LICENSE.txt |
| Microsoft.NET.Test.Sdk | 17.14.1 | https://github.com/microsoft/vstest | https://spdx.org/licenses/MIT.html |
| Tulesha.ToolBarControls.Avalonia | 11.3.6 | https://github.com/Tulesha/ToolBarControls.Avalonia | https://www.nuget.org/packages/Tulesha.ToolBarControls.Avalonia/11.3.6/license |
| xunit | 2.9.3 | https://xunit.net/ | https://spdx.org/licenses/Apache-2.0.html |
| xunit.runner.visualstudio | 3.1.4 | https://xunit.net/ | https://spdx.org/licenses/Apache-2.0.html |
| Xunit.SkippableFact | 1.4.13 | https://github.com/AArnott/Xunit.SkippableFact | https://spdx.org/licenses/MS-PL.html |

## Bundled Third-Party Assets

| Asset | Location | Source | License |
|---|---|---|---|
| WinUI theme dictionaries (`Common_themeresources*.xaml`) | `src/RemoteAgent.Desktop/Themes/WindowsAppSdk/` | https://github.com/microsoft/microsoft-ui-xaml | https://github.com/microsoft/microsoft-ui-xaml/blob/main/LICENSE |
| Fluent UI MDL2-compatible icon font (`fabric-icons-a13498cf.woff`) | `src/RemoteAgent.Desktop/Assets/Fonts/` | https://www.npmjs.com/package/@fluentui/font-icons-mdl2 | https://github.com/microsoft/fluentui/blob/master/packages/font-icons-mdl2/LICENSE |
| Font Awesome Solid font (`fa-solid-900.ttf`) | `src/RemoteAgent.App/Resources/Fonts/` | https://fontawesome.com/ | https://fontawesome.com/license/free |
| Open Sans fonts (`OpenSans-Regular.ttf`, `OpenSans-Semibold.ttf`) | `src/RemoteAgent.App/Resources/Fonts/` | https://fonts.google.com/specimen/Open+Sans | https://spdx.org/licenses/Apache-2.0.html |
| .NET MAUI template image (`dotnet_bot.png`) | `src/RemoteAgent.App/Resources/Images/` | https://github.com/dotnet/maui | https://github.com/dotnet/maui/blob/main/LICENSE.txt |

## Container Base Image

| Image | Location | Source | License |
|---|---|---|---|
| `mcr.microsoft.com/dotnet/aspnet:10.0` | `Dockerfile` | https://mcr.microsoft.com/en-us/product/dotnet/aspnet/about | https://github.com/dotnet/dotnet-docker/blob/main/LICENSE |

## Notes

- Versions listed are direct package references currently present in project files.
- Transitive dependencies are not enumerated in this document.
- Some asset licenses include additional trademark/brand usage terms; consult the upstream license pages for full details.
