begin;

alter type "NotificationType" add value 'AiContentGenerated';
alter table "Notification" add column "DataAiContentId" bigint;

commit;