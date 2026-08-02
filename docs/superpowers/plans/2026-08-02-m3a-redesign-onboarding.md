# M3-A Visual Redesign + Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retheme the entire app to #1D1D1D/#E5BDDF, replace PillWindow with a Dynamic Island-style floating capsule, and add a 4-step first-run onboarding wizard.

**Architecture:** Three independent subsystems wired in App.xaml.cs. (1) Color swap: find-and-replace hex strings across all XAML files + CountToColorConverter heatmap ramp. (2) DynamicIslandWindow replaces PillWindow — transparent always-on-top window, code-behind drives all state transitions (waveform DispatcherTimer, spring animations, error auto-dismiss). (3) OnboardingWindow is a blocking ShowDialog() on first run; sets `HasCompletedOnboarding = true` on completion; App.OnStartup still calls `InitializeAsync()` after the dialog (idempotent no-op for new users, real init for returning users).

**Tech Stack:** .NET 8, WPF, C# 12, CommunityToolkit.Mvvm 8.3.2, Microsoft.Win32 (DisplaySettingsChanged)

---

## File Structure

**New files:**
- `ViewModels/DynamicIslandViewModel.cs` — syncs DictationEngine state/error; replaces PillViewModel
- `Windows/DynamicIslandWindow.xaml` — transparent overlay window, capsule UI with 3 states
- `Windows/DynamicIslandWindow.xaml.cs` — state transitions, waveform timer, spinner, error auto-dismiss
- `Windows/OnboardingWindow.xaml` — 4-step setup wizard UI
- `Windows/OnboardingWindow.xaml.cs` — step navigation, model download with progress, settings save

**Modified files:**
- `Models/AppSettings.cs` — add `HasCompletedOnboarding bool`
- `Windows/MainWindow.xaml` — color swap
- `Views/InsightsView.xaml` — color swap
- `Views/SettingsView.xaml` — color swap
- `Converters/CountToColorConverter.cs` — new heatmap ramp
- `App.xaml.cs` — onboarding gate + swap PillWindow → DynamicIslandWindow

**Deleted files:**
- `Windows/PillWindow.xaml`
- `Windows/PillWindow.xaml.cs`
- `ViewModels/PillViewModel.cs`

---

## Task 1: Color retheme — XAML and heatmap converter

**Files:**
- Modify: `Windows/MainWindow.xaml`
- Modify: `Views/InsightsView.xaml`
- Modify: `Views/SettingsView.xaml`
- Modify: `Converters/CountToColorConverter.cs`

No unit tests (UI). Build verification only.

Color map applied across all files:

| Old | New | Role |
|-----|-----|------|
| `#0F172A` | `#1D1D1D` | Window/page background |
| `#0B1120` | `#141414` | Sidebar background |
| `#1E293B` | `#242424` | Cards, nav active state |
| `#0d9488` | `#E5BDDF` | Primary accent |
| `#2DD4BF` | `#E5BDDF` | Secondary accent (collapses to same) |
| `#334155` | `#333333` | Borders |
| `#64748B` | `#666666` | Muted labels |
| `#94A3B8` | `#888888` | Secondary text |
| `#CBD5E1` | `#CCCCCC` | Primary text (secondary) |
| `#F8FAFC` | `#F0F0F0` | Primary text |
| `#E2E8F0` | `#E0E0E0` | Hover text |

Buttons that gain `#E5BDDF` background get `#1D1D1D` foreground (dark text on light pink).

- [ ] **Step 1: Replace MainWindow.xaml**

Full replacement — only color values change, structure is identical:

```xml
<Window x:Class="WhisperFlowLocal.Windows.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WhisperFlowLocal.ViewModels"
        xmlns:views="clr-namespace:WhisperFlowLocal.Views"
        Title="Whisper Flow Local"
        Width="900" Height="620"
        MinWidth="800" MinHeight="500"
        Background="#1D1D1D"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
        <DataTemplate DataType="{x:Type vm:InsightsViewModel}">
            <views:InsightsView/>
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:SettingsViewModel}">
            <views:SettingsView/>
        </DataTemplate>

        <Style x:Key="NavBtn" TargetType="RadioButton">
            <Setter Property="Background"                 Value="Transparent"/>
            <Setter Property="Foreground"                 Value="#888888"/>
            <Setter Property="BorderThickness"            Value="0"/>
            <Setter Property="Padding"                    Value="16,10"/>
            <Setter Property="FontSize"                   Value="14"/>
            <Setter Property="HorizontalContentAlignment" Value="Left"/>
            <Setter Property="GroupName"                  Value="Nav"/>
            <Setter Property="Cursor"                     Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="RadioButton">
                        <Border Background="{TemplateBinding Background}"
                                Padding="{TemplateBinding Padding}"
                                CornerRadius="6" Margin="0,2">
                            <ContentPresenter VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsChecked" Value="True">
                                <Setter Property="Background" Value="#242424"/>
                                <Setter Property="Foreground" Value="#E5BDDF"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#242424"/>
                                <Setter Property="Foreground" Value="#E0E0E0"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="220"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Sidebar -->
        <Border Grid.Column="0" Background="#141414" BorderBrush="#242424" BorderThickness="0,0,1,0">
            <StackPanel Margin="12,24,12,0">
                <TextBlock Text="Whisper Flow" Foreground="#F0F0F0"
                           FontSize="15" FontWeight="SemiBold" Margin="16,0,0,24"/>
                <RadioButton Content="Insights" Style="{StaticResource NavBtn}"
                             IsChecked="True" Click="OnInsightsClick"/>
                <RadioButton Content="Settings" Style="{StaticResource NavBtn}"
                             Click="OnSettingsClick"/>
            </StackPanel>
        </Border>

        <!-- Content area -->
        <ContentControl Grid.Column="1" Content="{Binding CurrentView}"/>
    </Grid>
</Window>
```

