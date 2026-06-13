# Folder Size Visualizer Design

## Goal

Build a lightweight Windows desktop app that scans a selected folder and shows file and folder storage usage as an interactive geometric visualization. The app prioritizes quick understanding, safe file cleanup, and minimal secondary pages.

## Platform

- Target OS: Windows
- Technology: .NET 8 + WPF
- Packaging goal: lightweight desktop app with a polished native Windows experience
- Storage: local JSON settings, no database

## Main UI

The main screen uses a full-screen visualization with floating controls.

Core controls:

- Folder path input
- Folder picker button
- Analyze button
- Sort/group selector
- Theme toggle
- Settings entry
- Breadcrumb navigation for expanded folders

The user can select a folder with the picker or paste a folder path directly into the path input.

## Scan Feedback

During analysis, the UI shows:

- A progress bar
- A rolling fade-style step feed
- The currently processed path or operation

Scanning must keep the UI responsive.

## Scan Strategy

The app uses a hybrid scan strategy:

1. Read the selected folder's first-level files and folders.
2. Render the first-level result quickly.
3. Continue recursive size calculation in the background.
4. Update folder sizes and visualization areas as deeper results become available.

If the user switches folders while scanning, the app cancels the old scan and starts a new one.

## Visualization

Default visualization: rectangular treemap.

Reasons:

- Area maps clearly to storage usage.
- It handles many files better than a pie chart.
- It supports nested folder exploration cleanly.

Optional secondary view: ring chart, if it does not increase scope too much.

Each visual region represents one file or folder in the current level. Hovering a region shows:

- Name
- Size
- Type
- Full path
- Scan status

When hovering over a folder and staying briefly, the UI shows an expand action. Expanding switches the treemap to that folder's contents. Breadcrumbs allow returning to parent folders.

## Sorting And Grouping

The current level can be reorganized without rescanning.

Supported modes:

- Size
- File type
- Name
- Modified time
- Files first
- Folders first

## File Operations

Right-clicking a visual region opens a context menu.

Supported actions:

- Open location
- Expand folder
- Delete

Delete behavior:

- Default: move to Recycle Bin
- Optional setting: direct delete
- Recycle Bin mode requires one confirmation
- Direct delete mode requires one stronger warning confirmation

After a delete operation, the app updates the in-memory tree and recomputes the current visualization.

## Settings

Settings are available from the main screen and should not feel like a separate workflow.

Settings:

- Language: Chinese, English
- Theme: light, dark
- Delete behavior: Recycle Bin, direct delete
- Main background image
- Visualization transparency
- Background blur strength

The app copies the selected background image into the app configuration directory. It does not modify the source image.

## Error Handling

Expected cases:

- Invalid path: show inline path input error
- Path is not a folder: show inline path input error
- Permission denied: skip inaccessible items and show a partial-access warning
- File in use: show the failure reason and continue
- Scan cancelled: stop updating old results
- Large folder: keep UI responsive and continue showing progress

Hidden and system files are shown by default, with visual labeling where practical.

## Acceptance Criteria

- User can choose or paste a folder path.
- Analysis shows a progress bar and rolling fade-style step feed.
- First-level folder contents render quickly.
- The treemap accurately reflects size proportions.
- Deep folder sizes continue updating in the background.
- User can hover for details.
- User can expand folders and return to parent folders.
- Sorting/grouping modes work on the current level.
- Right-click delete supports Recycle Bin by default.
- Settings can switch language, theme, delete mode, and background image.
- Large folder scans do not freeze the UI.
- Errors are visible and do not crash the app.

## Test Plan

- Unit test folder size aggregation.
- Unit test current-level sorting and grouping.
- Unit test tree navigation and expansion state.
- Unit test settings save/load.
- Integration test scan cancellation.
- Integration test inaccessible files/folders.
- Manual test Recycle Bin deletion.
- Manual test direct delete confirmation flow.
- Manual test light/dark theme.
- Manual test Chinese/English language switching.

## Out Of Scope For First Version

- Full disk scanning
- Duplicate file detection
- Cloud sync
- Database storage
- Plugin system
- File preview
- Batch cleanup automation
