using System.ComponentModel.DataAnnotations.Schema;

namespace Frever.Shared.MainDb.Entities;

[Table("video_kpi", Schema = "stats")]
public class VideoKpi
{
    [Column("video_id")] public long VideoId { get; set; }
    [Column("likes")] public long Likes { get; set; }
    [Column("views")] public long Views { get; set; }
    [Column("comments")] public long Comments { get; set; }
    [Column("shares")] public long Shares { get; set; }
    [Column("remixes")] public long Remixes { get; set; }
    [Column("battles_won")] public long BattlesWon { get; set; }
    [Column("battles_lost")] public long BattlesLost { get; set; }
}