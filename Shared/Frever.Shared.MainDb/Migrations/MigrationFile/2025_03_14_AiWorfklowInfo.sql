begin;

alter table "AiGeneratedImage"
    add "Workflow" text null;

alter table "AiGeneratedVideoClip"
    add "Workflow" text null;

alter table "AiGeneratedVideo"
    add "Workflow" text null;

alter table "AiGeneratedVideoClip"
    alter column "AiGeneratedImageId" drop not null;

commit;