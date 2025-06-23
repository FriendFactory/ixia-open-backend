begin;

alter table "InAppUserSubscription"
    add "InAppPurchaseOrderId" uuid not null references "InAppPurchaseOrder"("Id");

commit;