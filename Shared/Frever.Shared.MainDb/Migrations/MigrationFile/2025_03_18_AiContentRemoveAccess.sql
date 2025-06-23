begin;

alter table "AiGeneratedContent"
    drop column "Access";

create index if not exists IDX_Video_AiContent
    on "Video" ("AiContentId");

commit;