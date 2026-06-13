using FolderSizeVisualizer.Core.Models;
using Microsoft.VisualBasic.FileIO;

namespace FolderSizeVisualizer.Core.Services;

public sealed class FileOperationService
{
    public void Delete(FileNode node, DeleteMode mode)
    {
        if (mode == DeleteMode.RecycleBin)
        {
            if (node.Kind == FileNodeKind.Directory)
                FileSystem.DeleteDirectory(node.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            else
                FileSystem.DeleteFile(node.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            return;
        }

        if (node.Kind == FileNodeKind.Directory)
            Directory.Delete(node.FullPath, recursive: true);
        else
            File.Delete(node.FullPath);
    }
}
