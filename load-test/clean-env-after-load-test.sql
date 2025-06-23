begin;

delete
from "Level"
where "Id" in (
    select l."Id"
    from "Level" l
    where l."GroupId" in
          (
              select ug."GroupId"
              from "UserAndGroup" ug
              where ug."UserId" in (
                  select u."Id"
                  from "User" u
                  where u."Email" = ANY
                        ('{xxxxxxxxx,bcc35@1.com,bcc26@1.com,bcc3@1.com,bcc5@1.com,bcc2@1.com,bcc@1.com,bcc9@1.com,bcc10@1.com,bcc13@1.com,bcc19@1.com,bcc14@1.com,bcc17@1.com,bcc16@1.com,bcc18@1.com,bcc20@1.com,bcc21@1.com,bcc22@1.com,bcc24@1.com,bcc25@1.com,bcc30@1.com,bcc27@1.com,bcc28@1.com,bcc7@1.com,bcc8@1.com,bcc6@1.com,bcc29@1.com,bcc31@1.com,bcc32@1.com,bcc11@1.com,bccooo@1.com,bcc123@1.com,bcc85@1.com,bcc6@2.com,bcc@12.com,bcc678@1.com,bcc666@1.com}')
              )
          ))
;



delete
from "UserAndGroup"
where "UserId" in (
    select "Id"
    from "User"
    where "Email" like 'loadTest%');

delete
from "User"
where "Email" like 'loadTest%';

delete
from "Group"
where "Id" not in (select "GroupId"
                   from "UserAndGroup"
                   union
                   select "GroupId"
                   from "Character"
                   union
                   select "MainGroupId"
                   from "User"
);


commit;

begin;

delete from "AspNetUsers" where "UserName" like 'loadTest%';

commit;