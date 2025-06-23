begin;

alter table "AiGeneratedVideo" add column "SongId" bigint;
alter table "AiGeneratedContent" add column "GenerationParameters" text;

commit;