namespace Lantean.QBTMud.Models
{
    public record RowContext<T>
    {
        private readonly Func<T, object?> _valueGetter;

        public RowContext(string headerText, T data, Func<T, object?> valueGetter)
        {
            HeaderText = headerText;
            Data = data;
            _valueGetter = valueGetter;
        }

        public string HeaderText { get; }

        public T Data { get; set; }

        public object? GetValue()
        {
            return _valueGetter(Data);
        }
    }
}