using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;

namespace Community.PowerToys.Run.Plugin.JsonTool
{
    public partial class JsonToolWindow : Window
    {
        private string lastOutput = "";

        public JsonToolWindow()
        {
            InitializeComponent();
        }

        public void SetInputJson(string json)
        {
            InputTextBox.Text = json;
            FormatClick(null, null);
        }

        private void ShowStatus(string message, bool isError = false)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError ? 
                System.Windows.Media.Brushes.DarkRed : 
                System.Windows.Media.Brushes.DarkGreen;
        }

        private string GetInput() => InputTextBox.Text.Trim();

        private void SetOutput(string output)
        {
            lastOutput = output;
            OutputTextBox.Text = output;
        }

        private void FormatClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入JSON内容", true); return; }
            try
            {
                var doc = JsonDocument.Parse(input);
                if (doc.RootElement.ValueKind != JsonValueKind.Object && doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    ShowStatus("请输入有效的JSON对象或数组", true); return;
                }
                var output = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                });
                SetOutput(output);
                TextModeRadio.IsChecked = true;
                TextModeClick(null, null);
                ShowStatus("格式化成功");
            }
            catch (Exception ex) { ShowStatus("格式化失败: " + ex.Message, true); }
        }

        private void CompressClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入JSON内容", true); return; }
            try
            {
                var doc = JsonDocument.Parse(input);
                SetOutput(JsonSerializer.Serialize(doc, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
                ShowStatus("压缩成功");
            }
            catch (Exception ex) { ShowStatus("压缩失败: " + ex.Message, true); }
        }

        private void EscapeClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入内容", true); return; }
            SetOutput(JsonSerializer.Serialize(input, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            ShowStatus("转义成功");
        }

        private void UnescapeClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入内容", true); return; }
            try { SetOutput(JsonSerializer.Deserialize<string>(input) ?? input); ShowStatus("去转义成功"); }
            catch { SetOutput(input.Replace("\\\"", "\"").Replace("\\\\", "\\")); ShowStatus("去转义成功"); }
        }

        private void ValidateClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入JSON内容", true); return; }
            try { JsonDocument.Parse(input); ShowStatus("✓ JSON格式有效"); }
            catch (JsonException ex) { ShowStatus("✗ JSON格式错误: " + ex.Message, true); }
        }

        private void SortClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入JSON内容", true); return; }
            try
            {
                var doc = JsonDocument.Parse(input);
                var dict = new System.Collections.Generic.SortedDictionary<string, JsonElement>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                    dict[prop.Name] = prop.Value.Clone();
                var obj = JsonSerializer.Serialize(dict);
                var doc2 = JsonDocument.Parse(obj);
                SetOutput(JsonSerializer.Serialize(doc2, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) }));
                ShowStatus("排序成功");
            }
            catch (Exception ex) { ShowStatus("排序失败: " + ex.Message, true); }
        }

        private void GenerateCSharpClick(object sender, RoutedEventArgs e)
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入JSON内容", true); return; }
            try
            {
                var doc = JsonDocument.Parse(input);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) { ShowStatus("JSON必须为对象类型", true); return; }
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
                SetOutput(sb.ToString());
                ShowStatus("C#实体生成成功");
            }
            catch (Exception ex) { ShowStatus("生成失败: " + ex.Message, true); }
        }

        private void ClearClick(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "";
            OutputTextBox.Text = "";
            lastOutput = "";
            ShowStatus("已清空");
        }

        private static string GetCSharpType(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "double",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Array => "List<object>",
            JsonValueKind.Object => "object",
            JsonValueKind.Null => "object",
            _ => "string"
        };

        private static string ToPascalCase(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + (s.Length > 1 ? s[1..] : "");

        private void TextModeClick(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Visibility = Visibility.Visible;
            TreeViewScroller.Visibility = Visibility.Collapsed;
        }

        private void TreeModeClick(object sender, RoutedEventArgs e)
        {
            RenderTreeView();
        }

        private void RenderTreeView()
        {
            var input = GetInput();
            if (string.IsNullOrEmpty(input)) { ShowStatus("请输入JSON内容", true); return; }
            try
            {
                var doc = JsonDocument.Parse(input);
                JsonTreeView.Items.Clear();
                JsonTreeView.Items.Add(CreateTreeNode("root", doc.RootElement));
                TreeViewScroller.Visibility = Visibility.Visible;
                OutputTextBox.Visibility = Visibility.Collapsed;
                ShowStatus("视图渲染成功");
            }
            catch (Exception ex) { ShowStatus("渲染失败: " + ex.Message, true); }
        }

        private TreeViewItem CreateTreeNode(string key, JsonElement element)
        {
            var node = new TreeViewItem { Header = GetNodeDisplay(key, element), IsExpanded = true };
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                        node.Items.Add(CreateTreeNode(prop.Name, prop.Value));
                    break;
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                        node.Items.Add(CreateTreeNode($"[{index++}]", item));
                    break;
            }
            return node;
        }

        private string GetNodeDisplay(string key, JsonElement element)
        {
            string value = element.ValueKind switch
            {
                JsonValueKind.String => $"\"{element.GetString()}\"",
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                JsonValueKind.Object => "{...}",
                JsonValueKind.Array => "[...]",
                _ => element.GetRawText()
            };
            return $"{key}: {value}";
        }
    }
}