- [ ] **Step 2: Replace InsightsView.xaml**

Full replacement — only color values and one accent fill change:

```xml
<UserControl x:Class="WhisperFlowLocal.Views.InsightsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:conv="clr-namespace:WhisperFlowLocal.Converters"
             Background="#1D1D1D"
             Loaded="OnLoaded">

    <UserControl.Resources>
        <conv:PercentToWidthConverter x:Key="PctWidth"/>
        <conv:CountToColorConverter  x:Key="CountColor"/>

        <Style x:Key="Card" TargetType="Border">
            <Setter Property="Background"   Value="#242424"/>
            <Setter Property="CornerRadius" Value="8"/>
            <Setter Property="Padding"      Value="20"/>
        </Style>
        <Style x:Key="CardLabel" TargetType="TextBlock">
            <Setter Property="Foreground" Value="#666666"/>
            <Setter Property="FontSize"   Value="12"/>
            <Setter Property="Margin"     Value="0,0,0,4"/>
        </Style>
        <Style x:Key="CardValue" TargetType="TextBlock">
            <Setter Property="Foreground" Value="#F0F0F0"/>
            <Setter Property="FontSize"   Value="28"/>
            <Setter Property="FontWeight" Value="Bold"/>
        </Style>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="28">

            <!-- Header row -->
            <Grid Margin="0,0,0,20">
                <TextBlock Text="Insights" Foreground="#F0F0F0" FontSize="22" FontWeight="Bold"/>
                <Button Content="Refresh" HorizontalAlignment="Right"
                        Command="{Binding RefreshCommand}"
                        Background="#242424" Foreground="#888888"
                        BorderThickness="0" Padding="12,6" Cursor="Hand"/>
            </Grid>

            <!-- Tabs -->
            <TabControl Background="Transparent" BorderThickness="0" Padding="0">

                <!-- Your usage -->
                <TabItem Header="Your usage" Foreground="#888888" Padding="14,8">
                    <StackPanel Margin="0,16,0,0">

                        <!-- Metric cards -->
                        <UniformGrid Rows="1" Columns="3" Margin="0,0,0,16">
                            <Border Style="{StaticResource Card}" Margin="0,0,8,0">
                                <StackPanel>
                                    <TextBlock Text="Total words dictated" Style="{StaticResource CardLabel}"/>
                                    <TextBlock Text="{Binding TotalWords, StringFormat=N0}" Style="{StaticResource CardValue}"/>
                                </StackPanel>
                            </Border>
                            <Border Style="{StaticResource Card}" Margin="4,0">
                                <StackPanel>
                                    <TextBlock Text="Today's avg WPM" Style="{StaticResource CardLabel}"/>
                                    <TextBlock Text="{Binding TodayWpm, StringFormat=N0}" Style="{StaticResource CardValue}"/>
                                </StackPanel>
                            </Border>
                            <Border Style="{StaticResource Card}" Margin="8,0,0,0">
                                <StackPanel>
                                    <TextBlock Text="AI fixes made" Style="{StaticResource CardLabel}"/>
                                    <TextBlock Text="{Binding TotalFixes, StringFormat=N0}" Style="{StaticResource CardValue}"/>
                                </StackPanel>
                            </Border>
                        </UniformGrid>

                        <!-- App breakdown -->
                        <Border Style="{StaticResource Card}" Margin="0,0,0,16">
                            <StackPanel>
                                <TextBlock Text="App breakdown" Style="{StaticResource CardLabel}" FontSize="14" Margin="0,0,0,12"/>
                                <ItemsControl ItemsSource="{Binding AppBreakdown}">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <Grid Margin="0,5">
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width="110"/>
                                                    <ColumnDefinition Width="*"/>
                                                    <ColumnDefinition Width="44"/>
                                                </Grid.ColumnDefinitions>
                                                <TextBlock Grid.Column="0" Text="{Binding AppName}"
                                                           Foreground="#CCCCCC" FontSize="13" VerticalAlignment="Center"/>
                                                <Rectangle Grid.Column="1" Height="14" HorizontalAlignment="Left"
                                                           Fill="#E5BDDF" RadiusX="3" RadiusY="3" Margin="0,0,8,0"
                                                           Width="{Binding Percent, Converter={StaticResource PctWidth}, ConverterParameter=280}"/>
                                                <TextBlock Grid.Column="2" Foreground="#666666" FontSize="13" VerticalAlignment="Center"
                                                           Text="{Binding Percent, StringFormat={}{0:F0}%}"/>
                                            </Grid>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </StackPanel>
                        </Border>

                        <!-- Streak heatmap -->
                        <Border Style="{StaticResource Card}">
                            <StackPanel>
                                <TextBlock Text="Dictation streak (last 13 weeks)" Style="{StaticResource CardLabel}" FontSize="14" Margin="0,0,0,12"/>
                                <ItemsControl ItemsSource="{Binding StreakDays}" HorizontalAlignment="Left">
                                    <ItemsControl.ItemsPanel>
                                        <ItemsPanelTemplate>
                                            <UniformGrid Columns="7"/>
                                        </ItemsPanelTemplate>
                                    </ItemsControl.ItemsPanel>
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <Rectangle Width="14" Height="14" Margin="2"
                                                       Fill="{Binding Count, Converter={StaticResource CountColor}}"
                                                       RadiusX="2" RadiusY="2">
                                                <Rectangle.ToolTip>
                                                    <ToolTip>
                                                        <TextBlock>
                                                            <Run Text="{Binding Date, StringFormat='yyyy-MM-dd', Mode=OneWay}"/>
                                                            <Run Text=" — "/>
                                                            <Run Text="{Binding Count, Mode=OneWay}"/>
                                                            <Run Text=" dictations"/>
                                                        </TextBlock>
                                                    </ToolTip>
                                                </Rectangle.ToolTip>
                                            </Rectangle>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </StackPanel>
                        </Border>

                    </StackPanel>
                </TabItem>

                <!-- Your voice -->
                <TabItem Header="Your voice" Foreground="#888888" Padding="14,8">
                    <Border Style="{StaticResource Card}" Margin="0,16,0,0">
                        <StackPanel>
                            <Grid Margin="0,0,0,14">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="200"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Text="Avg speaking length" Grid.Column="0" Style="{StaticResource CardLabel}" VerticalAlignment="Center"/>
                                <TextBlock Grid.Column="1" Foreground="#F0F0F0" VerticalAlignment="Center">
                                    <Run Text="{Binding AvgDurationSec, StringFormat=N1}"/><Run Text=" s"/>
                                </TextBlock>
                            </Grid>
                            <Grid Margin="0,0,0,14">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="200"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Text="WPM consistency" Grid.Column="0" Style="{StaticResource CardLabel}" VerticalAlignment="Center"/>
                                <TextBlock Grid.Column="1" Text="{Binding WpmConsistency}" Foreground="#F0F0F0" VerticalAlignment="Center"/>
                            </Grid>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="200"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Text="AI cleanup used" Grid.Column="0" Style="{StaticResource CardLabel}" VerticalAlignment="Center"/>
                                <TextBlock Grid.Column="1" Foreground="#F0F0F0" VerticalAlignment="Center">
                                    <Run Text="{Binding GroqUsagePct, StringFormat=N0}"/><Run Text="% of dictations"/>
                                </TextBlock>
                            </Grid>
                        </StackPanel>
                    </Border>
                </TabItem>

            </TabControl>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Replace SettingsView.xaml**

Full replacement — Save button gets #E5BDDF background with #1D1D1D foreground (dark text on pink):

```xml
<UserControl x:Class="WhisperFlowLocal.Views.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="#1D1D1D">

    <StackPanel Margin="32">
        <TextBlock Text="Settings" Foreground="#F0F0F0" FontSize="22" FontWeight="Bold" Margin="0,0,0,24"/>

        <Border Background="#242424" CornerRadius="8" Padding="24" MaxWidth="480" HorizontalAlignment="Left">
            <StackPanel>
                <TextBlock Text="Groq API" Foreground="#F0F0F0" FontSize="15" FontWeight="SemiBold" Margin="0,0,0,16"/>

                <TextBlock Text="API Key" Foreground="#666666" FontSize="12" Margin="0,0,0,4"/>
                <PasswordBox x:Name="ApiKeyBox"
                             Background="#1D1D1D" Foreground="#F0F0F0"
                             BorderBrush="#333333" BorderThickness="1"
                             Padding="8,6" Margin="0,0,0,16"
                             PasswordChanged="OnApiKeyChanged"/>

                <TextBlock Text="Model" Foreground="#666666" FontSize="12" Margin="0,0,0,4"/>
                <ComboBox ItemsSource="{Binding AvailableModels}"
                          SelectedItem="{Binding SelectedModel}"
                          Background="#1D1D1D" Foreground="#F0F0F0"
                          BorderBrush="#333333" Padding="8,6" Margin="0,0,0,24"/>

                <StackPanel Orientation="Horizontal">
                    <Button Content="Save" Command="{Binding SaveCommand}"
                            Background="#E5BDDF" Foreground="#1D1D1D"
                            BorderThickness="0" Padding="16,8" Cursor="Hand"/>
                    <TextBlock Text="{Binding SaveStatus}" Foreground="#E5BDDF"
                               VerticalAlignment="Center" Margin="12,0,0,0"/>
                </StackPanel>
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

