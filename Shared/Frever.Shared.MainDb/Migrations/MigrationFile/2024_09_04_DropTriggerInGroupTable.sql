begin;

drop trigger if exists update_birthday_day on "Group";
drop function if exists public.update_birthday_day;

commit;