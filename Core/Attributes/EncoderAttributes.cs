namespace Cutulu.Core
{
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class DontEncode : System.Attribute
    {
        public DontEncode()
        {

        }
    }
}