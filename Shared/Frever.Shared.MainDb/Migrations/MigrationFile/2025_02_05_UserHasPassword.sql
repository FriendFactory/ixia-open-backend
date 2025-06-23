begin;

alter table "User"
    add column "HasPassword" bool not null default false;

update "User" u
set "HasPassword" = true
from "Group" g
where g."Id" = u."MainGroupId"
  and u."Email" is null
  and u."PhoneNumber" is null
  and u."AppleId" is null
  and u."GoogleId" is null
  and g."IsTemporary" = false
  and g."DeletedAt" is null;

commit;