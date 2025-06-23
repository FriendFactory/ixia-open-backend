begin;

create table if not exists "FeatureFlag"
(
    "Id"              bigint generated always as identity primary key,
    "Name"            text not null unique,
    "Description"     text,
    "Value"           text not null,
    "AvailableValues" text[]
);

commit;