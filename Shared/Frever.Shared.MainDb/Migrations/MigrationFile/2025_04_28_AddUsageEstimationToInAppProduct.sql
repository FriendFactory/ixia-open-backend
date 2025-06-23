begin;

alter table "InAppProduct"
    add "UsageEstimation" json null;

commit;