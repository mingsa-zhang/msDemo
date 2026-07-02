using System;
using System.Text;

namespace CoreDemo2.Services
{
    public interface IStringEscapeService
    {
        string Escape(string input);
        string Unescape(string input);
    }

    public class StringEscapeService : IStringEscapeService
    {
        public string Escape(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // 使用JSON转义规则
            return Newtonsoft.Json.JsonConvert.SerializeObject(input).Trim('"');
        }

        public string Unescape(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            try
            {
                // 将转义后的字符串包装成JSON字符串格式，然后反序列化
                string wrappedJson = $"\"{input}\"";
                return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(wrappedJson);
            }
            catch
            {
                // 如果JSON反序列化失败，使用C#的字符串字面量转义
                return System.Text.RegularExpressions.Regex.Unescape(input);
            }
        }
    }
}