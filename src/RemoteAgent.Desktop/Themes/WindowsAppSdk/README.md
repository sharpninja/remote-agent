# Windows App SDK Theme Dictionaries

These files are downloaded from Microsoft's published WinUI source dictionaries:

- https://raw.githubusercontent.com/microsoft/microsoft-ui-xaml/main/src/controls/dev/CommonStyles/Common_themeresources.xaml
- https://raw.githubusercontent.com/microsoft/microsoft-ui-xaml/main/src/controls/dev/CommonStyles/Common_themeresources_any.xaml

Notes
- In WinUI, dark theme values are defined in the `Default` theme dictionary.
- Light theme values are defined in the `Light` theme dictionary.
- This project maps selected colors from those dictionaries into Avalonia theme resources in `src/RemoteAgent.Desktop/App.axaml`.
