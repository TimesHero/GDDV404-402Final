using Unity.Services.Analytics;

//create an instance of this class whenever you need to record this event.
public class GemAdViewedEvent : Event
{
    //same as the name in Unity Services
    public GemAdViewedEvent() : base("gemAdViewed") { }

    public string GemAdTypeWatched { set { SetParameter("gemAdTypeWatched", value); } }
}