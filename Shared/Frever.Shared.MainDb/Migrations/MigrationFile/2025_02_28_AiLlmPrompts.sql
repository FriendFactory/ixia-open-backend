begin;

create table "AiLlmPrompt"
(
    "Id"     bigint not null generated always as identity primary key,
    "Key"    text   not null unique,
    "Prompt" text   not null
);

create table "AiOpenAiKey"
(
    "Id"     bigint not null generated always as identity primary key,
    "ApiKey" text   not null unique
);

create table "AiOpenAiAgent"
(
    "Id"    bigint not null generated always as identity primary key,
    "Key"   text   not null,
    "Agent" text   not null
);


commit;