# Folder Size Visualizer

A lightweight Windows desktop app for scanning a folder and visualizing storage usage with an interactive treemap.

## Features

- Paste or browse for a folder path
- Analyze folder size with progress feedback
- Visualize file and folder usage as geometry
- Expand folders from the treemap
- Go back to the parent level from the context menu
- Copy the current folder path
- Switch Chinese and English UI
- Light and dark themes
- Move files to Recycle Bin or delete directly

## Tech Stack

- .NET 8
- WPF
- xUnit

## Project Structure

- `src/FolderSizeVisualizer.App`
  WPF desktop UI
- `src/FolderSizeVisualizer.Core`
  scanning, sorting, layout, settings, file operations
- `tests/FolderSizeVisualizer.Core.Tests`
  unit tests and regression tests

## Run

```powershell
dotnet build FolderSizeVisualizer.sln
dotnet run --project src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj
```

## Test

```powershell
dotnet test FolderSizeVisualizer.sln
```

## Notes

- Local publish output is ignored by Git.
- The app currently targets Windows desktop usage.

