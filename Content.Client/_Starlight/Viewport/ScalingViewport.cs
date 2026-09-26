// ReSharper disable once CheckNamespace
namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            InvalidateViewport();

        base.Dispose(disposing);
    }
}
