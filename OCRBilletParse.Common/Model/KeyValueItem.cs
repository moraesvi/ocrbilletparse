namespace OCRBilletParse.Common
{
    public class KeyValueItem
    {
        public KeyValueItem() 
        {
            Key = "no-key";
        }
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class GenericKeyValueItem<T>
    {
        public GenericKeyValueItem()
        {
            Key = "no-key";
        }
        public string Key { get; set; }
        public T Value { get; set; }
    }
}
