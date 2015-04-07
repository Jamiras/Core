using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jamiras.Controls
{
    public class TextPattern
    {
        public static readonly DependencyProperty PatternProperty =
            DependencyProperty.RegisterAttached("Pattern", typeof(string), typeof(TextPattern),
                new FrameworkPropertyMetadata(OnPatternChanged));

        public static string GetPattern(TextBox target)
        {
            return (string)target.GetValue(PatternProperty);
        }

        public static void SetPattern(TextBox target, string value)
        {
            target.SetValue(PatternProperty, value);
        }

        private static void OnPatternChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var textBox = (TextBox)sender;
                textBox.PreviewKeyDown += textBox_PreviewKeyDown;
                textBox.PreviewTextInput += textBox_PreviewTextInput;
                textBox.TextInput += textBox_TextInput;
                DataObject.AddPastingHandler(textBox, textBox_PasteHandler);
            }
            else if (e.OldValue != null)
            {
                var textBox = (TextBox)sender;
                textBox.PreviewKeyDown -= textBox_PreviewKeyDown;
                textBox.PreviewTextInput -= textBox_PreviewTextInput;
                textBox.TextInput -= textBox_TextInput;
                DataObject.RemovePastingHandler(textBox, textBox_PasteHandler);
            }
        }

        private static void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && !IsAllowed((TextBox)sender, ' '))
                e.Handled = true;
        }

        private static void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsAllowed((TextBox)sender, e.Text[0]))
                e.Handled = true;
        }

        private static bool IsAllowed(TextBox textBox, char c)
        {
            var pattern = GetPattern(textBox);
            var text = textBox.Text;

            int count = 0;
            while (text.Length < pattern.Length && !IsInputPlaceholder(pattern[text.Length]))
            {
                if (c == pattern[text.Length])
                    return true;

                text += pattern[text.Length];
                count++;
            }

            if (count > 0)
            {
                var caretIndex = textBox.CaretIndex;
                textBox.Text = text;
                textBox.CaretIndex = caretIndex + count;
            }

            return IsAllowed(pattern, text.Length, c);
        }

        private static void textBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var caretIndex = textBox.CaretIndex;
            var written = AppendChar(textBox, e.Text[0]);
            textBox.CaretIndex = caretIndex + written;            
        }

        private static int AppendChar(TextBox textBox, char c)
        {
            var pattern = GetPattern(textBox);
            var text = textBox.Text + c;
            int count = 0;

            while (text.Length < pattern.Length)
            {
                var patternChar = pattern[text.Length];
                if (!IsInputPlaceholder(patternChar))
                {
                    text += patternChar;
                    count++;
                }
            }

            textBox.Text = text;
            return count;
        }

        private static bool IsInputPlaceholder(char c)
        {
            return c == '#' || c == 'A' || c == '?';
        }

        private static void textBox_PasteHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var textBox = (TextBox)sender;
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                var pattern = GetPattern(textBox);
                int insertIndex = textBox.SelectionStart;

                var builder = new StringBuilder(textBox.Text.Length + pastedText.Length);
                builder.Append(textBox.Text);
                if (textBox.SelectionLength > 0)
                    builder.Remove(insertIndex, textBox.SelectionLength);

                int pasteIndex = 0;

                while (builder.Length < pattern.Length && pasteIndex < pastedText.Length)
                {
                    var c = pastedText[pasteIndex++];
                    if (!IsAllowed(pattern, insertIndex, c))
                    {
                        var patternChar = pattern[insertIndex];
                        if (IsInputPlaceholder(patternChar))
                            break;

                        if (patternChar != c)
                        {
                            pasteIndex--;
                            c = patternChar;
                        }
                    }

                    builder.Insert(insertIndex++, c);
                }

                textBox.Text = builder.ToString();
                textBox.CaretIndex = insertIndex;
                e.CancelCommand();
                e.Handled = true;
            }
        }

        private static bool IsAllowed(string pattern, int patternIndex, char c)
        {
            if (patternIndex >= pattern.Length)
                return false;

            switch (pattern[patternIndex])
            {
                case '#':
                    return Char.IsDigit(c);
                case 'A':
                    return Char.IsLetter(c);
                case '?':
                    return true;
                default:
                    return (c == pattern[patternIndex]);
            }
        }
    }
}
