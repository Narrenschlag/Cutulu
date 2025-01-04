namespace Cutulu.Core
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class DontEncode : System.Attribute
    {
        public DontEncode()
        {

        }
    }
}