using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloudStorage.Consts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Permission
    {
        None = 0,
        Read,
        Owner,
        Contributer,
        Admin
    }
}
