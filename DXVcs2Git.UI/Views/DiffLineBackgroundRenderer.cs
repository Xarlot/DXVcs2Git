using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace DXVcs2Git.UI.Views {
    public enum DiffLineStyle {
        Header,
        Added,
        Deleted,
        Context,
    }

    public class DiffLineBackgroundRenderer : IBackgroundRenderer {
        static Pen pen;

        static SolidColorBrush removedBackground;
        static SolidColorBrush addedBackground;
        static SolidColorBrush headerBackground;

        TextEditor host;

        static DiffLineBackgroundRenderer() {
            removedBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xdd, 0xdd));
            removedBackground.Freeze();
            addedBackground = new SolidColorBrush(Color.FromRgb(0xdd, 0xff, 0xdd));
            addedBackground.Freeze();
            headerBackground = new SolidColorBrush(Color.FromRgb(0xf8, 0xf8, 0xff));
            headerBackground.Freeze();

            var blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            blackBrush.Freeze();
            pen = new Pen(blackBrush, 0.0);
        }
        public DiffLineBackgroundRenderer(TextEditor host) {
            this.host = host;
        }
        public KnownLayer Layer {
            get { return KnownLayer.Background; }
        }
        public void Draw(TextView textView, DrawingContext drawingContext) {
            foreach (var v in textView.VisualLines) {
                var rc = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, v, 0, 1000).First();
                int offset = v.FirstDocumentLine.Offset;
                int length = v.FirstDocumentLine.Length;
                var text = this.host.Text.Substring(offset, length);

                Brush brush = null;
                if (text.StartsWith("+"))
                    brush = addedBackground;
                else if (text.StartsWith("-"))
                    brush = removedBackground;
                else if (text.StartsWith("@"))
                    brush = headerBackground;

                if (brush == null)
                    continue;

                drawingContext.DrawRectangle(brush, pen,
                    new Rect(0, rc.Top, textView.ActualWidth, rc.Height));
            }
        }
    }
}
