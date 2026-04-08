using Unity.Services.Analytics;

//create an instance of this class whenever you need to record this event.
public class BoughtGemsEvent : Event
{
    //same as the name in Unity Services
    public BoughtGemsEvent() : base("boughtGems") { }

    public int GemAmountPurchased { set { SetParameter("gemAmountPurchased", value); } }
}
