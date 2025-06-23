with ins as (
    insert into "Follower" ("FollowingId", "FollowerId", "State", "IsMutual", "Time")
        select :followsGroupId,
               :groupId,
               'Following',
               false,
               :time
        from "Group" a
        where not exists (select 1
                          from "Follower" f
                          where f."FollowingId" = :followsGroupId
                            and f."FollowerId" = :groupId)
        limit 1
        returning *)
update "Follower"

set "IsMutual" = true
where exists (select 1
              from "Follower" f
              where f."FollowerId" = "Follower"."FollowingId"
                and f."FollowingId" = "Follower"."FollowerId")
;