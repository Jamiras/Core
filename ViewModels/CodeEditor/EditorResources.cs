using Jamiras.Components;
using Jamiras.DataModels;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    public class EditorResources
    {
        public class BrushResource : ModelBase
        {
            public BrushResource(EditorProperties properties, ModelProperty editorProperty)
            {
                _editorProperties = properties;
                _editorProperty = editorProperty;
            }

            private readonly EditorProperties _editorProperties;
            private readonly ModelProperty _editorProperty;

            private static readonly ModelProperty BrushProperty = ModelProperty.Register(typeof(BrushResource), "Brush", typeof(Brush), new ModelProperty.UnitializedValue(GetBrush));
            public Brush Brush
            {
                get { return (Brush)GetValue(BrushProperty); }
            }

            private static object GetBrush(ModelBase model)
            {
                var resource = (BrushResource)model;
                var color = (Color)resource._editorProperties.GetValue(resource._editorProperty);

                resource._editorProperties.AddPropertyChangedHandler(resource._editorProperty, resource.OnColorChanged);

                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }

            private void OnColorChanged(object sender, ModelPropertyChangedEventArgs e)
            {
                var brush = new SolidColorBrush((Color)e.NewValue);
                brush.Freeze();
                SetValue(BrushProperty, brush);
            }
        }

        public EditorResources(EditorProperties properties)
        {
            _properties = properties;

            Background = new BrushResource(properties, EditorProperties.BackgroundProperty);
            Foreground = new BrushResource(properties, EditorProperties.ForegroundProperty);
            Selection = new BrushResource(properties, EditorProperties.SelectionProperty);
            LineNumber = new BrushResource(properties, EditorProperties.LineNumberProperty);

            FontName = properties.FontName;
            FontSize = properties.FontSize;

            _customBrushes = new TinyDictionary<int, Brush>();

            var formattedText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontName), FontSize, Brushes.Black);
            CharacterWidth = formattedText.Width;
            CharacterHeight = (int)(formattedText.Height + 0.75);
        }

        private readonly EditorProperties _properties;
        private readonly TinyDictionary<int, Brush> _customBrushes;

        public string FontName { get; private set; }
        public double FontSize { get; private set; }

        public double CharacterWidth { get; private set; }
        public int CharacterHeight { get; private set; }

        public BrushResource Background { get; private set; }
        public BrushResource Foreground { get; private set; }
        public BrushResource Selection { get; private set; }
        public BrushResource LineNumber { get; private set; }

        public Brush GetCustomBrush(int id)
        {
            Brush brush;
            if (_customBrushes.TryGetValue(id, out brush))
                return brush;

            Color color = _properties.GetCustomColor(id);

            brush = new SolidColorBrush(color);
            brush.Freeze();
            _customBrushes[id] = brush;

            return brush;
        }
    }
}
