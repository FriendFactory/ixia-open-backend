begin;

alter type "AssetStoreTransactionType" add value 'VideoRating';
alter type "UserActionType" add value 'VideoRaterRewardClaimed';
alter type "UserActionType" add value 'RatedVideoRewardClaimed';
alter type "NotificationType" add value 'VideoRatingCompleted';

alter table "Video"
    add column "AvailableForRating" boolean default false not null,
    add column "RatingCompleted"    boolean default false not null;

create index if not exists idx_video_available_for_rating on "Video" ("AvailableForRating");
create index if not exists idx_video_rating_completed on "Video" ("RatingCompleted");

create table if not exists "VideoRating"
(
    "Id"           bigint generated always as identity primary key,
    "VideoId"      bigint not null references "Video",
    "GroupId"      bigint references "Group",
    "RaterLevelId" bigint references "Level",
    "Rating"       int    not null,
    "Time"         timestamp with time zone default CURRENT_TIMESTAMP not null
);

create index idx_video_rating_rater_video_id on "VideoRating" ("RaterLevelId");
create index idx_video_rating_video_id_group_id on "VideoRating" ("VideoId", "GroupId");
create index idx_video_rating_video_id on "VideoRating" ("VideoId");

commit;