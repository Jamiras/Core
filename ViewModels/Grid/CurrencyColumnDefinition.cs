using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    public class CurrencyColumnDefinition : GridColumnDefinition
    {
        public CurrencyColumnDefinition(string header, ModelProperty sourceProperty, CurrencyFieldMetadata metadata)
            : base(header, sourceProperty)
        {
            _metadata = metadata;
        }

        private readonly CurrencyFieldMetadata _metadata;

        private static readonly IConverter _floatToCurrencyConverter = new DelegateConverter(ConvertFloatToCurrency, null);

        private static string ConvertFloatToCurrency(object c)
        {
            return String.Format("${0:F2}", c);
        }

        public static readonly ModelProperty SummarizationProperty =
            ModelProperty.Register(typeof(CurrencyColumnDefinition), "Summarization", typeof(Summarization), Summarization.None, OnSummarizationChanged);

        /// <summary>
        /// Gets or sets the type of summarization to perform on this column.
        /// </summary>
        public Summarization Summarization
        {
            get { return (Summarization)GetValue(SummarizationProperty); }
            set { SetValue(SummarizationProperty, value); }
        }

        private static void OnSummarizationChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var column = (CurrencyColumnDefinition)sender;
            switch ((Summarization)e.NewValue)
            {
                case Summarization.None:
                    column.SummarizeFunction = null;
                    break;
                case Summarization.Total:
                    column.SummarizeFunction = TotalCurrencies;
                    break;
                default:
                    throw new NotSupportedException("No currency summarizer for " + e.NewValue);
            }
        }

        private static string TotalCurrencies(System.Collections.IEnumerable values)
        {
            float total = 0;
            foreach (var f in values)
            {
                if (f is float)
                    total += (float)f;
            }

            return ConvertFloatToCurrency(total);
        }

        protected override FieldViewModelBase CreateFieldViewModel(GridRowViewModel row)
        {
            if (IsReadOnly)
            {
                var textViewModel = new ReadOnlyTextFieldViewModel(Header);
                textViewModel.IsRightAligned = true;
                textViewModel.SetBinding(ReadOnlyTextFieldViewModel.TextProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.OneWay, _floatToCurrencyConverter));
                return textViewModel;
            }

            var viewModel = new CurrencyFieldViewModel(Header, _metadata);
            viewModel.BindValue(row, SourceProperty, ModelBindingMode.TwoWay);
            return viewModel;
        }
    }
}
