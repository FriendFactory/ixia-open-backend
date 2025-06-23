namespace Frever.Video.Contract;

public class VideoSharingInfo
{
    public string SharedPlayerUrl { get; set; }
    public int RewardedShareCount { get; set; }
    public int CurrentShareCount { get; set; }
    public int SoftCurrency { get; set; }
}