namespace GoodsTracker.DataCollector.Common.Parsers.Exceptions;

[System.Serializable]
public class InvalidItemFormatException : Exception
{
    public InvalidItemFormatException() { }
    public InvalidItemFormatException(string message) : base(message) { }
    public InvalidItemFormatException(string message, System.Exception inner) : base(message, inner) { }
    protected InvalidItemFormatException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
