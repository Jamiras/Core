using System;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    public class TextFieldViewModel : FieldViewModelBase
    {
        public TextFieldViewModel(string label, int maxLength)
        {
            Label = label;
            MaxLength = maxLength;
        }

        public static readonly ModelProperty TextProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "Text", typeof(string), null);

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly ModelProperty MaxLengthProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "MaxLength", typeof(int), 80);

        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        public static readonly ModelProperty IsTextBindingDelayedProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "IsTextBindingDelayed", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the Text property binding should be delayed to account for typing.
        /// </summary>
        public bool IsTextBindingDelayed
        {
            get { return (bool)GetValue(IsTextBindingDelayedProperty); }
            set { SetValue(IsTextBindingDelayedProperty, value); }
        }

        protected override string Validate(ModelProperty property, object value)
        {
            if (property == TextFieldViewModel.TextProperty && IsRequired && String.IsNullOrEmpty((string)value))
                return String.Format("{0} is required", Label);

            return base.Validate(property, value);
        }

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (e.Property == TextFieldViewModel.TextProperty && IsTextBindingDelayed)
            {
                WaitForTyping(() => base.OnModelPropertyChanged(e));
                return;
            }

            base.OnModelPropertyChanged(e);
        }

        private void WaitForTyping(Action callback)
        {
            lock (typeof(TextFieldViewModel))
            {
                if (_typingTimer == null)
                {
                    _typingTimer = new System.Timers.Timer(300);
                    _typingTimer.AutoReset = false;
                    _typingTimer.Elapsed += (o, e) => _typingTimerCallback();
                }
                else
                {
                    _typingTimer.Stop();
                }

                _typingTimerCallback = callback;
                _typingTimer.Start();
            }
        }

        private System.Timers.Timer _typingTimer;
        private Action _typingTimerCallback;
    }
}
