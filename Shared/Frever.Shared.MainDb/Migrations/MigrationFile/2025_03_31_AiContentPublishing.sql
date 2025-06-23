begin;

alter table "AiGeneratedContent"
    add "Status"           varchar(128) not null default 'Draft', -- Draft, Published (probably Template in future)
    add "DraftAiContentId" bigint       null references "AiGeneratedContent" ("Id"); -- AiContent draft used to produce published content


update "AiGeneratedContent"
set "Status" = 'Published'
where "Id" in
      (select "Id"
       from "AiGeneratedContent" c
       where exists (select 1
                     from "Video" v
                     where v."AiContentId" = c."Id"));

commit;