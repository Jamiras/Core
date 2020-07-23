using Jamiras.Components;
using Jamiras.DataModels;
using System;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Provides a series of properties that define the appearance of a <see cref="CodeEditor"/>.
    /// </summary>
    /// <seealso cref="Jamiras.DataModels.ModelBase" />
    public class EditorProperties : ModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorProperties"/> class.
        /// </summary>
        public EditorProperties()
        {
            _customColors = new TinyDictionary<int, Color>();
        }

        private readonly TinyDictionary<int, Color> _customColors;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="FontName"/>.
        /// </summary>
        public static readonly ModelProperty FontNameProperty = ModelProperty.Register(typeof(EditorProperties), "FontName", typeof(string), "Consolas");

        /// <summary>
        /// Gets or sets the name of the font.
        /// </summary>
        public string FontName
        {
            get { return (string)GetValue(FontNameProperty); }
            set { SetValue(FontNameProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="FontSize"/>.
        /// </summary>
        public static readonly ModelProperty FontSizeProperty = ModelProperty.Register(typeof(EditorProperties), "FontSize", typeof(double), 13.0);

        /// <summary>
        /// Gets or sets the size of the font.
        /// </summary>
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Background"/>.
        /// </summary>
        public static readonly ModelProperty BackgroundProperty = ModelProperty.Register(typeof(EditorProperties), "Background", typeof(Color), Colors.White);

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        public Color Background
        {
            get { return (Color)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Foreground"/>.
        /// </summary>
        public static readonly ModelProperty ForegroundProperty = ModelProperty.Register(typeof(EditorProperties), "Foreground", typeof(Color), Colors.Black);

        /// <summary>
        /// Gets or sets the foreground color.
        /// </summary>
        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Selection"/>.
        /// </summary>
        public static readonly ModelProperty SelectionProperty = ModelProperty.Register(typeof(EditorProperties), "Selection", typeof(Color), Colors.LightGray);

        /// <summary>
        /// Gets or sets the background color for selected text.
        /// </summary>
        public Color Selection
        {
            get { return (Color)GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="LineNumber"/>.
        /// </summary>
        public static readonly ModelProperty LineNumberProperty = ModelProperty.Register(typeof(EditorProperties), "LineNumber", typeof(Color), Colors.LightGray);

        /// <summary>
        /// Gets or sets the color to use for line numbers.
        /// </summary>
        public Color LineNumber
        {
            get { return (Color)GetValue(LineNumberProperty); }
            set { SetValue(LineNumberProperty, value); }
        }

        /// <summary>
        /// Provides a color for a custom syntax type.
        /// </summary>
        /// <param name="id">The unique identifier of the syntax type.</param>
        /// <param name="color">The color to use when text is identified as the syntax type.</param>
        public void SetCustomColor(int id, Color color)
        {
            _customColors[id] = color;
            OnCustomColorChanged(new CustomColorChangedEventArgs(id, color));
        }

        /// <summary>
        /// Event args for the <see cref="CustomColorChanged"/> event.
        /// </summary>
        public class CustomColorChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Constructs a new <see cref="CustomColorChangedEventArgs"/>
            /// </summary>
            /// <param name="id">The unique identifier of the syntax type.</param>
            /// <param name="color">The new color to use when text is identified as the syntax type.</param>
            public CustomColorChangedEventArgs(int id, Color color)
            {
                Id = id;
                Color = color;
            }

            /// <summary>
            /// Gets the unique identifier of the syntax type whose color changed.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// Gets the new color to use when text is identified as the syntax type.
            /// </summary>
            public Color Color { get; private set; }
        }

        private void OnCustomColorChanged(CustomColorChangedEventArgs e)
        {
            if (CustomColorChanged != null)
                CustomColorChanged(this, e);
        }

        /// <summary>
        /// Raised when a custom color changes.
        /// </summary>
        public event EventHandler<CustomColorChangedEventArgs> CustomColorChanged;

        /// <summary>
        /// Get the color registered for a custom syntax type.
        /// </summary>
        /// <param name="id">The unique identifier of the syntax type.</param>
        /// <returns>The color to use when text is identified as the syntax type.</returns>
        public Color GetCustomColor(int id)
        {
            Color color;
            if (!_customColors.TryGetValue(id, out color))
                color = Colors.Orange;
            return color;
        }
    }
}
