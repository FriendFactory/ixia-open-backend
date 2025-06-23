begin;

alter table "Video"
    add column "RatingCompletedAt" timestamp with time zone;

create index if not exists idx_video_rating_completed_at on "Video" ("RatingCompletedAt");

update "Video"
set "RatingCompletedAt" = "CreatedTime" + INTERVAL '1 hours'
where "RatingCompleted" = true;

commit;