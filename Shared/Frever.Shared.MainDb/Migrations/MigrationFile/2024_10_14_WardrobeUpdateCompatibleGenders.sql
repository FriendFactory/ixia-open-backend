update "Wardrobe"
set "CompatibleGenderIds" =
        case
            when "GenderId" = 2 then ARRAY [2, 3]::bigint[]
            when "GenderId" = 3 then ARRAY [1, 2, 3]::bigint[]
            else ARRAY ["GenderId"]
            end;
