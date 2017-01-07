namespace System
{
    public static class TypeExtensions
    {
        // By JaredPar (http://stackoverflow.com/a/457708)
        public static bool IsSubclassOfGeneric(this Type me, Type generic)
        {
            generic = generic.GetGenericTypeDefinition();

            while (me != typeof(object))
            {
                var cur = me.IsGenericType ? me.GetGenericTypeDefinition() : me;
                if (generic == cur)
                    return true;
                me = me.BaseType;
            }
            return false;
        }
    }
}
