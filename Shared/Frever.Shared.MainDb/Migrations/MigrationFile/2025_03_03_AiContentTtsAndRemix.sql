begin;

alter table "AiGeneratedContent"
    add "RemixedFromAiGeneratedContentId" bigint null references "AiGeneratedContent" ("Id");

alter table "AiGeneratedVideoClip"
    add "Tts" text null;

alter table "AiGeneratedVideo"
    add "Tts" text null;

alter table "AiGeneratedImage"
    add "AiArtStyleId" bigint null references "AiArtStyle" ("Id");

commit;