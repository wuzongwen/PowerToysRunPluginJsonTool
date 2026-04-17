using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Wox.Plugin;
using Wox.Infrastructure;

namespace Community.PowerToys.Run.Plugin.JsonTool
{
    public class Main : IPlugin
    {
        public static string PluginID => "b2c3d4e5-f6a7-8901-b2c3-d4e5f6a78901";
        
        private string? IconPath { get; set; }
        private PluginInitContext? Context { get; set; }

        public string Name => "JSON工具";
        public string Description => "JSON格式化、压缩、转义、生成C#实体等工具";

        public void Init(PluginInitContext context)
        {
            Context = context;
            IconPath = "Images/icon.light.png";
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            if (string.IsNullOrEmpty(query.Search))
            {
                results.Add(new Result
                {
                    Title = "JSON工具箱",
                    SubTitle = "输入json空格+JSON内容 打开JSON工具箱窗口",
                    IcoPath = IconPath,
                    Action = _ => { ShowWindow(null); return true; }
                });
                return results;
            }

            results.Add(new Result
            {
                Title = "打开JSON工具",
                SubTitle = query.Search.Length > 50 ? query.Search[..50] + "..." : query.Search,
                IcoPath = IconPath,
                Action = _ => { ShowWindow(query.Search); return true; }
            });

            return results;
        }

        private void ShowWindow(string? json)
        {
            if (!string.IsNullOrEmpty(json) && !json.StartsWith("{") && !json.StartsWith("["))
            {
                json = "";
            }
            var window = new JsonToolWindow();
            if (!string.IsNullOrEmpty(json))
            {
                window.SetInputJson(json);
            }
            window.Show();
        }

        public static class JsonHelper
        {
            public static string Format(string json)
            {
                var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                });
            }

            public static string Minify(string json)
            {
                var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc);
            }

            public static string Escape(string text)
            {
                return JsonSerializer.Serialize(text);
            }

            public static string Unescape(string text)
            {
                try { return JsonSerializer.Deserialize<string>(text) ?? text; }
                catch { return text.Replace("\\\"", "\"").Replace("\\\\", "\\"); }
            }

            public static string Validate(string json)
            {
                try
                {
                    JsonDocument.Parse(json);
                    return "✓ JSON格式有效";
                }
                catch (JsonException ex)
                {
                    return $"✗ JSON格式错误: {ex.Message}";
                }
            }

            public static string GenerateCSharp(string json)
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return "JSON必须为对象类型";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("public class RootObject");
                sb.AppendLine("{");
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var type = GetCSharpType(prop.Value);
                    var name = ToPascalCase(prop.Name);
                    sb.AppendLine($"    public {type} {name} {{ get; set; }}");
                }
                sb.AppendLine("}");
                return sb.ToString();
            }

            public static string Sort(string json)
            {
                var doc = JsonDocument.Parse(json);
                var dict = new SortedDictionary<string, JsonElement>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.Clone();
                }
                var obj = JsonSerializer.Serialize(dict); 
                return Format(obj);
            }

            private static string GetCSharpType(JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => "string",
                    JsonValueKind.Number => "double",
                    JsonValueKind.True or JsonValueKind.False => "bool",
                    JsonValueKind.Array => "List<object>",
                    JsonValueKind.Object => "object",
                    JsonValueKind.Null => "object",
                    _ => "string"
                };
            }

            private static string ToPascalCase(string s)
            {
                if (string.IsNullOrEmpty(s)) return s;
                return char.ToUpper(s[0]) + (s.Length > 1 ? s[1..] : "");
            }
        }
    }
}