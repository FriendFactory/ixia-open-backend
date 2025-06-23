namespace Frever.Video.Core.Features.Comments.DataAccess;

public static class Sql
{
    public const string BasicCommentsSql = """
                                           select
                                               c."Id",
                                               c."VideoId",
                                               c."Time",
                                               c."GroupId",
                                               c."Text",
                                               c."IsDeleted",
                                               c."Mentions",
                                               c."Thread"::text "Thread",
                                               c."ReplyToCommentId",
                                               c."ReplyCount",
                                               c."LikeCount",
                                               c."IsPinned"
                                           from "Comments" c
                                           where "VideoId" = {0} and
                                                 c."IsDeleted" = false and
                                                 not exists(
                                                       select 1 from "Group" g
                                                       where g."Id" = c."GroupId" and (
                                                           g."IsBlocked" = true or
                                                           g."DeletedAt" is not null
                                                       )
                                           )
                                           """;

    public const string CommentGroupInfo = """
                                           select c."Id" "CommentId",
                                                  c."GroupId",
                                                  g."NickName",
                                                  g."CreatorScoreBadge"
                                           from "Comments" c
                                                inner join "Group" g on c."GroupId" = g."Id"
                                           where c."VideoId" = {0}
                                             and c."IsDeleted" = false
                                             and g."IsBlocked" = false
                                             and g."DeletedAt" is null
                                           """;
}