- [ ] **Step 4: Update CountToColorConverter.cs with pink heatmap ramp**

```csharp
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WhisperFlowLocal.Converters;

public class CountToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = value is int c ? c : 0;
        var hex = count switch
        {
            0      => "#242424",
            1 or 2 => "#6b3f6b",
            <= 5   => "#8f4f8f",
            <= 9   => "#b86ab8",
            _      => "#E5BDDF"
        };
        return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```
git add Windows/MainWindow.xaml Views/InsightsView.xaml Views/SettingsView.xaml Converters/CountToColorConverter.cs
git commit -m "feat(m3a): retheme to #1D1D1D/#E5BDDF color palette"
```

---

## Task 2: Add HasCompletedOnboarding to AppSettings

**Files:**
- Modify: `Models/AppSettings.cs`

- [ ] **Step 1: Add the property**

Replace the file content:

```csharp
namespace WhisperFlowLocal.Models;

public class AppSettings
{
    public string GroqApiKey { get; set; } = string.Empty;
    public string GroqModel { get; set; } = "llama-3.3-70b-versatile";
    public bool HasCompletedOnboarding { get; set; } = false;
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```
git add Models/AppSettings.cs
git commit -m "feat(m3a): add HasCompletedOnboarding to AppSettings"
```

---

## Task 3: DynamicIslandViewModel

**Files:**
- Create: `ViewModels/DynamicIslandViewModel.cs`

Same pattern as PillViewModel: subscribes to DictationEngine.PropertyChanged and mirrors State + ErrorMessage. Code-behind on DynamicIslandWindow reads these two properties and drives all visual transitions.

- [ ] **Step 1: Create ViewModels/DynamicIslandViewModel.cs**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.ViewModels;

public partial class DynamicIslandViewModel : ObservableObject
{
    [ObservableProperty] private RecordingState _state = RecordingState.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public void SyncFrom(DictationEngine engine)
    {
        engine.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DictationEngine.State))
                State = engine.State;
            if (e.PropertyName == nameof(DictationEngine.ErrorMessage))
                ErrorMessage = engine.ErrorMessage;
        };
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```
git add ViewModels/DynamicIslandViewModel.cs
git commit -m "feat(m3a): add DynamicIslandViewModel"
```

