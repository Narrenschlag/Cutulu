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
}