namespace PDFBlender.App;

internal enum SessionChangeKind
{
    All,
    Preferences,
    InputFiles,
    BlendSelection,
    Configurations,
    ProcessingState
}

internal sealed class SessionChangedEventArgs(SessionChangeKind kind) : EventArgs
{
    public SessionChangeKind Kind { get; } = kind;
}
