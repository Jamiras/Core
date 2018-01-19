using Jamiras.Components;
using Jamiras.DataModels;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    public class EditorProperties : ModelBase
    {
        public EditorProperties()
        {
            _customColors = new TinyDictionary<int, Color>();
        }

        private readonly TinyDictionary<int, Color> _customColors;

        public static readonly ModelProperty FontNameProperty = ModelProperty.Register(typeof(EditorProperties), "FontName", typeof(string), "Consolas");
        public string FontName
        {
            get { return (string)GetValue(FontNameProperty); }
            set { SetValue(FontNameProperty, value); }
        }

        public static readonly ModelProperty FontSizeProperty = ModelProperty.Register(typeof(EditorProperties), "FontSize", typeof(double), 13.0);
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly ModelProperty BackgroundProperty = ModelProperty.Register(typeof(EditorProperties), "Background", typeof(Color), Colors.White);
        public Color Background
        {
            get { return (Color)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public static readonly ModelProperty ForegroundProperty = ModelProperty.Register(typeof(EditorProperties), "Foreground", typeof(Color), Colors.Black);
        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly ModelProperty LineNumberProperty = ModelProperty.Register(typeof(EditorProperties), "LineNumber", typeof(Color), Colors.LightGray);
        public Color LineNumber
        {
            get { return (Color)GetValue(LineNumberProperty); }
            set { SetValue(LineNumberProperty, value); }
        }

        public void SetCustomColor(int id, Color color)
        {
            _customColors[id] = color;
        }

        public Color GetCustomColor(int id)
        {
            Color color;
            _customColors.TryGetValue(id, out color);
            return color;
        }
    }
}
