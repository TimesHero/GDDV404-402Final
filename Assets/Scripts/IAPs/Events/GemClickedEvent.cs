using Unity.Services.Analytics;

public class GemClickedEvent : Event
{
    public GemClickedEvent() : base("gemClicked") { }

    public string GemColor { set { SetParameter("gemColor", value); } }
}
