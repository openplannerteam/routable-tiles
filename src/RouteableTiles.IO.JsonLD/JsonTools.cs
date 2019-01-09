using System.Text;

namespace RouteableTiles.IO.JsonLD
{
    /// <summary>
    /// Contains some tools for json.
    /// </summary>
    internal static class JsonTools
    {
        /// <summary>
        /// Escape a string.
        /// </summary>
        internal static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            int i;
            var len = s.Length;
            var sb = new StringBuilder(len + 4);

            for (i = 0; i < len; i += 1)
            {
                var c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            var t = "000" + string.Format("X", c);
                            sb.Append("\\u" + t.Substring(t.Length - 4));
                        }
                        else {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}