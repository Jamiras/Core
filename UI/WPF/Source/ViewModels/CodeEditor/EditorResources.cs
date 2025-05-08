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

        /// <summary>
        /// Gets the number of pixels requires to display the string using the local
        /// <see cref="FontName"/>/<see cref="FontSize"/>.
        /// </summary>
        /// <param name="str">String to measure</param>
        /// <param name="index">Indext of first character of string to measure</param>
        /// <param name="count">Number of characters of string to measure</param>
        /// <returns>Pixels required for string</returns>
        public double GetPixelWidth(string str, int index, int count)
        {
            if (CharacterWidth > 0)
            {
                bool isOnlyAscii = true;
                var stop = index + count;
                for (int i = index; i < stop; i++)
                {
                    if (!Char.IsAscii(str[i]))
                    {
                        isOnlyAscii = false;
                        break;
                    }
                }
                if (isOnlyAscii)
                    return count * CharacterWidth;
            }

            if (index > 0 || count < str.Length)
                str = str.Substring(index, count);

            var textBlock = new TextBlock
            {
                Text = str,
                FontFamily = new FontFamily(FontName),
                FontSize = FontSize,
            };
            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            textBlock.Arrange(new Rect(textBlock.DesiredSize));

            return textBlock.ActualWidth;
        }

        /// <summary>
        /// Gets the index of <paramref name="text"/> nearest the specified pixel.
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="pixelOffset">Offset to map</param>
        /// <returns>Index of nearest "between characters"</returns>
        /// <remarks>
        /// If <paramref name="pixelOffset"/> is within the right 1/4 of a character,
        /// the next index will be returned. Otherwise, the index of the character
        /// containing the offset will be returned.
        /// </remarks>
        public int GetNearestCharacterIndex(string text, double pixelOffset)
        {
            // if clicking in the right fourth of a character, put the cursor after the character
            var characterWidth = CharacterWidth;
            var adjustedPixelOffset = pixelOffset + characterWidth / 4;

            // convert the editor relative point to a column index
            int column = (int)(adjustedPixelOffset / characterWidth);

            if (column > text.Length)
                column = text.Length;

            // if the string is only ASCII characters, return the calculated column (column indices are 1-based)
            bool isOnlyAscii = true;
            for (int i = 0; i < column; i++)
            {
                if (!Char.IsAscii(text[i]))
                {
                    isOnlyAscii = false;
                    break;
                }
            }
            if (isOnlyAscii)
                return column + 1;

            // string contains non-ASCII characters. Find the column before and after
            // the target pixel
            var textBlock = new TextBlock
            {
                Text = text.Substring(0, column),
                FontFamily = new FontFamily(FontName),
                FontSize = FontSize,
            };
            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            textBlock.Arrange(new Rect(textBlock.DesiredSize));

            var width = textBlock.ActualWidth;
            do
            {
                --column;
                textBlock.Text = text.Substring(0, column);
                textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                textBlock.Arrange(new Rect(textBlock.DesiredSize));

                var newWidth = textBlock.ActualWidth;
                if (newWidth < pixelOffset)
                {
                    // if clicking in the right fourth of a character, put the cursor after the character
                    var charWidth = width - newWidth;
                    if (width - pixelOffset < charWidth / 4)
                        ++column;
                    return column + 1;
                }
                width = newWidth;
            } while (true);
        }
    }
}