---

## Task 4: DynamicIslandWindow

**Files:**
- Create: `Windows/DynamicIslandWindow.xaml`
- Create: `Windows/DynamicIslandWindow.xaml.cs`

Transparent always-on-top window positioned 24 px above the taskbar. The capsule is `Visibility.Collapsed` at Idle — the window itself stays open. State transitions are driven by the ViewModel's PropertyChanged in code-behind. Waveform: 10 named Rectangle bars (`Bar0`–`Bar9`) driven by a DispatcherTimer at 80ms, heights randomised 4–16 px. Spinner: Ellipse with RotateTransform animated by a Storyboard resource.

- [ ] **Step 1: Create Windows/DynamicIslandWindow.xaml**

```xml
<Window x:Class="WhisperFlowLocal.Windows.DynamicIslandWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="" Width="240" Height="50"
        AllowsTransparency="True" Background="Transparent"
        WindowStyle="None" Topmost="True"
        ShowInTaskbar="False" ShowActivated="False"
        Focusable="False" IsHitTestVisible="False"
        ResizeMode="NoResize">

    <Window.Resources>
        <!-- Spinner: rotates a dashed arc 360° in 1 s, forever -->
        <Storyboard x:Key="SpinnerAnim" RepeatBehavior="Forever">
            <DoubleAnimation Storyboard.TargetName="SpinnerRotate"
                             Storyboard.TargetProperty="Angle"
                             From="0" To="360" Duration="0:0:1"/>
        </Storyboard>
    </Window.Resources>

    <Grid>
        <!-- Black rounded capsule — Collapsed at idle, animated open on recording -->
        <Border x:Name="Capsule" CornerRadius="25" Background="#000000"
                Width="0" Height="36"
                HorizontalAlignment="Center" VerticalAlignment="Center"
                Visibility="Collapsed" ClipToBounds="True">
            <Grid Margin="14,0">

                <!-- RECORDING: 10 animated waveform bars + "Listening" label -->
                <StackPanel x:Name="RecordingContent" Orientation="Horizontal"
                            VerticalAlignment="Center" HorizontalAlignment="Center"
                            Visibility="Collapsed">
                    <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="0,0,8,0">
                        <Rectangle x:Name="Bar0" Width="2" Height="8"  Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar1" Width="2" Height="12" Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar2" Width="2" Height="6"  Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar3" Width="2" Height="14" Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar4" Width="2" Height="9"  Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar5" Width="2" Height="11" Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar6" Width="2" Height="5"  Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar7" Width="2" Height="13" Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar8" Width="2" Height="7"  Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                        <Rectangle x:Name="Bar9" Width="2" Height="10" Fill="#E5BDDF" Margin="1,0" RadiusX="1" RadiusY="1" VerticalAlignment="Center"/>
                    </StackPanel>
                    <TextBlock Text="Listening" Foreground="#E5BDDF" FontSize="11" VerticalAlignment="Center"/>
                </StackPanel>

                <!-- TRANSCRIBING: spinning arc + "Transcribing…" label -->
                <StackPanel x:Name="TranscribingContent" Orientation="Horizontal"
                            VerticalAlignment="Center" HorizontalAlignment="Center"
                            Visibility="Collapsed">
                    <Ellipse Width="14" Height="14"
                             Stroke="#E5BDDF" StrokeThickness="1.5"
                             StrokeDashArray="6 2" Margin="0,0,8,0"
                             RenderTransformOrigin="0.5,0.5">
                        <Ellipse.RenderTransform>
                            <RotateTransform x:Name="SpinnerRotate"/>
                        </Ellipse.RenderTransform>
                    </Ellipse>
                    <TextBlock Text="Transcribing…" Foreground="#888888" FontSize="11" VerticalAlignment="Center"/>
                </StackPanel>

                <!-- ERROR: short error text, auto-dismissed after 3 s -->
                <TextBlock x:Name="ErrorContent" Foreground="#E5BDDF" FontSize="10"
                           VerticalAlignment="Center" HorizontalAlignment="Center"
                           TextTrimming="CharacterEllipsis"
                           Visibility="Collapsed"/>
            </Grid>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 2: Create Windows/DynamicIslandWindow.xaml.cs**

```csharp
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.ViewModels;

