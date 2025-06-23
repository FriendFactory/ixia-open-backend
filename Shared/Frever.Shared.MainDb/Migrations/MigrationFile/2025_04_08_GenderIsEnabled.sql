begin;

alter table "Gender"
    add "IsEnabled" bool not null default false;

update "Gender"
set "IsEnabled" = true
where "Id" in (1, 2, 3);

commit;