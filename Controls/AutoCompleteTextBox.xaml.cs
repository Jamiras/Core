using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Jamiras.ViewModels;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for DidYouMeanTextBox.xaml
    /// </summary>
    public partial class AutoCompleteTextBox : UserControl
    {
        public AutoCompleteTextBox()
        {
            InitializeComponent();

            grid.DataContext = this;

            NoMatchesList = new[] { new LookupItem(0, "No Matches") };
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AutoCompleteTextBox),
            new FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextChanged));

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void TextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            AutoCompleteTextBox textBox = ((AutoCompleteTextBox)source);
            if (textBox.IsTyping)
            {
                textBox.SelectedId = 0;
                textBox.IsTyping = false;
            }
        }

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register("MaxLength", typeof(int), typeof(AutoCompleteTextBox));

        /// <summary>
        /// Gets or sets the maximum length of the text
        /// </summary>
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        public static readonly DependencyProperty SelectedIdProperty =
            DependencyProperty.Register("SelectedId", typeof(int), typeof(AutoCompleteTextBox),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Gets or sets the unique identifier of the selected item
        /// </summary>
        public int SelectedId
        {
            get { return (int)GetValue(SelectedIdProperty); }
            set { SetValue(SelectedIdProperty, value); }
        }

        public static readonly DependencyProperty MatchColorProperty = 
            DependencyProperty.Register("MatchColor", typeof(Brush), typeof(AutoCompleteTextBox),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE0, 0xFF, 0xE0))));
                
        public Brush MatchColor
        {
            get { return (Brush)GetValue(MatchColorProperty); }
            set { SetValue(MatchColorProperty, value); }
        }

        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register("IsPopupOpen", typeof(bool), typeof(AutoCompleteTextBox));

        /// <summary>
        /// Gets whether or not the suggestion list is visible
        /// </summary>
        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            set { SetValue(IsPopupOpenProperty, value); }
        }

        private static readonly DependencyPropertyKey HasSuggestionsPropertyKey =
            DependencyProperty.RegisterReadOnly("HasSuggestions", typeof(bool), typeof(AutoCompleteTextBox), new PropertyMetadata());

        public static readonly DependencyProperty HasSuggestionsProperty = HasSuggestionsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether or not suggestions are available.
        /// </summary>
        public bool HasSuggestions
        {
            get { return (bool)GetValue(HasSuggestionsProperty); }
            private set { SetValue(HasSuggestionsPropertyKey, value); }
        }

        public static readonly DependencyProperty SuggestionsProperty =
            DependencyProperty.Register("Suggestions", typeof(IEnumerable<LookupItem>), typeof(AutoCompleteTextBox), 
            new FrameworkPropertyMetadata(OnSuggestionsChanged));

        /// <summary>
        /// Gets the current set of suggestions
        /// </summary>
        public IEnumerable<LookupItem> Suggestions
        {
            get { return (IEnumerable<LookupItem>)GetValue(SuggestionsProperty); }
            set { SetValue(SuggestionsProperty, value); }
        }

        public IEnumerable<LookupItem> NoMatchesList { get; private set; }

        private static void OnSuggestionsChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            AutoCompleteTextBox textBox = ((AutoCompleteTextBox)source);
            if (textBox.IsLoaded)
            {
                if (e.NewValue != null && ((IEnumerable<LookupItem>)e.NewValue).Any())
                {
                    textBox.HasSuggestions = true;
                    textBox.IsPopupOpen = true;
                }
                else
                {
                    textBox.HasSuggestions = false;
                }
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            IsTyping = true;

            switch (e.Key)
            {
                case Key.Escape:
                case Key.Tab:
                    IsPopupOpen = false;
                    break;

                case Key.Down:
                    if (IsPopupOpen)
                    {
                        suggestionsListBox.SelectedIndex = 0;
                        suggestionsListBox.Focus();                        
                    }
                    break;
            }
        }

        private void listBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    IsPopupOpen = false;
                    autoCompleteTextBox.Focus();
                    e.Handled = true;
                    break;

                case Key.Tab:
                case Key.Enter:
                case Key.Space:
                    LookupItem item = suggestionsListBox.SelectedItem as LookupItem;
                    if (item != null)
                    {
                        SelectItem(item);
                        if (e.Key == Key.Tab)
                            autoCompleteTextBox.MoveFocus(new TraversalRequest((e.KeyboardDevice.Modifiers == ModifierKeys.Shift) ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
                        else
                            autoCompleteTextBox.Focus();

                        e.Handled = true;
                    }
                    break;
            }
        }

        private void item_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                LookupItem item = ((ListBoxItem)sender).Content as LookupItem;
                if (item != null)
                {
                    SelectItem(item);
                    e.Handled = true;
                }
            }
        }

        private void SelectItem(LookupItem item)
        {
            if (item.Id > 0)
            {
                autoCompleteTextBox.Text = item.Label;
                autoCompleteTextBox.SelectAll();

                SelectedId = item.Id;
                IsPopupOpen = false;
            }
            else
            {
                IsPopupOpen = false;
            }
        }

        private bool IsTyping { get; set; }
    }
}
