using System;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// Base class for fields that allow input of text.
    /// </summary>
    public abstract class TextFieldViewModelBase : FieldViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFieldViewModelBase"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="maxLength">The maximum length of the field.</param>
        protected TextFieldViewModelBase(string label, int maxLength)
        {
            Label = label;
            MaxLength = maxLength;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Text"/>
        /// </summary>
        public static readonly ModelProperty TextProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "Text", typeof(string), null);

        /// <summary>
        /// Gets or sets the text in the field.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Sets the <see cref="Text"/> property without using the delayed binding if <see cref="IsTextBindingDelayed"/> is <c>true</c>.
        /// </summary>
        internal virtual void SetText(string value)
        {
            if (!IsTextBindingDelayed)
            {
                Text = value;
            }
            else
            {
                StopWaitingForTyping();
                SetValueCore(IsTextBindingDelayedProperty, false); // use SetValueCore to avoid raising the PropertyChanged event
                Text = value;
                SetValueCore(IsTextBindingDelayedProperty, true);
            }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="MaxLength"/>
        /// </summary>
        public static readonly ModelProperty MaxLengthProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "MaxLength", typeof(int), 80);

        /// <summary>
        /// Gets or sets the maximum length of the <see cref="Text"/> value.
        /// </summary>
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsTextBindingDelayed"/>
        /// </summary>
        public static readonly ModelProperty IsTextBindingDelayedProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "IsTextBindingDelayed", typeof(bool), false, OnIsTextBindingDelayedChanged);

        /// <summary>
        /// Gets or sets whether the Text property binding should be delayed to account for typing.
        /// </summary>
        public bool IsTextBindingDelayed
        {
            get { return (bool)GetValue(IsTextBindingDelayedProperty); }
            set { SetValue(IsTextBindingDelayedProperty, value); }
        }

        private static void OnIsTextBindingDelayedChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                TextFieldViewModelBase.StopWaitingForTyping();
        }

        /// <summary>
        /// Validates a value being assigned to a property.
        /// </summary>
        /// <param name="property">Property being modified.</param>
        /// <param name="value">Value being assigned to the property.</param>
        /// <returns>
        ///   <c>null</c> if the value is valid for the property, or an error message indicating why the value is not valid.
        /// </returns>
        protected override string Validate(ModelProperty property, object value)
        {
            if (property == TextFieldViewModel.TextProperty && IsRequired && String.IsNullOrEmpty((string)value))
                return String.Format("{0} is required.", LabelWithoutAccelerators);

            return base.Validate(property, value);
        }

        /// <summary>
        /// Notifies any subscribers that the value of a <see cref="ModelProperty" /> has changed.
        /// </summary>
        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (e.Property == TextFieldViewModel.TextProperty && IsTextBindingDelayed)
            {
                WaitForTyping(() => base.OnModelPropertyChanged(e));
                return;
            }
            else if (e.Property == IsRequiredProperty)
            {
                Validate(TextProperty);
            }

            base.OnModelPropertyChanged(e);
        }

        /// <summary>
        /// Performs any data processing that needs to occur prior to committing.
        /// </summary>
        protected override void OnBeforeCommit()
        {
            if (_typingTimerCallback != null)
            {
                Action callback = null;

                lock (typeof(TextFieldViewModel))
                {
                    if (_typingTimerCallback != null)
                    {
                        callback = _typingTimerCallback;
                        _typingTimerCallback = null;
                    }
                }

                if (callback != null)
                    callback();
            }

            base.OnBeforeCommit();
        }

        /// <summary>
        /// Schedules a callback to be called when the user stops typing.
        /// </summary>
        public static void WaitForTyping(Action callback)
        {
            lock (typeof(TextFieldViewModel))
            {
                if (_typingTimer == null)
                {
                    _typingTimer = new System.Timers.Timer(300);
                    _typingTimer.AutoReset = false;
                    _typingTimer.Elapsed += TypingTimerElapsed;
                }
                else
                {
                    _typingTimer.Stop();
                }

                _typingTimerCallback = callback;
                _typingTimer.Start();
            }
        }

        private static void TypingTimerElapsed(object sender, EventArgs e)
        {
            Action callback = null;

            lock (typeof(TextFieldViewModel))
            {
                callback = _typingTimerCallback;
                _typingTimerCallback = null;
            }

            if (callback != null)
                callback();
        }

        private static void StopWaitingForTyping()
        {
            if (_typingTimer != null)
            {
                _typingTimer.Stop();

                lock (typeof(TextFieldViewModel))
                {
                    _typingTimerCallback = null;
                }
            }
        }

        private static System.Timers.Timer _typingTimer;
        private static Action _typingTimerCallback;
    }
}
