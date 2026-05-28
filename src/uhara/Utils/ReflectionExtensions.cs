using System.Reflection;

internal static class ReflectionExtensions
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    extension(object? obj)
    {
        public T? GetValue<T>(string fieldName, BindingFlags flags = InstanceFlags)
        {
            return typeof(T).GetField(fieldName, flags) is { } fi
                ? (T?)fi.GetValue(obj)
                : default;
        }

        public void SetValue<T>(string fieldName, T value, BindingFlags flags = InstanceFlags)
        {
            if (typeof(T).GetField(fieldName, flags) is { } fi)
                fi.SetValue(obj, value);
        }
    }
}
