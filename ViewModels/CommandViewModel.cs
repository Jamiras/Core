using System.Windows.Input;
using System.Windows.Media;
using Jamiras.DataModels;

namespace Jamiras.ViewModels
{
    public class CommandViewModel : ViewModelBase
    {
        public CommandViewModel(string label, ICommand command)
        {
            Label = label;
            Command = command;
        }

        public static readonly ModelProperty LabelProperty = ModelProperty.Register(typeof(CommandViewModel), "Label", typeof(string), "");

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly ModelProperty CommandProperty = ModelProperty.Register(typeof(CommandViewModel), "Command", typeof(ICommand), null);

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly ModelProperty CommandParameterProperty = ModelProperty.Register(typeof(CommandViewModel), "CommandParameter", typeof(object), null);

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly ModelProperty ImageSourceProperty = ModelProperty.Register(typeof(CommandViewModel), "ImageSource", typeof(ImageSource), null);

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly ModelProperty IsSelectedProperty = ModelProperty.Register(typeof(CommandViewModel), "IsSelected", typeof(bool), false);

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
    }
}
