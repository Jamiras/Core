using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Jamiras.Components;

namespace Jamiras.Controls
{
    /// <summary>
    /// Creates a formatted block of text from wiki markup.
    /// 
    /// == heading ==    - string in two or more equals is a heading
    /// ''italic''       - string in two single quotes is italicized
    /// '''bold'''       - string in three single quotes is bolded
    /// [[text:link]]    - creates a link with the provided text. command will be passed to the LinkCommand
    /// :indent          - indents a piece of text for each colon at the start of a line
    /// {{color|c|text}} - renders text in the color specified by c.
    /// </summary>
    public class FormattedTextBlock : TextBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedTextBlock"/> class.
        /// </summary>
        public FormattedTextBlock()
        {
            _hyperlinkCommand = new HyperlinkCommand(this);
            Visibility = Visibility.Collapsed;
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

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="LinkCommand"/>
        /// </summary>
        public static readonly DependencyProperty LinkCommandProperty = DependencyProperty.Register("LinkCommand",
            typeof(ICommand), typeof(FormattedTextBlock), new FrameworkPropertyMetadata(OnLinkChanged));

        /// <summary>
        /// Gets or sets the command to call when a link is clicked.
        /// </summary>
        public ICommand LinkCommand
        {
            get { return (ICommand)GetValue(LinkCommandProperty); }
            set { SetValue(LinkCommandProperty, value); }
        }

        private static void OnLinkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FormattedTextBlock)sender)._hyperlinkCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="Text"/>
        /// </summary>
        public static readonly new DependencyProperty TextProperty = DependencyProperty.Register("Text",
            typeof(string), typeof(FormattedTextBlock), new FrameworkPropertyMetadata(OnTextChanged));

        /// <summary>
        /// Gets or sets the unformatted text.
        /// </summary>
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
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            var converter = new SyntaxToInlineConverter(_hyperlinkCommand);
            converter.Convert(input, Inlines);
            Visibility = Visibility.Visible;
        }

        private class SyntaxToInlineConverter
        {
            public SyntaxToInlineConverter(ICommand hyperlinkCommand)
            {
                _formatStack = new Stack<InlineCollection>();
                _buffer = new StringBuilder();
                _hyperlinkCommand = hyperlinkCommand;
            }

            private readonly ICommand _hyperlinkCommand;
            private readonly Stack<InlineCollection> _formatStack;
            private readonly StringBuilder _buffer;
            private bool _isBold;
            private bool _isItalic;
            private bool _isHeading;
            private bool _isLink;
            private bool _isRedirectedLink;
            private bool _isNewLine;
            private Tokenizer _tokenizer;

            public void Convert(string input, InlineCollection inlinesToPopulate)
            {
                _formatStack.Push(inlinesToPopulate);
                _isNewLine = true;
                _tokenizer = Tokenizer.CreateTokenizer(input);

                while (_tokenizer.NextChar != '\0')
                {
                    switch (_tokenizer.NextChar)
                    {
                        case '\'':
                            if (_tokenizer.Match("'''"))
                            {
                                FlushInline();
                                ToggleState(ref _isBold, () => new Bold());
                                continue;
                            }
                            if (_tokenizer.Match("''"))
                            {
                                FlushInline();
                                ToggleState(ref _isItalic, () => new Italic());
                                continue;
                            }
                            break;

                        case '=':
                            if (HandleHeader())
                                continue;
                            break;

                        case ':':
                            if (_isNewLine && HandleIndent())
                                continue;
                            break;

                        case '[':
                            if (!_isLink && _tokenizer.Match("[["))
                            {
                                _isRedirectedLink = false;
                                FlushInline();
                                ToggleState(ref _isLink, () => new Hyperlink { Command = _hyperlinkCommand });
                                continue;
                            }
                            break;

                        case ']':
                            if (_isLink && _tokenizer.Match("]]"))
                            {
                                var parameter = _buffer.ToString();
                                _buffer.Length = 0;

                                if (!_isRedirectedLink)
                                    FlushInline();

                                ToggleState(ref _isLink, null);

                                var hyperlink = _formatStack.Peek().Last() as Hyperlink;
                                if (hyperlink != null)
                                    hyperlink.CommandParameter = parameter;

                                continue;
                            }
                            break;

                        case '|':
                            if (_isLink)
                            {
                                _isRedirectedLink = true;
                                _tokenizer.Advance();
                                FlushInline();
                                continue;
                            }
                            break;

                        case '{':
                            if (HandleColoredText())
                                continue;
                            break;

                        case '\r':
                            _tokenizer.Advance();
                            FlushInline();
                            continue;

                        case '\n':
                            _tokenizer.Advance();
                            FlushInline();
                            _formatStack.Peek().Add(new LineBreak());
                            _isNewLine = true;
                            continue;
                    }

                    _isNewLine = false;
                    _buffer.Append(_tokenizer.NextChar);
                    _tokenizer.Advance();
                }

                FlushInline();
            }

            private bool HandleColoredText()
            {
                if (!_tokenizer.Match("{{color|"))
                    return false;

                FlushInline();
                while (_tokenizer.NextChar != '|')
                {
                    if (_tokenizer.NextChar == '\0')
                        return false;

                    _buffer.Append(_tokenizer.NextChar);
                    _tokenizer.Advance();
                }

                var color = _buffer.ToString();
                _buffer.Length = 0;

                _tokenizer.Advance();
                while (!_tokenizer.Match("}}"))
                {
                    if (_tokenizer.NextChar == '\0')
                        return false;

                    _buffer.Append(_tokenizer.NextChar);
                    _tokenizer.Advance();
                }

                var inline = new Run(_buffer.ToString());
                inline.Foreground = (Brush)new BrushConverter().ConvertFromString(color);
                _formatStack.Peek().Add(inline);

                _buffer.Length = 0;
                return true;
            }

            private bool HandleHeader()
            {
                int headingLevel = _tokenizer.MatchSubstring("======");
                if (headingLevel >= 2)
                {
                    if (_isHeading)
                    {
                        if (_buffer.Length == 0 || _buffer[_buffer.Length - 1] != ' ')
                            return false;

                        _buffer.Length--;
                        FlushInline();
                        ToggleState(ref _isHeading, null);

                        for (int i = 0; i < headingLevel; i++)
                            _tokenizer.Advance();
                        return true;
                    }

                    if (_isNewLine)
                    {
                        var headerToken = new String('=', headingLevel) + ' ';
                        if (_tokenizer.Match(headerToken))
                        {
                            FlushInline();
                            ToggleState(ref _isHeading, () => new Bold { FontSize = headingLevel + 11 });
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool HandleIndent()
            {
                int indent = _tokenizer.MatchSubstring("::::::::::::::::::::");
                var indentToken = new String(':', indent) + ' ';
                if (!_tokenizer.Match(indentToken))
                    return false;

                FlushInline();
                _formatStack.Peek().Add(new Run(new String(' ', indent * 2)));
                return true;
            }

            private void FlushInline()
            {
                if (_buffer.Length > 0)
                {
                    _formatStack.Peek().Add(new Run(_buffer.ToString()));
                    _buffer.Length = 0;
                }
            }

            private void ToggleState(ref bool state, Func<Span> createState)
            {
                if (state)
                {
                    _formatStack.Pop();
                    state = false;
                }
                else
                {
                    var inline = createState();
                    _formatStack.Peek().Add(inline);
                    _formatStack.Push(inline.Inlines);
                    state = true;
                }
            }
        }
    }
}
