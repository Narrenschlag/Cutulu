namespace Cutulu.Core
{
    using System;

    public static partial class Numberf
    {
        public static bool IsSigned(this object _obj)
        {
            return _obj switch
            {
                sbyte => true,
                short => true,
                int => true,
                long => true,

                decimal => true,
                float => true,
                double => true,

                _ => false,
            };
        }

        public static bool IsUnsigned(this object _obj)
        {
            return _obj switch
            {
                byte => true,
                ushort => true,
                uint => true,
                ulong => true,

                _ => false,
            };
        }
    }
}