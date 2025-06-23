with wd as (select *
            from "Wardrobe"
            where "GroupId" = 1
              and "ReadinessId" = 2),
     gg as (select "WardrobeGenderGroupId"
            from wd
            group by "WardrobeGenderGroupId"
            having count("WardrobeGenderGroupId") > 1)
select *
from wd
where exists (select 1 from gg where gg."WardrobeGenderGroupId" = wd."WardrobeGenderGroupId")

