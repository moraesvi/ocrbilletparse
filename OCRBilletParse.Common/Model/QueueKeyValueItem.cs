namespace OCRBilletParse.Common.Model;
public struct QueueKeyValueItem<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
}
