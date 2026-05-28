using System.Collections.Generic;

internal class UDynamicVars
{
    private readonly Dictionary<string, object> storage = [];

    public object this[string identifier]
    {
        get
        {
            if (storage.TryGetValue(identifier, out object result))
            {
                return result;
            }

            return null;
        }

        set => storage[identifier] = value;
    }
}
