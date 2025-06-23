begin;

alter table "AiWorkflowPrice"
    rename to "AiWorkflowMetadata";

alter table "AiWorkflowMetadata"
    add column "EstimatedLoadingTimeSec" int null;

commit;