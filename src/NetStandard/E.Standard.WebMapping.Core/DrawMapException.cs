using System;

public class DrawMapException : Exception
{
    public DrawMapException(Exception ex)
        : base(ex.Message, ex)
    {
    }

    public DrawMapException(string message)
        : base(message)
    { }
}
