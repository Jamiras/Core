using Jamiras.Components;
using Jamiras.DataModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Wrapper for <see cref="EditorProperties"/> that converts program-friendly data types to UI-bindable data types.
    /// </summary>
    public class EditorResources : IDisposable
    {
        /// <summary>
        /// Container for a <see cref="Brush"/> that will be updated if the related <see cref="EditorProperties"/> value changes.
        /// </summary>
        /// <seealso cref="Jamiras.DataModels.ModelBase" />
        public class BrushResource : ModelBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BrushResource"/> class.
            /// </summary>
            public BrushResource(EditorProperties properties, ModelProperty editorProperty)
            {
                _editorProperties = properties;
                _editorProperty = editorProperty;
            }

            private readonly EditorProperties _editorProperties;
            private readonly ModelProperty _editorProperty;

            private static readonly ModelProperty BrushProperty = ModelProperty.Register(typeof(BrushResource), "Brush", typeof(Brush), new ModelProperty.UnitializedValue(GetBrush));

            /// <summary>
            /// Gets the brush.
            /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorResources"/> class.
        /// </summary>
        /// <param name="properties">The <see cref="EditorProperties"/> to wrap.</param>
        public EditorResources(EditorProperties properties)
        {
            _properties = properties;
            _properties.CustomColorChanged += properties_CustomColorChanged;

            Background = new BrushResource(properties, EditorProperties.BackgroundProperty);
            Foreground = new BrushResource(properties, EditorProperties.ForegroundProperty);
            Selection = new BrushResource(properties, EditorProperties.SelectionProperty);
            LineNumber = new BrushResource(properties, EditorProperties.LineNumberProperty);

            FontName = properties.FontName;
            FontSize = properties.FontSize;

            _customBrushes = new TinyDictionary<int, Brush>();

            var formattedText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontName), 
                FontSize, Brushes.Black, VisualTreeHelper.GetDpi(new Button()).PixelsPerDip);
            CharacterWidth = formattedText.Width;
            CharacterHeight = (int)(formattedText.Height + 0.75);
        }

        void IDisposable.Dispose()
        {
            if (_properties != null)
                _properties.CustomColorChanged -= properties_CustomColorChanged;
        }

        private void properties_CustomColorChanged(object sender, EditorProperties.CustomColorChangedEventArgs e)
        {
            _customBrushes.Remove(e.Id);
        }

        private readonly EditorProperties _properties;
        private readonly TinyDictionary<int, Brush> _customBrushes;

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        public string FontName { get; private set; }

        /// <summary>
        /// Gets the size of the font.
        /// </summary>
        public double FontSize { get; private set; }

        /// <summary>
        /// Gets the width of a character.
        /// </summary>
        public double CharacterWidth { get; private set; }

        /// <summary>
        /// Gets the height of the character.
        /// </summary>
        public int CharacterHeight { get; private set; }

        /// <summary>
        /// Gets the background brush container.
        /// </summary>
        public BrushResource Background { get; private set; }

        /// <summary>
        /// Gets the foreground brush container.
        /// </summary>
        public BrushResource Foreground { get; private set; }

        /// <summary>
        /// Gets the background for selected text brush container.
        /// </summary>
        public BrushResource Selection { get; private set; }

        /// <summary>
        /// Gets the brush container for line numbers.
        /// </summary>
        public BrushResource LineNumber { get; private set; }

        /// <summary>
        /// Gets the brush for a custom syntax type.
        /// </summary>
        /// <param name="id">The unique identifier of the syntax type.</param>
        /// <returns>The brush to use when text is identified as the syntax type.</returns>
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
