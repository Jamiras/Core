using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Jamiras.Commands;

namespace Jamiras.Controls
{
    public class FormattedTextBlock : ContentControl
    {
        public FormattedTextBlock()
        {
            _textBlock = new TextBlock();
            this.Content = _textBlock;

            _hyperlinkCommand = new DelegateCommand<string>(OnHyperlinkClicked);
        }

        private TextBlock _textBlock;
        private ICommand _hyperlinkCommand;

        public static readonly DependencyProperty LinkCommandProperty = DependencyProperty.Register("LinkCommand",
            typeof(ICommand), typeof(FormattedTextBlock));

        public ICommand LinkCommand
        {
            get { return (ICommand)GetValue(LinkCommandProperty); }
            set { SetValue(LinkCommandProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text",
            typeof(string), typeof(FormattedTextBlock), new FrameworkPropertyMetadata(OnTextChanged));

        public string Text
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
            _textBlock.Inlines.Clear();
            if (String.IsNullOrEmpty(input))
                return;

            bool isBold = false, isItalic = false, isLink = false, isHeading = false, isRedirectedLink = false;
            Stack<InlineCollection> formatStack = new Stack<InlineCollection>();
            formatStack.Push(_textBlock.Inlines);

            int index = 0, start = 0;
            while (index < input.Length)
            {
                switch (input[index])
                {
                    case '\'':
                        if (index + 1 == input.Length || input[index + 1] != '\'')
                            goto default;

                        if (index < input.Length + 2 && input[index + 2] == '\'')
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
                        break;

                    default:
                        index++;
                        break;
                }
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

        private void OnHyperlinkClicked(string link)
        {
            var command = LinkCommand;
            if (command != null)
                command.Execute(link);
        }
    }
}
