begin;

alter table "Views"
    add "FeedType" text null,
    add "FeedTab"  text null;

commit;