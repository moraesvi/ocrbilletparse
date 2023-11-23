namespace OCRBilletParse.Interface
{
    public interface ImageRecognitionService
    {
        Task<string> GetText(byte[] image);
    }
}
