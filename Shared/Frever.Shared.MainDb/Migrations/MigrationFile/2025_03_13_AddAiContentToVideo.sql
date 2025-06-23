begin;

alter table "Video"
    add "AiContentId" bigint null references "AiGeneratedContent" ("Id");


commit;