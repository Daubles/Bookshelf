using NpgsqlTypes;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace Bookshelf.Logging;

/// <summary>Column layout of the <c>logs</c> table written by the Serilog PostgreSQL sink.</summary>
public static class LogColumns
{
    public const string TableName = "logs";

    public static IDictionary<string, ColumnWriterBase> Writers { get; } = new Dictionary<string, ColumnWriterBase>
    {
        ["timestamp"] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
        ["level"] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
        // "l" writes string values without the quotes Serilog adds by default
        ["source_context"] = new SinglePropertyColumnWriter("SourceContext", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l"),
        ["message"] = new UnquotedMessageColumnWriter(),
        ["message_template"] = new MessageTemplateColumnWriter(),
        ["exception"] = new ExceptionColumnWriter(),
        ["properties"] = new PropertiesColumnWriter(NpgsqlDbType.Jsonb),
    };

    /// <summary>Renders the message like the console does, without quoting string property values.</summary>
    private sealed class UnquotedMessageColumnWriter() : ColumnWriterBase(NpgsqlDbType.Text)
    {
        public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null)
        {
            using var writer = new StringWriter();
            new MessageTemplateTextFormatter("{Message:l}", formatProvider).Format(logEvent, writer);
            return writer.ToString();
        }
    }
}
