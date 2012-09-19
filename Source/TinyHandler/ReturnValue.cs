namespace TinyHandler
{
    public class ReturnValue
    {
        public static EmptyReturnValue Empty
        {
            get
            {
                return new EmptyReturnValue();
            }
        }
    }

    public class EmptyReturnValue
    {
        
    }
}
