begin;

create table if not exists "Watermark"
(
    "Id"                bigint generated always as identity primary key,
    "Name"              text,
    "FilesInfo"         json,
    "DurationSeconds"   integer default 0 not null
);

alter table "Group" add column "DisableWatermark" boolean default false not null;
alter table "IntellectualProperty" add column "WatermarkId" bigint references "Watermark";

commit;