namespace WhisperFlowLocal.Windows;

public partial class DynamicIslandWindow : Window
{
    private readonly DynamicIslandViewModel _vm;
    private readonly DispatcherTimer _waveformTimer;
    private readonly Storyboard _spinnerAnim;
    private Rectangle[] _bars = [];

    public DynamicIslandWindow(DynamicIslandViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        _waveformTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _waveformTimer.Tick += OnWaveformTick;

        // Clone so the resource Storyboard (which may be frozen) is independently controllable
        _spinnerAnim = ((Storyboard)FindResource("SpinnerAnim")).Clone();

        Loaded += (_, _) => _bars = [Bar0, Bar1, Bar2, Bar3, Bar4, Bar5, Bar6, Bar7, Bar8, Bar9];
        vm.PropertyChanged += OnVmPropertyChanged;
        SystemEvents.DisplaySettingsChanged += (_, _) => Dispatcher.Invoke(PositionBottomCenter);

        PositionBottomCenter();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DynamicIslandViewModel.State))
            Dispatcher.Invoke(UpdateVisualState);
        if (e.PropertyName == nameof(DynamicIslandViewModel.ErrorMessage) &&
            _vm.State == RecordingState.Error)
            Dispatcher.Invoke(() => ErrorContent.Text = _vm.ErrorMessage);
    }

    private void UpdateVisualState()
    {
        switch (_vm.State)
        {
            case RecordingState.Idle:
                _waveformTimer.Stop();
                _spinnerAnim.Stop(this);
                HideCapsule();
                break;

            case RecordingState.Recording:
                RecordingContent.Visibility   = Visibility.Visible;
                TranscribingContent.Visibility = Visibility.Collapsed;
                ErrorContent.Visibility        = Visibility.Collapsed;
                ShowCapsule(200);
                _waveformTimer.Start();
                _spinnerAnim.Stop(this);
                break;

            case RecordingState.Transcribing:
            case RecordingState.Inserting:
                RecordingContent.Visibility   = Visibility.Collapsed;
                TranscribingContent.Visibility = Visibility.Visible;
                ErrorContent.Visibility        = Visibility.Collapsed;
                ShowCapsule(160);
                _waveformTimer.Stop();
                _spinnerAnim.Begin(this, true);
                break;

            case RecordingState.Error:
                ErrorContent.Text              = _vm.ErrorMessage;
                RecordingContent.Visibility    = Visibility.Collapsed;
                TranscribingContent.Visibility  = Visibility.Collapsed;
                ErrorContent.Visibility         = Visibility.Visible;
                ShowCapsule(220);
                _waveformTimer.Stop();
                _spinnerAnim.Stop(this);
                ScheduleErrorDismiss();
                break;
        }
    }

    private void ShowCapsule(double targetWidth)
    {
        double fromWidth = Capsule.Visibility == Visibility.Collapsed ? 0 : Capsule.ActualWidth;
        Capsule.Visibility = Visibility.Visible;
        var anim = new DoubleAnimation(fromWidth, targetWidth, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Capsule.BeginAnimation(WidthProperty, anim);
    }

    private void HideCapsule()
    {
        if (Capsule.Visibility == Visibility.Collapsed) return;
        var anim = new DoubleAnimation(Capsule.ActualWidth, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) => Capsule.Visibility = Visibility.Collapsed;
        Capsule.BeginAnimation(WidthProperty, anim);
    }

    private void ScheduleErrorDismiss()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_vm.State == RecordingState.Error)
                HideCapsule();
        };
        timer.Start();
    }

    private void OnWaveformTick(object? sender, EventArgs e)
    {
        foreach (var bar in _bars)
            bar.Height = Random.Shared.Next(4, 17);
    }

    private void PositionBottomCenter()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Left + (wa.Width - Width) / 2;
        Top  = wa.Bottom - Height - 24;
    }
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add Windows/DynamicIslandWindow.xaml Windows/DynamicIslandWindow.xaml.cs
git commit -m "feat(m3a): add DynamicIslandWindow (replaces PillWindow)"
```

---

## Task 5: OnboardingWindow

**Files:**
- Create: `Windows/OnboardingWindow.xaml`
- Create: `Windows/OnboardingWindow.xaml.cs`

4-step wizard shown as `ShowDialog()`. Pink progress bar at top animates 25%→50%→75%→100% across steps. Step 2 calls `TranscriptionService.InitializeAsync(IProgress<string>)` — already accepts optional progress; it is idempotent so App.OnStartup's subsequent call is a no-op for new users. Error in Step 2 shows inline Retry button without crashing. Closing the window before Step 4 completes leaves `HasCompletedOnboarding = false`; App.OnStartup detects this and calls `Shutdown()`.

- [ ] **Step 1: Create Windows/OnboardingWindow.xaml**

```xml
<Window x:Class="WhisperFlowLocal.Windows.OnboardingWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Whisper Flow — Setup"
        Width="560" Height="420"
        Background="#1D1D1D"
        WindowStyle="None"
        ShowInTaskbar="True"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        Topmost="True">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="3"/>   <!-- pink progress bar track -->
            <RowDefinition Height="*"/>   <!-- step content -->
            <RowDefinition Height="Auto"/><!-- footer -->
        </Grid.RowDefinitions>

        <!-- Progress bar track -->
        <Border Grid.Row="0" Background="#242424">
            <Border x:Name="ProgressFill" Background="#E5BDDF"
                    HorizontalAlignment="Left" Width="0"/>
        </Border>

        <!-- ── Step content panels ── -->
        <Grid Grid.Row="1">

            <!-- Step 1: Welcome -->
            <StackPanel x:Name="Step1Panel"
                        VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock Text="&#127897;" FontSize="52" HorizontalAlignment="Center" Margin="0,0,0,16"/>
                <TextBlock Text="Welcome to Whisper Flow"
                           Foreground="#F0F0F0" FontSize="22" FontWeight="Bold"
                           HorizontalAlignment="Center" Margin="0,0,0,10"/>
                <TextBlock Text="Dictate into any app. Text appears at your cursor — no clipboard, no cloud."
                           Foreground="#888888" FontSize="13"
                           TextAlignment="Center" TextWrapping="Wrap" MaxWidth="380"/>
            </StackPanel>

            <!-- Step 2: Model download -->
            <StackPanel x:Name="Step2Panel" Visibility="Collapsed"
                        VerticalAlignment="Center" Margin="48,0">
                <TextBlock Text="Downloading speech model"
                           Foreground="#F0F0F0" FontSize="18" FontWeight="Bold" Margin="0,0,0,8"/>
                <TextBlock Text="~465 MB · one-time download · never sent to the cloud"
                           Foreground="#888888" FontSize="12" Margin="0,0,0,20"/>
                <ProgressBar x:Name="DownloadProgress" IsIndeterminate="True"
                             Height="4" Background="#242424" Foreground="#E5BDDF"
                             BorderThickness="0" Margin="0,0,0,10"/>
                <TextBlock x:Name="DownloadStatus" Foreground="#888888" FontSize="12"/>
                <!-- Inline error (hidden until download fails) -->
                <StackPanel x:Name="DownloadErrorPanel" Visibility="Collapsed" Margin="0,12,0,0">
                    <TextBlock x:Name="DownloadErrorText" Foreground="#E5BDDF"
                               FontSize="12" TextWrapping="Wrap" Margin="0,0,0,10"/>
                    <Button Content="Retry" Click="OnRetryClick"
                            Background="#E5BDDF" Foreground="#1D1D1D"
                            BorderThickness="0" Padding="16,8"
                            HorizontalAlignment="Left" Cursor="Hand"/>
                </StackPanel>
            </StackPanel>

            <!-- Step 3: Hotkey tutorial -->
            <StackPanel x:Name="Step3Panel" Visibility="Collapsed"
                        VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock Text="Your dictation hotkey"
                           Foreground="#F0F0F0" FontSize="18" FontWeight="Bold"
                           HorizontalAlignment="Center" Margin="0,0,0,28"/>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,0,0,28">
                    <Border Background="#242424" BorderBrush="#E5BDDF" BorderThickness="1.5"
                            CornerRadius="6" Padding="18,10">
                        <TextBlock Text="Ctrl" Foreground="#F0F0F0" FontSize="14" FontWeight="Medium"/>
                    </Border>
                    <TextBlock Text="+" Foreground="#666666" FontSize="20"
                               VerticalAlignment="Center" Margin="16,0"/>
                    <Border Background="#242424" BorderBrush="#E5BDDF" BorderThickness="1.5"
                            CornerRadius="6" Padding="18,10">
                        <TextBlock Text="Space" Foreground="#F0F0F0" FontSize="14" FontWeight="Medium"/>
                    </Border>
                </StackPanel>
                <TextBlock Text="Hold Ctrl+Space anywhere to start dictating. Release to transcribe and insert."
                           Foreground="#CCCCCC" FontSize="13"
                           TextAlignment="Center" TextWrapping="Wrap" MaxWidth="400" Margin="0,0,0,10"/>
                <TextBlock Text="Toggle mode available via tray menu (hold once to start, once to stop)."
                           Foreground="#666666" FontSize="11"
                           TextAlignment="Center" TextWrapping="Wrap" MaxWidth="400"/>
            </StackPanel>

            <!-- Step 4: Try it -->
            <StackPanel x:Name="Step4Panel" Visibility="Collapsed"
                        VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock Text="Give it a try"
                           Foreground="#F0F0F0" FontSize="18" FontWeight="Bold"
                           HorizontalAlignment="Center" Margin="0,0,0,16"/>
                <TextBlock Text="Click into any text field on your desktop, then hold Ctrl+Space and say something."
                           Foreground="#CCCCCC" FontSize="13"
                           TextAlignment="Center" TextWrapping="Wrap" MaxWidth="400" Margin="0,0,0,12"/>
                <TextBlock Text="This window stays on top so you can see it while you dictate."
                           Foreground="#666666" FontSize="11"
                           TextAlignment="Center" TextWrapping="Wrap" MaxWidth="400"/>
            </StackPanel>

            <!-- Drag strip (top 36 px of content area, below step panels in z-order) -->
            <Border Height="36" VerticalAlignment="Top" Background="Transparent"
                    MouseLeftButtonDown="OnDragHandle"/>

            <!-- Custom close button (top-right corner) -->
            <Button Content="×" Click="OnCloseClick"
                    HorizontalAlignment="Right" VerticalAlignment="Top"
                    Margin="0,6,14,0"
                    Background="Transparent" Foreground="#666666"
                    BorderThickness="0" FontSize="18" Cursor="Hand"
                    Panel.ZIndex="10"/>
        </Grid>

        <!-- Footer: Back / Next -->
        <Border Grid.Row="2" Padding="40,16,40,24"
                BorderBrush="#242424" BorderThickness="0,1,0,0">
            <Grid>
                <Button x:Name="BackButton" Content="← Back" Click="OnBackClick"
                        HorizontalAlignment="Left"
                        Background="Transparent" Foreground="#888888"
                        BorderBrush="#333333" BorderThickness="1"
                        Padding="16,8" Cursor="Hand"
                        Visibility="Collapsed"/>
                <Button x:Name="NextButton" Content="Get started →" Click="OnNextClick"
                        HorizontalAlignment="Right"
                        Background="#E5BDDF" Foreground="#1D1D1D"
                        BorderThickness="0" Padding="20,8" Cursor="Hand"/>
            </Grid>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 2: Create Windows/OnboardingWindow.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Media.Animation;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Windows;

