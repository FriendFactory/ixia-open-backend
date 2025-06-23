namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public class DateSequence(DateTime? initialDate = null)
{
    private DateTime _currentDate = initialDate ?? DateTime.Now;

    public DateTime NextMonth()
    {
        _currentDate = _currentDate.AddMonths(1);
        return _currentDate;
    }

    public DateTime NextDay()
    {
        _currentDate = _currentDate.AddDays(1);
        return _currentDate;
    }

    public DateTime NextHour()
    {
        _currentDate = _currentDate.AddHours(1);
        return _currentDate;
    }

    public DateTime NextMoment()
    {
        _currentDate = _currentDate.AddSeconds(Random.Shared.Next(1, 10));
        return _currentDate;
    }

    public static implicit operator DateTime(DateSequence d) => d._currentDate;
}