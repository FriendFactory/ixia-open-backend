begin;

alter table "AiGeneratedContent"
    add column "GenerationStatus" text default 'Completed',
    add column "GenerationKey" text;

alter table "AiGeneratedContent" alter column "GenerationStatus" drop default;

commit;