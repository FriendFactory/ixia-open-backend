begin;

alter table "InAppPurchaseOrder"
    add "RefInAppProductId" bigint null;

commit;