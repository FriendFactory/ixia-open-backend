begin;

delete
from "BodyAnimationAndVfx"
where ctid not in (select MIN(ctid)
                   from "BodyAnimationAndVfx"
                   group by "BodyAnimationId");

delete
from "BodyAnimationAndVfx"
where ctid not in (select min(ctid)
                   from "BodyAnimationAndVfx"
                   group by "VfxId");

alter table "BodyAnimationAndVfx"
    add constraint uq_body_animation_and_vfx_body_animation_id unique ("BodyAnimationId");

alter table "BodyAnimationAndVfx"
    add constraint uq_body_animation_and_vfx_vfx_id unique ("VfxId");

commit;