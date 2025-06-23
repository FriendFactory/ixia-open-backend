begin;

alter table "Vfx"
    add column "AnchorPoint" text;

update "Vfx"
set "AnchorPoint" =
        case "VfxCategoryId"
            when 7 then 'right_hand'
            when 8 then 'left_hand'
            when 9 then 'spine'
            else 'head'
            end
where "AnchorPoint" is null
  and "VfxCategoryId" in (5, 6, 7, 8, 9);

commit;