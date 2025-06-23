begin;

alter table "InAppProduct"
    add "IsFreeProduct" bool not null default (false),
    alter "InAppProductPriceTierId" drop not null;

commit;