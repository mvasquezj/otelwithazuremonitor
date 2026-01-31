using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace ApiWithAzureMonitorOtel;

public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null)
            return;

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("trace_id", activity.TraceId.ToString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("span_id", activity.SpanId.ToString()));

        if (activity.ParentSpanId != default)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("parent_span_id", activity.ParentSpanId.ToString()));
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("OperationName", activity.DisplayName));
    }
}