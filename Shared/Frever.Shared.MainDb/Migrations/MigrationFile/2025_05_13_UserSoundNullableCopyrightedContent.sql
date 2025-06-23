begin;

alter table "UserSound" alter column "ContainsCopyrightedContent" drop default;

commit;