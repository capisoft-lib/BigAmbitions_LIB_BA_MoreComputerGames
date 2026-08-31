// Registry/ownership tests only. Presentation is checked by the real Unity player harness.
namespace UnityEngine
{
    public static class JsonUtility
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson(object value, bool pretty = false) => System.Text.Json.JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, Options);
    }
    public class MonoBehaviour { }
    public class Camera { }
    public struct Vector2 { }
    public class GameObject
    {
        public static int Components;
        public T AddComponent<T>() { Components++; return (T)System.Activator.CreateInstance(typeof(T)); }
    }
}
namespace BAModAPI
{
    public class ModContext { public string ModId; public string ModRootPath; }
    public interface IModBigAmbitions
    { string[] RelativeAssetBundlePaths { get; } Task OnLoadAsync(ModContext context); Task OnUnloadAsync(); }
}
