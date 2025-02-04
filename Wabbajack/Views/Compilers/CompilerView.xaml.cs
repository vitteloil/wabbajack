﻿using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using ReactiveUI;
using System.Windows;
using Wabbajack.Common;

namespace Wabbajack
{
    /// <summary>
    /// Interaction logic for CompilerView.xaml
    /// </summary>
    public partial class CompilerView : ReactiveUserControl<CompilerVM>
    {
        public CompilerView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                // Bind percent completed chanes
                this.WhenAny(x => x.ViewModel.PercentCompleted)
                    .Select(f => (double)f)
                    .BindToStrict(this, x => x.HeatedBackground.PercentCompleted)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.PercentCompleted)
                    .Select(f => (double)f)
                    .BindToStrict(this, x => x.ModlistDetailsHeatBorder.Opacity)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.PercentCompleted)
                    .Select(f => (double)f)
                    .BindToStrict(this, x => x.TopProgressBar.ProgressPercent)
                    .DisposeWith(dispose);

                // Bind detail image display
                this.WhenAny(x => x.ViewModel.CurrentModlistSettings.ModListName)
                    .BindToStrict(this, x => x.DetailImage.Title)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.CurrentModlistSettings.AuthorText)
                    .BindToStrict(this, x => x.DetailImage.Author)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.CurrentModlistSettings.Description)
                    .BindToStrict(this, x => x.DetailImage.Description)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.Image)
                    .BindToStrict(this, x => x.DetailImage.Image)
                    .DisposeWith(dispose);

                // Top Progress Bar
                this.WhenAny(x => x.ViewModel.CurrentModlistSettings.ModListName)
                    .BindToStrict(this, x => x.TopProgressBar.Title)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.ProgressTitle)
                    .BindToStrict(this, x => x.TopProgressBar.StatePrefixTitle)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.BackCommand)
                    .BindToStrict(this, x => x.BackButton.Command)
                    .DisposeWith(dispose);

                // Settings Panel
                this.WhenAny(x => x.ViewModel.Compiling)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.SettingsScrollViewer.IsEnabled)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, x => x.CurrentModlistSettings.ModListName, x => x.ModListNameSetting.Text)
                    .DisposeWith(dispose);
                this.BindStrict(ViewModel, x => x.CurrentModlistSettings.VersionText, x => x.VersionSetting.Text)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, x => x.CurrentModlistSettings.AuthorText, x => x.AuthorNameSetting.Text)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, x => x.CurrentModlistSettings.Description, x => x.DescriptionSetting.Text)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.CurrentModlistSettings.ImagePath)
                    .BindToStrict(this, x => x.ImageFilePicker.PickerVM)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, x => x.CurrentModlistSettings.Website, x => x.WebsiteSetting.Text)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, x => x.CurrentModlistSettings.Readme, x => x.ReadmeSetting.Text)
                    .DisposeWith(dispose);

                // Bottom Compiler Settings
                this.WhenAny(x => x.ViewModel.StartedCompilation)
                    .Select(started => started ? Visibility.Hidden : Visibility.Visible)
                    .BindToStrict(this, x => x.BottomCompilerSettingsGrid.Visibility)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.Compiler)
                    .BindToStrict(this, x => x.CustomCompilerSettingsPresenter.Content)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.BeginCommand)
                    .BindToStrict(this, x => x.BeginButton.Command)
                    .DisposeWith(dispose);

                // Mid-compilation panel
                this.WhenAny(x => x.ViewModel.StartedCompilation)
                    .Select(started => started ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.MidCompilationGrid.Visibility)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.PercentCompleted)
                    .Select(f => (double)f)
                    .BindToStrict(this, x => x.LogView.ProgressPercent)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.PercentCompleted)
                    .BindToStrict(this, x => x.CpuView.ProgressPercent)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.MWVM.Settings)
                    .BindToStrict(this, x => x.CpuView.SettingsHook)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.ActiveGlobalUserIntervention)
                    .Select(x => x == null ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.CpuView.Visibility)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.ActiveGlobalUserIntervention)
                    .Select(x => x != null ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.UserInterventionsControl.Visibility)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.Completed)
                    .Select(x => x != null ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.CompilationComplete.Visibility)
                    .DisposeWith(dispose);
            });
        }
    }
}
