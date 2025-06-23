begin;

alter table "AiGeneratedImage"
    alter column "Prompt" drop not null,
    alter column "ShortPromptSummary" drop not null;

alter table "AiGeneratedVideoClip"
    alter column "Prompt" drop not null,
    alter column "ShortPromptSummary" drop not null;

commit;