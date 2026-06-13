using FolderSizeVisualizer.App.ViewModels;
using System.IO;

namespace FolderSizeVisualizer.Core.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public void FolderPathChangeRaisesAnalyzeCanExecuteChanged()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var viewModel = new MainViewModel();
            var raised = false;
            viewModel.AnalyzeCommand.CanExecuteChanged += (_, _) => raised = true;

            viewModel.FolderPath = directory.FullName;

            Assert.True(raised);
            Assert.True(viewModel.AnalyzeCommand.CanExecute(null));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void CopyPathCommandIsAvailable()
    {
        var viewModel = new MainViewModel();

        Assert.True(viewModel.CopyPathCommand.CanExecute(null));
    }
}
