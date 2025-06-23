begin;

alter type "UserActionType" add value 'RatingReceived';
alter table "UserActivity" add column "Value" int;

commit;