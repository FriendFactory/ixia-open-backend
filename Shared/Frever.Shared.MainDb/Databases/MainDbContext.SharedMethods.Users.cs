using System;
using System.Linq;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Shared.MainDb;

public partial class MainDbContext
{
    public IQueryable<FriendInfo> GetRankedFriendList(long groupId)
    {
        var sql = $"""
                   with inner_friends as (select f."FollowingId" group_id,
                                                 f."Time"        followed_at
                                          from "Follower" f
                                          where f."FollowerId" = {groupId}
                                            and f."IsMutual"
                                            and not exists(select 1
                                                           from "BlockedUser" b
                                                           where (b."BlockedByUserId" = {groupId} and b."BlockedUserId" = f."FollowingId")
                                                              or (b."BlockedUserId" = {groupId} and b."BlockedByUserId" = f."FollowingId"))),
                        inner_group_posted_video as (select *
                                                     from inner_friends g
                                                     where exists (select 1
                                                                   from "Video" v
                                                                   where v."GroupId" = g.group_id
                                                                     and not v."IsDeleted"
                                                                     and v."Access" = 'Public'
                                                                     and v."CreatedTime" > (current_timestamp - interval '30 day'))),
                        inner_group_not_posted_video as (select *
                                                         from inner_friends g
                                                         where not exists(select 1
                                                                          from inner_group_posted_video i
                                                                          where i.group_id = g.group_id)),
                        inner_tag_info as (select f.group_id,
                                                  v."CreatedTime" tagged_at,
                                                  v."Id"          video_id
                                           from "VideoGroupTag" t
                                                    inner join
                                                "Video" v on t."VideoId" = v."Id"
                                                    inner join
                                                inner_friends f on t."GroupId" = f.group_id
                                           where v."GroupId" = {groupId} and t."IsCharacterTag" = true),
                        last_friends as (select f.*
                                         from inner_friends f
                                         where f.followed_at > (CURRENT_TIMESTAMP - INTERVAL '9 DAY')
                                         order by f.followed_at desc
                                         limit 3),
                        last_active_tagged as (select f.*,
                                                      (select count(t.video_id)
                                                       from inner_tag_info t
                                                       where t.group_id = f.group_id) tag_count,
                                                      (select max(t.tagged_at)
                                                       from inner_tag_info t
                                                       where t.group_id = f.group_id) last_tagged_at
                                               from inner_group_posted_video f
                                               where exists (select 1 from inner_tag_info ti where ti.group_id = f.group_id)
                                                 and not exists (select 1 from last_friends lf where lf.group_id = f.group_id)
                                               order by tag_count desc, last_tagged_at desc nulls last, followed_at desc),
                        last_active_not_tagged as (select f.*
                                                   from inner_group_posted_video f
                                                   where not exists (select 1 from inner_tag_info ti where ti.group_id = f.group_id)
                                                     and not exists (select 1 from last_friends lf where lf.group_id = f.group_id)
                                                   order by followed_at desc),
                        last_non_active as (select f.*
                                            from inner_group_not_posted_video f
                                            where not exists (select 1 from last_friends lf where lf.group_id = f.group_id)
                                            order by f.followed_at desc),
                        final_result as (select t.group_id,
                                                t.followed_at,
                                                true                                is_new_friend,
                                                100000000 - row_number() over () as rank,
                                                'last_friends'                      src
                                         from last_friends t
                                         union all
                                         select t.group_id,
                                                t.followed_at,
                                                false                           is_new_friend,
                                                10000000 - row_number() over () rank,
                                                'active_tagged'                 src
                                         from last_active_tagged t
                                         union all
                                         select t.group_id,
                                                t.followed_at,
                                                false                          is_new_friend,
                                                1000000 - row_number() over () rank,
                                                'active_not_tagged'            src
                                         from last_active_not_tagged t
                                         union all
                                         select t.group_id,
                                                t.followed_at,
                                                false                         is_new_friend,
                                                100000 - row_number() over () rank,
                                                'non_active'                  src
                                         from last_non_active t)
                   select r.group_id      "GroupId",
                          r.is_new_friend "IsNew",
                          r.followed_at   "FollowedAt",
                          g."IsMinor",
                          r.src           "RankingSrc",
                          r.rank          "Rank",
                          g."NickName",
                          g."CharacterAccess"
                   from final_result r
                            inner join "Group" g on r.group_id = g."Id"
                            inner join "User" u on u."MainGroupId" = g."Id"
                   where g."DeletedAt" is null and
                         g."IsBlocked" = false
                   order by rank desc
                   """;

        return Database.SqlQueryRaw<FriendInfo>(sql);
    }
}

public class FriendInfo
{
    public long GroupId { get; set; }
    public string NickName { get; set; }
    public CharacterAccess CharacterAccess { get; set; }
    public bool IsNew { get; set; }
    public DateTime FollowedAt { get; set; }
    public bool IsMinor { get; set; }
    public string RankingSrc { get; set; }
    public int Rank { get; set; }

}