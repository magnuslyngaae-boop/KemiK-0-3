using System.Windows;
using System.Windows.Media;

namespace KemiK_0_3
{
    internal class VisualHost : FrameworkElement
    {
        private readonly VisualCollection _children;
        public VisualHost()
        {
            _children = new VisualCollection(this);
        }
        public void AddVisual(DrawingVisual visual)
        {
            _children.Add(visual);
        }
        protected override int VisualChildrenCount => _children.Count;
        protected override Visual GetVisualChild(int index) => _children[index];
    }
}
