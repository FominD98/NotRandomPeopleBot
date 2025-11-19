using System.Text.Json;
using Serilog.Sinks.Http;

namespace AiTelegramBot.Logging;

public class LokiFormatter : IBatchFormatter
{
    private readonly string _serviceName;
    private readonly string _environment;

    public LokiFormatter(string serviceName, string environment)
    {
        _serviceName = serviceName;
        _environment = environment;
    }

    public void Format(IEnumerable<string> logEvents, TextWriter output)
    {
        var streams = new List<object>();
        var values = new List<object[]>();

        foreach (var logEvent in logEvents)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000;
            values.Add(new object[] { timestamp.ToString(), logEvent });
        }

        if (values.Count > 0)
        {
            streams.Add(new
            {
                stream = new Dictionary<string, string>
                {
                    { "app", _serviceName },
                    { "environment", _environment }
                },
                values = values
            });
        }

        var payload = new { streams = streams };
        var json = JsonSerializer.Serialize(payload);
        output.Write(json);
    }
}