public partial class OnboardingWindow : Window
{
    private readonly TranscriptionService _transcription;
    private readonly SettingsService _settingsService;
    private int _step = 1;
    private bool _modelReady;

    // 560 px window width: 25 / 50 / 75 / 100 %
    private static readonly double[] ProgressWidths = [140, 280, 420, 560];

    public OnboardingWindow(TranscriptionService transcription, SettingsService settingsService)
    {
        InitializeComponent();
        _transcription  = transcription;
        _settingsService = settingsService;
        ShowStep(1);
    }

    private void ShowStep(int step)
    {
        _step = step;

        Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        Step4Panel.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = step > 1 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Content    = step == 1 ? "Get started →" : step == 4 ? "Done" : "Next →";
        NextButton.IsEnabled  = step != 2 || _modelReady;

        var anim = new DoubleAnimation(ProgressFill.ActualWidth, ProgressWidths[step - 1],
            TimeSpan.FromMilliseconds(300));
        ProgressFill.BeginAnimation(WidthProperty, anim);
    }

    private async void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_step == 4)
        {
            _settingsService.Current.HasCompletedOnboarding = true;
            _settingsService.Save();
            Close();
            return;
        }
        if (_step == 1)
        {
            ShowStep(2);
            await StartModelDownloadAsync();
            return;
        }
        ShowStep(_step + 1);
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_step > 1) ShowStep(_step - 1);
    }

    private async Task StartModelDownloadAsync()
    {
        DownloadProgress.Visibility  = Visibility.Visible;
        DownloadErrorPanel.Visibility = Visibility.Collapsed;
        NextButton.IsEnabled         = false;

        var progress = new Progress<string>(msg =>
            Dispatcher.Invoke(() => DownloadStatus.Text = msg));

        try
        {
            await _transcription.InitializeAsync(progress);
            DownloadProgress.Visibility = Visibility.Collapsed;
            DownloadStatus.Text         = "Model ready ✓";
            _modelReady                 = true;
            NextButton.IsEnabled        = true;
        }
        catch (Exception ex)
        {
            DownloadProgress.Visibility  = Visibility.Collapsed;
            DownloadErrorText.Text       = ex.Message;
            DownloadErrorPanel.Visibility = Visibility.Visible;
        }
    }

    private async void OnRetryClick(object sender, RoutedEventArgs e)
    {
        DownloadErrorPanel.Visibility = Visibility.Collapsed;
        await StartModelDownloadAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnDragHandle(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add Windows/OnboardingWindow.xaml Windows/OnboardingWindow.xaml.cs
git commit -m "feat(m3a): add OnboardingWindow 4-step first-run wizard"
```

---

## Task 6: Wire App.xaml.cs — onboarding gate + swap PillWindow → DynamicIslandWindow

**Files:**
- Modify: `App.xaml.cs`

Two changes from the existing file: (1) onboarding gate inserted after `settingsService.Load()`, (2) `PillViewModel`/`PillWindow` lines replaced with `DynamicIslandViewModel`/`DynamicIslandWindow`. The `_pillWindow` field type changes from `PillWindow?` to `DynamicIslandWindow?`. The tray balloon and `await transcription.InitializeAsync()` remain — for new users this is a no-op (model was loaded in Step 2); for returning users it loads the model normally.

- [ ] **Step 1: Replace App.xaml.cs**

```csharp
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using WhisperFlowLocal.Interop;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;
using WhisperFlowLocal.ViewModels;
using WhisperFlowLocal.Windows;

namespace WhisperFlowLocal;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private IntPtr _hookHandle = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _hookProc;
    private DictationEngine? _engine;
    private DynamicIslandWindow? _pillWindow;
    private Windows.MainWindow? _mainWindow;
    private MainViewModel? _mainVm;
    private InsightsViewModel? _insightsVm;
    private InsightsRepository? _insights;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Settings
        var settingsService = new SettingsService();
        settingsService.Load();

        // Insights DB
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WhisperFlowLocal", "insights.db");
        _insights = new InsightsRepository(dbPath);
        await _insights.InitAsync();

        // Core services
        var focus      = new FocusService();
        var audio      = new AudioCaptureService();
        var modelPath  = Path.Combine(AppContext.BaseDirectory, "Resources", "Models", "ggml-small.en.bin");
        var transcription = new TranscriptionService(modelPath);
        var regex      = new RegexCleanupService();
        var groq       = new GroqCleanupService(new HttpClient(), settingsService);
        var cleanup    = new TieredCleanupService(groq, regex);
        var insertion  = new InsertionService(focus);

        // First-run onboarding (ShowDialog blocks until window closes)
        if (!settingsService.Current.HasCompletedOnboarding)
        {
            var onboarding = new OnboardingWindow(transcription, settingsService);
            onboarding.ShowDialog();
            if (!settingsService.Current.HasCompletedOnboarding)
            {
                // User closed before finishing — exit without starting the app
                Shutdown();
                return;
            }
        }

        // Tray + model load (InitializeAsync is a no-op when model is already loaded from onboarding)
        SetupTray();
        _trayIcon!.ShowBalloonTip(3000, "Whisper Flow", "Loading speech model...", ToolTipIcon.Info);
        await transcription.InitializeAsync();
        _trayIcon.ShowBalloonTip(2000, "Whisper Flow", "Ready. Hold Ctrl+Space to dictate.", ToolTipIcon.Info);

        // Engine
        _engine = new DictationEngine(audio, transcription, cleanup, insertion, focus, _insights);

        // Dynamic Island pill (transparent at idle — capsule is Collapsed)
        var pillVm = new DynamicIslandViewModel();
        pillVm.SyncFrom(_engine);
        _pillWindow = new DynamicIslandWindow(pillVm);
        _pillWindow.Show();

        // ViewModels
        _insightsVm = new InsightsViewModel(_insights);
        var settingsVm = new SettingsViewModel(settingsService);
        _mainVm = new MainViewModel(_insightsVm, settingsVm);

        // Refresh Insights after each dictation (marshal to UI thread)
        _engine.DictationCompleted += () =>
            Dispatcher.BeginInvoke(() => _ = _insightsVm.RefreshAsync());

        // Keyboard hook
        InstallHook();
    }

    private void SetupTray()
    {
        var iconUri    = new Uri("pack://application:,,,/Resources/tray-icon.ico");
        var iconStream = GetResourceStream(iconUri)?.Stream;

        _trayIcon = new NotifyIcon
        {
            Icon    = iconStream != null ? new System.Drawing.Icon(iconStream) : SystemIcons.Application,
            Visible = true,
            Text    = "Whisper Flow Local"
        };

        var menu       = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Toggle Mode") { CheckOnClick = true };
        toggleItem.CheckedChanged += (_, _) =>
        {
            if (_engine != null) _engine.ToggleMode = toggleItem.Checked;
        };
        menu.Items.Add(toggleItem);
        menu.Items.Add("Settings", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Quit",     null, (_, _) => Shutdown());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick     += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new Windows.MainWindow(_mainVm!);
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void InstallHook()
    {
        _hookProc = LowLevelKeyboardHook;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule  = curProcess.MainModule!;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private IntPtr LowLevelKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kb       = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool isSpace  = kb.vkCode == NativeMethods.VK_SPACE;
            bool ctrlDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;

            if (isSpace && ctrlDown)
            {
                int msg = wParam.ToInt32();
                if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                    Dispatcher.BeginInvoke(() => _engine?.OnHotkeyPressed());
                else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                    Dispatcher.BeginInvoke(() => _engine?.OnHotkeyReleased());
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        _insights?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 2: Build and verify** (PillWindow still on disk — no missing-type errors yet)

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```
git add App.xaml.cs
git commit -m "feat(m3a): wire onboarding gate and Dynamic Island in App.xaml.cs"
```

---

## Task 7: Delete PillWindow files and final smoke test

**Files:**
- Delete: `Windows/PillWindow.xaml`
- Delete: `Windows/PillWindow.xaml.cs`
- Delete: `ViewModels/PillViewModel.cs`

- [ ] **Step 1: Git-remove the three deleted files**

```
git rm Windows/PillWindow.xaml Windows/PillWindow.xaml.cs ViewModels/PillViewModel.cs
```

- [ ] **Step 2: Build — confirm nothing references the deleted files**

Run: `dotnet build WhisperFlowLocal.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Run tests**

Run: `dotnet test Tests/WhisperFlowLocal.Tests.csproj`
Expected: All existing tests pass (tests cover services/converters, not UI)

- [ ] **Step 4: Manual smoke test**

1. Delete `%APPDATA%\WhisperFlowLocal\settings.json` to force first-run state
2. Launch the app → OnboardingWindow appears at centre screen with pink progress bar
3. Complete all 4 steps → window closes, tray icon appears, app idles
4. Relaunch → OnboardingWindow does NOT appear (flag persisted)
5. Hold Ctrl+Space → Dynamic Island capsule appears at bottom-centre with pink waveform bars
6. Release → capsule shrinks to Transcribing state with pink spinner + "Transcribing…"
7. Text inserts at cursor → capsule disappears (smooth close animation)
8. Open Settings from tray → MainWindow shows #1D1D1D background, #E5BDDF accents throughout

- [ ] **Step 5: Commit**

```
git commit -m "feat(m3a): delete PillWindow and PillViewModel (replaced by DynamicIslandWindow)"
```

- [ ] **Step 6: Push to remote**

```
git push origin master:main
```
