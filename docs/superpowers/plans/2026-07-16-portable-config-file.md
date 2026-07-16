# Portable Configuration File Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Load one selected portable `.txt` configuration file at startup and save current settings to it when the user chooses Save Settings or closes the app.

**Architecture:** Add a focused configuration-path store that persists only the path of the selected file in local app metadata. `Form1` owns startup loading, Save Settings, close-time persistence, and the missing-file selection dialog. Settings UI removes named profiles and exposes one configuration-file path with Browse plus the existing Save Settings action.

**Tech Stack:** C# 7.3, .NET Framework 4.8, WPF, existing executable test runner.

## Global Constraints

- The portable configuration payload remains plain text with the existing `key=value` format.
- Configuration paths support local and UNC network paths.
- The app does not block startup or overwrite a missing/unavailable configuration file.
- Do not change PLC motion, camera, or DXF behavior.
- Do not commit because the working tree contains unrelated ongoing changes.

---

### Task 1: Persist the selected configuration-file path

**Files:**
- Create: `src/DACDT_2026.App/ConfigurationFilePathStore.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Produces: `ConfigurationFilePathStore(string defaultConfigurationPath, string selectionStatePath)`.
- Produces: `string GetSelectedPath()` and `bool TrySaveSelectedPath(string path)`.
- Produces: `bool NeedsSelection(string path)`.

- [x] **Step 1: Write the failing test**

```csharp
private static void ConfigurationFilePathIsRememberedAndMissingFilesNeedSelection()
{
    string root = Path.Combine(Path.GetTempPath(), "dacdt-config-test-" + Guid.NewGuid());
    string defaultPath = Path.Combine(root, "Documents", "DACDT_2026_settings.txt");
    string statePath = Path.Combine(root, "state", "config_path.txt");
    var store = new ConfigurationFilePathStore(defaultPath, statePath);

    AssertEqual(defaultPath, store.GetSelectedPath(), "The default configuration file must be used before a path is selected.");
    AssertTrue(store.TrySaveSelectedPath(@"\\server\dacdt\machine.txt"), "A UNC configuration path must be remembered.");
    AssertEqual(@"\\server\dacdt\machine.txt", store.GetSelectedPath(), "The remembered path must be restored.");
    AssertTrue(store.NeedsSelection(store.GetSelectedPath()), "A missing selected file must request a replacement selection.");
}
```

- [x] **Step 2: Run test to verify it fails**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
```

Expected: compilation fails because `ConfigurationFilePathStore` does not exist.

- [x] **Step 3: Write minimal implementation**

```csharp
public sealed class ConfigurationFilePathStore
{
    private readonly string defaultConfigurationPath;
    private readonly string selectionStatePath;

    public string GetSelectedPath()
    {
        try
        {
            string selected = File.Exists(selectionStatePath) ? File.ReadAllText(selectionStatePath).Trim() : string.Empty;
            return string.IsNullOrWhiteSpace(selected) ? defaultConfigurationPath : selected;
        }
        catch { return defaultConfigurationPath; }
    }

    public bool TrySaveSelectedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(selectionStatePath));
            File.WriteAllText(selectionStatePath, path.Trim());
            return true;
        }
        catch { return false; }
    }

    public bool NeedsSelection(string path) { return string.IsNullOrWhiteSpace(path) || !File.Exists(path); }
}
```

Add the source file to both project files so the app and test runner compile it.

- [x] **Step 4: Run test to verify it passes**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

### Task 2: Use the portable file for startup, Save Settings, and closing

**Files:**
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Consumes: `ConfigurationFilePathStore.GetSelectedPath`, `TrySaveSelectedPath`, and `NeedsSelection`.
- Produces: `ConfigurationFilePathInput` and `BrowseConfigurationFileCommand` on `WpfUiState`.

- [x] **Step 1: Write the failing test**

```csharp
private static void PortableConfigurationIsLoadedSavedAndRecoveredAtStartup()
{
    string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
    AssertTrue(formSource.Contains("LoadSelectedConfigurationAtStartup"), "Startup must load the remembered configuration file.");
    AssertTrue(formSource.Contains("PromptForConfigurationFileAsync"), "A missing configuration file must prompt for replacement selection.");
    AssertTrue(formSource.Contains("SaveSettingsToFile(configurationFilePath)"), "Save Settings must write to the selected portable file.");
    AssertTrue(formSource.Contains("SyncSettingsFromUiForPersistence();"), "Closing must snapshot current UI values before saving.");
}
```

- [x] **Step 2: Run test to verify it fails**

Run the same test-runner command from Task 1.

Expected: test failure stating that portable startup loading is missing.

- [x] **Step 3: Write minimal implementation**

```csharp
private void LoadSelectedConfigurationAtStartup()
{
    configurationFilePath = configurationFilePathStore.GetSelectedPath();
    ui.ConfigurationFilePathInput = configurationFilePath;
    if (configurationFilePathStore.NeedsSelection(configurationFilePath))
    {
        configurationFileSelectionRequired = true;
        return;
    }

    LoadSettingsFromFile(configurationFilePath);
}

private async Task PromptForConfigurationFileAsync()
{
    var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Configuration files (*.txt)|*.txt|All files (*.*)|*.*" };
    if (dialog.ShowDialog(this) == true)
        await SelectConfigurationFileAsync(dialog.FileName);
}
```

At startup, load the path from `ConfigurationFilePathStore`; load it when present, otherwise queue the dialog after `Loaded`. Save Settings and the final `OnClosing` path both sync UI values then write the selected file. On a successfully selected path, persist its pointer and load it immediately.

- [x] **Step 4: Run test to verify it passes**

Run the Task 1 test-runner command.

Expected: `All tests passed.`

### Task 3: Simplify Settings UI to one configuration file

**Files:**
- Modify: `src/DACDT_2026.App/Views/SettingsView.xaml`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Consumes: `ConfigurationFilePathInput` and `BrowseConfigurationFileCommand` from Task 2.
- Produces: a Settings page with configuration path, Browse, and one Save Settings action.

- [x] **Step 1: Write the failing test**

```csharp
private static void SettingsUsesOnePortableConfigurationFileWorkflow()
{
    string xaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "SettingsView.xaml"));
    AssertTrue(xaml.Contains("Configuration File"), "Settings must show the selected configuration file.");
    AssertTrue(xaml.Contains("BrowseConfigurationFileCommand"), "Settings must let the operator choose a portable configuration file.");
    AssertTrue(!xaml.Contains("Configuration Profiles"), "Named profiles must be removed from Settings.");
}
```

- [x] **Step 2: Run test to verify it fails**

Run the Task 1 test-runner command.

Expected: test failure because the old Configuration Profiles UI is still present.

- [x] **Step 3: Write minimal implementation**

Replace the Configuration Profiles panel with a `Configuration File` path input and Browse command. Keep the existing `Save Settings` command as the only settings-save action. Remove profile-only commands, state, and methods that no longer have a UI consumer.

```xml
<TextBlock Text="Configuration File" Style="{StaticResource PanelTitleStyle}"/>
<Grid Margin="0,10,0,0">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <TextBox Text="{Binding ConfigurationFilePathInput, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
    <Button Grid.Column="1" Content="Browse" Command="{Binding BrowseConfigurationFileCommand}" Style="{StaticResource SecondaryButtonStyle}"/>
</Grid>
```

- [x] **Step 4: Run the full verification**

Run:

```powershell
git diff --check
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'src\DACDT_2026.App\DACDT_2026.csproj' /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal
```

Expected: no diff whitespace errors, `All tests passed.`, and both Release executables build.
