using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum ScreenId { Title, Home, Quest, Merge, Result, Growth, Settings }
    public sealed class ScreenContext
    {
        public UIFlowController Flow { get; }
        public GameStateManager Game => Flow.Game;
        public UIWidgets Widgets { get; }
        public ScreenContext(UIFlowController flow, UIWidgets widgets) { Flow = flow; Widgets = widgets; }
    }
}
