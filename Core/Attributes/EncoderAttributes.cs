namespace Cutulu.Core
{
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class DontEncode : System.Attribute
    {
        public DontEncode()
        {

        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class Encodable : System.Attribute
    {
        public Encodable()
        {

        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class DisableEncoder : System.Attribute
    {
        public DisableEncoder()
        {

        }
    }
}