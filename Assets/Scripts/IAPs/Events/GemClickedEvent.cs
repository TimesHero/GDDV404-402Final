using Unity.Services.Analytics;

//create an instance of this class whenever you need to record this event.
public class GemClickedEvent : Event
{
    //same as the name in Unity Services
    public GemClickedEvent() : base("gemClicked") { }

    public string GemColor { set { SetParameter("gemColor", value); } }
}
