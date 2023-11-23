namespace OCRBilletParse.Queue.Interface;
public interface IBillParseQueueInfraestructure
{
    void SendToQueue(byte[] message);
}
