begin;

alter table "Video" add column "DeletedByGroupId" bigint references "Group";

commit;