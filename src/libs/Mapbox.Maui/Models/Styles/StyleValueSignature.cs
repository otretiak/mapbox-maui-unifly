using System.Collections;
using System.Globalization;
using System.Text;
using MapboxMaui.Expressions;

namespace MapboxMaui.Styles;

/// Builds a deterministic text form of a style object's properties, so an unchanged layer can be
/// recognised without converting it to native values.
internal static class StyleValueSignature
{
    public static string Of(BaseKVContainer container)
    {
        var builder = new StringBuilder();
        Append(builder, container.Properties);

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, object value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case string text:
                builder.Append('"').Append(text).Append('"');
                break;
            case bool flag:
                builder.Append(flag ? "true" : "false");
                break;
            case INamedString named:
                builder.Append('"').Append(named.Value).Append('"');
                break;
            case IPropertyValue propertyValue:
                Append(builder, propertyValue.Value);
                break;
            case DslExpression expression:
                Append(builder, expression.ToObjects());
                break;
            case Color color:
                builder.Append(color.ToRgbaHex(true));
                break;
            case Enum enumValue:
                builder.Append(enumValue.ToString());
                break;
            case double number:
                builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float number:
                builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                break;
            case IConvertible convertible when value.GetType().IsPrimitive || value is decimal:
                builder.Append(convertible.ToString(CultureInfo.InvariantCulture));
                break;
            case IDictionary dictionary:
                builder.Append('{');
                foreach (var key in dictionary.Keys.Cast<object>().OrderBy(key => key.ToString(), StringComparer.Ordinal))
                {
                    builder.Append(key).Append(':');
                    Append(builder, dictionary[key]);
                    builder.Append(',');
                }
                builder.Append('}');
                break;
            case IEnumerable items:
                builder.Append('[');
                foreach (var item in items)
                {
                    Append(builder, item);
                    builder.Append(',');
                }
                builder.Append(']');
                break;
            default:
                builder.Append(value.GetType().Name).Append('(').Append(value).Append(')');
                break;
        }
    }
}
