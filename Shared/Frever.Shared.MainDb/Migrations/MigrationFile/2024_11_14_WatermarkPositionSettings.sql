begin;

alter table "Watermark"
    add column "Positions" text;

update "Watermark"
set "Positions" = '[{"videoOrientation":1,"offsetX":0.732,"offsetY":0.63,"scale":0.17,},' ||
                  '{"videoOrientation":2,"offsetX":0.07,"offsetY":0.1,"scale":0.05}]'
where "Positions" is null;

commit;