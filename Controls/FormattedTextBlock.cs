using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Jamiras.Commands;

namespace Jamiras.Controls
{
    public class FormattedTextBlock : TextBlock
    {
        public FormattedTextBlock()
        {
            _hyperlinkCommand = new HyperlinkCommand(this);
        }

        private class HyperlinkCommand : ICommand
        {
            public HyperlinkCommand(FormattedTextBlock owner)
            {
                _owner = owner;
            }

            private readonly FormattedTextBlock _owner;

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                var command = _owner.LinkCommand;
                return (command != null && command.CanExecute(parameter));
            }

            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, EventArgs.Empty);
            }

            public void Execute(object parameter)
            {
                var command = _owner.LinkCommand;
                if (command != null)
                    command.Execute(parameter);
            }

            #endregion
        }

        private readonly HyperlinkCommand _hyperlinkCommand;

        public static readonly DependencyProperty LinkCommandProperty = DependencyProperty.Register("LinkCommand",
            typeof(ICommand), typeof(FormattedTextBlock), new FrameworkPropertyMetadata(OnLinkChanged));
        
        public ICommand LinkCommand
        {
            get { return (ICommand)GetValue(LinkCommandProperty); }
            set { SetValue(LinkCommandProperty, value); }
        }

        private static void OnLinkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FormattedTextBlock)sender)._hyperlinkCommand.RaiseCanExecuteChanged();
        }

        public static readonly new DependencyProperty TextProperty = DependencyProperty.Register("Text",
            typeof(string), typeof(FormattedTextBlock), new FrameworkPropertyMetadata(OnTextChanged));

        public new string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FormattedTextBlock)sender).ParseText((string)e.NewValue);
        }

        private void ParseText(string input)
        {
            Inlines.Clear();
            if (String.IsNullOrEmpty(input))
                return;

            bool isBold = false, isItalic = false, isLink = false, isHeading = false, isRedirectedLink = false;
            bool isNewLine = true;
            Stack<InlineCollection> formatStack = new Stack<InlineCollection>();
            formatStack.Push(Inlines);

            int index = 0, start = 0;
            while (index < input.Length)
            {
                switch (input[index])
                {
                    case '\'':
                        if (index + 1 == input.Length || input[index + 1] != '\'')
                            goto default;

                        if (index < input.Length - 2 && input[index + 2] == '\'')
                        {
                            FlushInline(formatStack.Peek(), input, start, index);
                            isBold = ToggleState(isBold, formatStack, () => new Bold());
                            index += 3;
                        }
                        else
                        {
                            FlushInline(formatStack.Peek(), input, start, index);
                            isItalic = ToggleState(isItalic, formatStack, () => new Italic());
                            index += 2;
                        }
                        start = index;
                        break;

                    case '=':
                        if (!isNewLine && !isHeading)
                            goto default;

                        int headingLevel = 1;
                        while (index + headingLevel < input.Length && input[index + headingLevel] == '=')
                            headingLevel++;
                        if (headingLevel == 1)
                            goto default;

                        if (isHeading)
                        {
                            if (index == 0 || input[index - 1] != ' ')
                                goto default;

                            index--;
                        }
                        else 
                        {
                            if (index + headingLevel == input.Length || input[index + headingLevel] != ' ')
                                goto default;
                        }

                        FlushInline(formatStack.Peek(), input, start, index);
                        isHeading = ToggleState(isHeading, formatStack, () => new Bold { FontSize = headingLevel + 11 });
                        index += headingLevel + 1;
                        start = index;
                        break;

                    case ':':
                        if (!isNewLine)
                            goto default;

                        int indent = 1;
                        while (index + indent < input.Length && input[index + indent] == ':')
                            indent++;
                        if (index + indent == input.Length || input[index + indent] != ' ')
                            goto default;

                        FlushInline(formatStack.Peek(), input, start, index);
                        formatStack.Peek().Add(new Run(new String(' ', indent * 2)));
                        index += indent + 1;
                        start = index;
                        break;

                    case '[':
                        if (isLink || index + 1 == input.Length || input[index + 1] != '[')
                            goto default;

                        isRedirectedLink = false;
                        FlushInline(formatStack.Peek(), input, start, index);
                        isLink = ToggleState(isLink, formatStack, () => new Hyperlink { Command = _hyperlinkCommand });
                        index += 2;
                        start = index;
                        break;

                    case ']':
                        if (!isLink || index + 1 == input.Length || input[index + 1] != ']')
                            goto default;

                        string parameter = input.Substring(start, index - start);
                        if (!isRedirectedLink)
                            FlushInline(formatStack.Peek(), input, start, index);

                        isLink = ToggleState(isLink, formatStack, null);

                        var hyperlink = formatStack.Peek().Last() as Hyperlink;
                        if (hyperlink != null)
                            hyperlink.CommandParameter = parameter;

                        index += 2;
                        start = index;
                        break;

                    case '|':
                        if (!isLink)
                            goto default;

                        isRedirectedLink = true;
                        FlushInline(formatStack.Peek(), input, start, index);
                        index++;
                        start = index;
                        break;

                    case '\r':
                        FlushInline(formatStack.Peek(), input, start, index);
                        index++;
                        start = index;
                        break;

                    case '\n':
                        FlushInline(formatStack.Peek(), input, start, index);
                        formatStack.Peek().Add(new LineBreak());
                        index++;
                        start = index;
                        isNewLine = true;
                        continue;

                    default:
                        index++;
                        break;
                }

                isNewLine = false;
            }

            FlushInline(formatStack.Peek(), input, start, index);
        }

        private static void FlushInline(InlineCollection container, string input, int start, int index)
        {
            if (index > start)
                container.Add(new Run(input.Substring(start, index - start)));
        }

        private static bool ToggleState(bool state, Stack<InlineCollection> formatStack, Func<Span> createState)
        {
            if (state)
            {
                formatStack.Pop();
                return false;
            }

            var inline = createState();
            formatStack.Peek().Add(inline);
            formatStack.Push(inline.Inlines);
            return true;
        }
    }
}
