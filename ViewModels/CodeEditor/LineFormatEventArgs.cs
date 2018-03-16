using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.ViewModels.CodeEditor
{
    public class LineFormatEventArgs : LineEventArgs
    {
        public LineFormatEventArgs(LineViewModel line)
            : base(line)
        {
            _ranges = new List<ColorRange>();
            _ranges.Add(new ColorRange(1, Text.Length, 0, null));

            _errorRanges = new List<ColorRange>();
            _errorRanges.Add(new ColorRange(1, Text.Length, 0, null));
        }

        private List<ColorRange> _ranges;
        private List<ColorRange> _errorRanges;

        public void SetColor(int startColumn, int length, int color, string toolTip = null)
        {
            UpdateRange(_ranges, startColumn, length, color, toolTip);
        }

        public void SetError(int startColumn, int length, string toolTip = null)
        {
            UpdateRange(_errorRanges, startColumn, length, 1, toolTip);
        }

        private static void UpdateRange(List<ColorRange> ranges, int startColumn, int length, int color, string toolTip)
        {
            var lineLength = ranges[ranges.Count - 1].EndColumn;
            if (startColumn > lineLength)
                return;

            var endColumn = startColumn + length - 1;
            if (endColumn > lineLength)
            {
                endColumn = lineLength;
                length = endColumn - startColumn + 1;
            }

            // find the range containing startColumn
            int i = 0;
            if (startColumn > 1)
            {
                i = ranges.Count - 1;
                while (ranges[i].StartColumn > endColumn)
                    --i;
            }

            var range = ranges[i];

            // if the new range includes part of the previous range, separate them.
            int extra = startColumn - range.StartColumn;
            while (extra < 0)
            {
                if (ranges[i].EndColumn > endColumn)
                {
                    range.StartColumn = endColumn + 1;
                    ranges[i] = range;
                }
                else
                {
                    ranges.RemoveAt(i);
                }

                range = ranges[--i];
                range.Length = endColumn - range.StartColumn + 1;
                extra = startColumn - range.StartColumn;
            }

            // if there's extra space to the left, shorten the existing range and add a new one starting at startColumn
            if (extra > 0)
            {
                var newRange = new ColorRange(startColumn, range.Length - extra, range.Color, range.ToolTip);

                range.Length = extra;
                ranges[i] = range;

                i++;
                ranges.Insert(i, newRange);
                range = newRange;
            }

            // if there's extra space to the right, shorten the existing range and add a new one starting at endColumn + 1
            if (range.Length > length)
            {
                var newRange = new ColorRange(range.StartColumn + length, range.Length - length, range.Color, range.ToolTip);
                ranges.Insert(i + 1, newRange);

                range.Length = length;
            }

            // update the color of the range
            range.Color = color;
            range.ToolTip = toolTip;
            ranges[i] = range;
        }

        [DebuggerDisplay("{StartColumn}-{EndColumn} => {Color}")]
        private struct ColorRange
        {
            public ColorRange(int startColumn, int length, int color, string toolTip)
            {
                StartColumn = startColumn;
                Length = length;
                Color = color;
                ToolTip = toolTip;
            }

            public int StartColumn { get; set; }
            public int Length { get; set; }
            public int Color { get; set; }
            public string ToolTip { get; set; }

            public int EndColumn
            {
                get { return StartColumn + Length - 1; }
            }
        }

        private void MergeErrorRanges()
        {
            int rangeIndex = 0;
            var range = _ranges[0];
            foreach (var error in _errorRanges)
            {
                if (error.Color == 0)
                    continue;

                while (range.EndColumn < error.StartColumn)
                    range = _ranges[++rangeIndex];

                if (range.StartColumn < error.StartColumn)
                {
                    var newRange = new ColorRange(error.StartColumn, range.EndColumn - error.StartColumn + 1, range.Color, range.ToolTip);

                    range.Length = error.StartColumn - range.StartColumn;
                    _ranges[rangeIndex] = range;

                    _ranges.Insert(++rangeIndex, newRange);
                    range = newRange;
                }

                if (range.EndColumn > error.EndColumn)
                {
                    var newRange = new ColorRange(error.EndColumn + 1, range.EndColumn - error.EndColumn, range.Color, range.ToolTip);
                    range.Length = newRange.StartColumn - range.StartColumn;
                    _ranges[rangeIndex] = range;

                    _ranges.Insert(++rangeIndex, newRange);
                    range = newRange;
                }
            }
        }

        internal IEnumerable<TextPiece> BuildTextPieces()
        {
            if (_errorRanges.Count > 1)
                MergeErrorRanges();

            var newPieces = new TextPiece[_ranges.Count];
            Debug.Assert(newPieces.Length > 0);
            for (int i = 0; i < _ranges.Count; i++)
            {
                var range = _ranges[i];
                newPieces[i] = new TextPiece
                {
                    Text = Text.Substring(range.StartColumn - 1, range.Length),
                    Foreground = (range.Color == 0) ? Line.Resources.Foreground.Brush : Line.Resources.GetCustomBrush(range.Color),
                    ToolTip = range.ToolTip
                };
            }

            int rangeIndex = 0;
            foreach (var error in _errorRanges)
            {
                if (error.Color == 0)
                    continue;

                while (_ranges[rangeIndex].StartColumn != error.StartColumn)
                    ++rangeIndex;

                do
                {
                    newPieces[rangeIndex].IsError = true;
                    newPieces[rangeIndex].ToolTip = error.ToolTip;

                    if (_ranges[rangeIndex].EndColumn == error.EndColumn)
                        break;

                    rangeIndex++;
                } while (true);
            }

            // if anything changed, use the updated values
            return newPieces;
        }
    }
}
