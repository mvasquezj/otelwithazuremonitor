using System.Diagnostics;
using OpenTelemetry;

namespace ServiceDefault;

public class BaggageToTagsProcessor: BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        foreach (var item in Baggage.Current.GetBaggage())
        {
            data.SetTag(item.Key, item.Value);
        }
    }

    public override void OnEnd(Activity data)
    {
        foreach (var item in Baggage.Current.GetBaggage())
        {
            data.SetTag(item.Key, item.Value);
        }
    }
}