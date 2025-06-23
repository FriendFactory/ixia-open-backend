begin;

alter table "SetLocation"
    add "IsExcludedFromLists" bool default false;

commit;