using System.Windows.Input;
using System.Windows.Media;
using Jamiras.DataModels;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// ViewModel for a button or similar control.
    /// </summary>
    public class CommandViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandViewModel"/> class.
        /// </summary>
        /// <param name="label">A label for the control.</param>
        /// <param name="command">The command to execute when the control is activated.</param>
        public CommandViewModel(string label, ICommand command)
        {
            Label = label;
            Command = command;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Label"/>
        /// </summary>
        public static readonly ModelProperty LabelProperty = ModelProperty.Register(typeof(CommandViewModel), "Label", typeof(string), "");

        /// <summary>
        /// Gets or sets the label for the control.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Command"/>
        /// </summary>
        public static readonly ModelProperty CommandProperty = ModelProperty.Register(typeof(CommandViewModel), "Command", typeof(ICommand), null);

        /// <summary>
        /// Gets or sets the command to execute when the control is activated.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="CommandParameter"/>
        /// </summary>
        public static readonly ModelProperty CommandParameterProperty = ModelProperty.Register(typeof(CommandViewModel), "CommandParameter", typeof(object), null);

        /// <summary>
        /// Gets or sets the parameter to pass to the command when the control is activated.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="ImageSource"/>
        /// </summary>
        public static readonly ModelProperty ImageSourceProperty = ModelProperty.Register(typeof(CommandViewModel), "ImageSource", typeof(ImageSource), null);

        /// <summary>
        /// Gets or sets an image to display in the control.
        /// </summary>
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsSelected"/>
        /// </summary>
        public static readonly ModelProperty IsSelectedProperty = ModelProperty.Register(typeof(CommandViewModel), "IsSelected", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether this control is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
    }
}
