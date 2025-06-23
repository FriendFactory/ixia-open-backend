begin;

alter type "UserActionType" add value 'PublishedVideoShare';
alter type "AssetStoreTransactionType" add value 'PublishedVideoShare';

commit;