begin;

alter type "AssetStoreTransactionType" add value 'AiWorkflowRun';
alter type "AssetStoreTransactionType" add value 'AiWorkflowRunErrorRefund';

alter table "AssetStoreTransaction"
    add "AiWorkflow"             text  null,
    add "AiWorkflowBillingUnits" float null;

create table "AiWorkflowPrice"
(
    "Id"                  bigint not null generated always as identity primary key,
    "AiWorkflow"          text   not null,
    "Description"         text       null,
    "RequireBillingUnits" bool   not null,
    "IsActive"            bool   not null default (true),
    "HardCurrencyPrice"   int    not null,
    "Key"                 text   unique,
    unique ("AiWorkflow", "Key")
);

commit;