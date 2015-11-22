using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Jamiras.ViewModels;
using Jamiras.ViewModels.Fields;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for AutoCompleteTextBox.xaml
    /// </summary>
    public partial class AutoCompleteTextBox : TextBox
    {
        static AutoCompleteTextBox()
        {
            // change default UpdateSourceTrigger to PropertyChanged so we get as-you-type suggestions
            var defaultMetadata = TextProperty.GetMetadata(typeof(TextBox));
            TextProperty.OverrideMetadata(typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(
                string.Empty, FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                defaultMetadata.PropertyChangedCallback, defaultMetadata.CoerceValueCallback, true,
                System.Windows.Data.UpdateSourceTrigger.PropertyChanged));
        }

        public AutoCompleteTextBox()
        {
            InitializeComponent();
            
            NoMatchesList = new[] { new LookupItem(0, "No Matches") };
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _suggestionsListBox = (ListBox)GetTemplateChild("suggestionsListBox");
        }

        private ListBox _suggestionsListBox;

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (IsTyping)
            {
                SelectedId = 0;
                IsTyping = false;
            }
            base.OnTextChanged(e);
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
            var textBox = ((AutoCompleteTextBox)source);
            if (textBox.IsLoaded)
            {
                if (e.NewValue != null && ((IEnumerable<LookupItem>)e.NewValue).Any())
                {
                    textBox.HasSuggestions = true;
                    textBox.IsPopupOpen = textBox.IsKeyboardFocusWithin;
                }
                else
                {
                    textBox.HasSuggestions = false;
                    textBox.IsPopupOpen = !String.IsNullOrEmpty(textBox.Text) && textBox.IsKeyboardFocusWithin;
                }
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            SelectAll();
            base.OnGotFocus(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            IsTyping = true;

            if (_suggestionsListBox.IsKeyboardFocusWithin)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        IsPopupOpen = false;
                        Focus();
                        e.Handled = true;
                        break;

                    case Key.Tab:
                    case Key.Enter:
                    case Key.Space:
                        LookupItem item = _suggestionsListBox.SelectedItem as LookupItem;
                        if (item != null)
                        {
                            SelectItem(item);
                            if (e.Key == Key.Tab)
                                MoveFocus(new TraversalRequest((e.KeyboardDevice.Modifiers == ModifierKeys.Shift) ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
                            else
                                Focus();

                            e.Handled = true;
                        }
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        if (IsPopupOpen)
                        {
                            IsPopupOpen = false;
                            e.Handled = true;
                        }
                        break;

                    case Key.Tab:
                        IsPopupOpen = false;
                        break;

                    case Key.Down:
                        if (IsPopupOpen)
                        {
                            _suggestionsListBox.SelectedIndex = 0;
                            _suggestionsListBox.UpdateLayout();
                            var listBoxItem = (ListBoxItem)_suggestionsListBox.ItemContainerGenerator.ContainerFromItem(_suggestionsListBox.SelectedItem);
                            listBoxItem.Focus();
                            e.Handled = true;
                        }
                        else
                        {
                            IsPopupOpen = true;
                        }
                        break;
                }
            }

            base.OnPreviewKeyDown(e);
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
                // if backed by a TextFieldViewModel, call SetText to circumvent the typing delay
                var textFieldViewModel = this.DataContext as TextFieldViewModelBase;
                if (textFieldViewModel != null)
                    textFieldViewModel.SetText(item.Label);
                else
                    Text = item.Label;

                SelectAll();

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
