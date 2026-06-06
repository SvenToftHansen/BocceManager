# BocceManager Implementation Rules

## WinForms Layout Rules

1. SplitContainer safety:
- Never rely on a fixed SplitterDistance by itself.
- Always clamp SplitterDistance to a valid range based on current Width, Panel1MinSize, and Panel2MinSize.
- Apply clamped SplitterDistance on size changes and after handle creation.

2. Min-size realism:
- Panel1MinSize + Panel2MinSize must fit realistic app content widths.
- Avoid setting strict panel minimums that exceed typical user window sizes.

3. Defensive startup behavior:
- Initial layout must not throw if the window starts small or has not fully measured.
- Defer layout-sensitive sizing with BeginInvoke where needed.

## Build Workflow Rules

1. Before building, stop any running BocceManager process.
2. Build with dotnet build.
3. If build succeeds, run with dotnet run.

## Documentation Rule

1. After any code change, update docs/AI_HANDOFF.md before ending the session.
