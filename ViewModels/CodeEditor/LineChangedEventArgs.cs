using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.ViewModels.CodeEditor
{
    public class LineChangedEventArgs : EventArgs
    {
        public LineChangedEventArgs(LineViewModel line)
        {
            Line = line;
            NewText = line.Text;

            _ranges = new List<ColorRange>();
            _ranges.Add(new ColorRange(1, NewText.Length, 0));
        }

        public LineViewModel Line { get; private set; }

        public string NewText { get; private set; }

        public void SetColor(int startColumn, int length, int color)
        {
            var endColumn = startColumn + length - 1;

            // find the range containing startColumn
            int i = (startColumn > 1) ? _ranges.Count - 1 : 0;
            while (_ranges[i].StartColumn > endColumn)
                --i;

            var range = _ranges[i];

            // if there's extra space to the left, shorten the existing range and add a new one starting at startColumn
            int extra = startColumn - range.StartColumn;
            if (extra > 0)
            {
                var newRange = new ColorRange(startColumn, range.Length - extra, range.Color);

                range.Length = extra;
                _ranges[i] = range;

                i++;
                _ranges.Insert(i, newRange);
                range = newRange;
            }

            // if there's extra space to the right, shorten the existing range and add a new one starting at endColumn + 1
            if (range.Length > length)
            {
                var newRange = new ColorRange(range.StartColumn + length, range.Length - length, range.Color);
                _ranges.Insert(i + 1, newRange);

                range.Length = length;
            }

            // update the color of the range
            range.Color = color;
            _ranges[i] = range;
        }

        private List<ColorRange> _ranges;

        [DebuggerDisplay("{StartColumn}-{EndColumn} => {Color}")]
        private struct ColorRange
        {
            public ColorRange(int startColumn, int length, int color)
            {
                StartColumn = startColumn;
                Length = length;
                Color = color;
            }

            public int StartColumn { get; set; }
            public int Length { get; set; }
            public int Color { get; set; }

            public int EndColumn
            {
                get { return StartColumn + Length - 1; }
            }
        }

        internal IEnumerable<TextPiece> ApplyColors(IEnumerable<TextPiece> pieces)
        {
            // prepare the colors first
            var newPieces = new TextPiece[_ranges.Count];
            for (int i = 0; i < _ranges.Count; i++)
            {
                var range = _ranges[i];
                newPieces[i] = new TextPiece
                {
                    Foreground = (range.Color == 0) ? Line.Resources.Foreground.Brush : Line.Resources.GetCustomBrush(range.Color)
                };
            }

            // if the existing items don't have the same colors or lengths, we know we need to use the new pieces
            bool same = true;
            int index = 0;
            var enumerator = pieces.GetEnumerator();
            while (index < _ranges.Count)
            {
                if (!enumerator.MoveNext())
                {
                    same = false;
                    break;
                }

                var range = _ranges[index];
                if (!ReferenceEquals(enumerator.Current.Foreground, newPieces[index].Foreground))
                {
                    same = false;
                    break;
                }

                if (enumerator.Current.Text.Length != range.Length)
                {
                    same = false;
                    break;
                }

                index++;
            }

            // if existing items match, but previously more items existed, it's not a match
            if (same && enumerator.MoveNext())
                same = false;

            // now process the string contents
            index = 0;
            enumerator = pieces.GetEnumerator();
            while (index < _ranges.Count)
            {
                var range = _ranges[index];
                var text = NewText.Substring(range.StartColumn - 1, range.Length);
                newPieces[index++].Text = text;
                if (!enumerator.MoveNext() || enumerator.Current.Text != text)
                    same = false;
            }

            // if anything changed, use the updated values
            return (same ? pieces : newPieces);
        }
    }
}
