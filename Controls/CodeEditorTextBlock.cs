using Jamiras.Components;
using Jamiras.Services;
using Jamiras.ViewModels.CodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Jamiras.Controls
{
    public class CodeEditorTextBlock : TextBlock
    {
        public CodeEditorTextBlock()
        {
            if (_wavyLine == null)
            {
                // <VisualBrush x: Key="WavyBrush" Viewbox="0,0,3,2" ViewboxUnits="Absolute" Viewport="0,1,6,5" ViewportUnits="Absolute" TileMode="Tile" >
                //   <VisualBrush.Visual >
                //     <Path Data="M 0,1 C 1,0 2,2 3,1" Stroke="Red" Opacity=".8" StrokeThickness="0.2" StrokeEndLineCap="Square" StrokeStartLineCap="Square" />
                //   </VisualBrush.Visual >
                // </VisualBrush >
                var brush = new VisualBrush();
                brush.Viewbox = new Rect(0, 0, 3, 2);
                brush.ViewboxUnits = BrushMappingMode.Absolute;
                brush.Viewport = new Rect(0, 1, 6, 5);
                brush.ViewportUnits = BrushMappingMode.Absolute;
                brush.TileMode = TileMode.Tile;
                brush.Visual = new System.Windows.Shapes.Path()
                {
                    Data = Geometry.Parse("M 0,1 C 1,0 2, 2, 3,1"),
                    Stroke = Brushes.Red,
                    Opacity = 0.8,
                    StrokeThickness = 0.2,
                    StrokeEndLineCap = PenLineCap.Square,
                    StrokeStartLineCap = PenLineCap.Square
                };

                // <TextDecoration Location="Underline">
                //   <TextDecoration.Pen>
                //     <Pen Brush="{StaticResource WavyBrush}" Thickness="6" />
                //   </TextDecoration.Pen>
                // </TextDecoration>
                var pen = new Pen(brush, 6);
                _wavyLine = new TextDecoration() { Location = TextDecorationLocation.Underline, Pen = pen };
            }
        }

        private static TextDecoration _wavyLine;

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="TextPieces"/>
        /// </summary>
        public static readonly DependencyProperty TextPiecesProperty = DependencyProperty.Register("TextPieces",
            typeof(IEnumerable<TextPiece>), typeof(CodeEditorTextBlock), new FrameworkPropertyMetadata(OnTextPiecesChanged));

        /// <summary>
        /// Gets or sets the unformatted text.
        /// </summary>
        public IEnumerable<TextPiece> TextPieces
        {
            get { return (IEnumerable<TextPiece>)GetValue(TextPiecesProperty); }
            set { SetValue(TextPiecesProperty, value); }
        }

        private static void OnTextPiecesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = (CodeEditorTextBlock)sender;
            vm.BuildText();
        }

        private void BuildText()
        {
            Inlines.Clear();

            var pieces = TextPieces;
            if (pieces != null)
            {
                foreach (var piece in pieces)
                {
                    var inline = new Run(piece.Text);
                    inline.Foreground = piece.Foreground;

                    if (!String.IsNullOrEmpty(piece.ToolTip))
                        inline.ToolTip = piece.ToolTip;

                    if (piece.IsError)
                        inline.TextDecorations.Add(_wavyLine);

                    Inlines.Add(inline);
                }
            }
        }
    }
}
