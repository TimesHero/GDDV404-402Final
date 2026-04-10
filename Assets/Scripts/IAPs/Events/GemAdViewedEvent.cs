using Unity.Services.Analytics;

public class GemAdViewedEvent : Event
{
    public GemAdViewedEvent() : base("gemAdViewed") { }

    public string GemAdTypeWatched { set { SetParameter("gemAdTypeWatched", value); } }
}
