namespace Cedar.Projections.Example.RavenDb.Handlers
{
    using System;
    using System.Linq;
    using System.Text;

    internal static class TypeUtilities
    {
        internal static string ToPartiallyQualifiedName(this Type type)
        {
            var sb = new StringBuilder();
            sb.Append(type.FullName);
            if (type.IsGenericType)
            {
                sb.Append("[");

                sb.Append(string.Join(", ",
                    type.GetGenericArguments()
                        .Select(g => "[" + ToPartiallyQualifiedName(g) + "]")
                        .ToArray()));

                sb.Append("]");
            }
            sb.Append(", ").Append(type.Assembly.GetName().Name);
            return sb.ToString();
        }
    }
}