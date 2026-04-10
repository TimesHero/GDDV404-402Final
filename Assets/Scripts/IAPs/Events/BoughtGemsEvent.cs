using Unity.Services.Analytics;

public class BoughtGemsEvent : Event
{
    public BoughtGemsEvent() : base("boughtGems") { }

    public int GemAmountPurchased { set { SetParameter("gemAmountPurchased", value); } }
}
