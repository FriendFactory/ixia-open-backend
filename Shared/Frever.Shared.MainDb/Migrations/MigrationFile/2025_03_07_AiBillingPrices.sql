begin;


insert into "AiWorkflowPrice" ("AiWorkflow", "Description", "IsActive", "RequireBillingUnits", "HardCurrencyPrice")
values ('flux-prompt', 'Flux Prompt', false, false, 0),
       ('flux-photo-prompt', 'Flux Photo', false, false, 0),
       ('photo-pulid-2', 'Pulid Two Chars', false, false, 0),
       ('photo-pulid-3', 'Pulid Three Chars', false, false, 0),
       ('flux-photo-redux-style', 'Flux Photo Redux Style', false, false, 0),
       ('photo-make-up-thumbnails', 'Photo Makeup', false, false, 0),
       ('photo-make-up-eyebrows-thumbnails', 'Photo Makeup Eye Brows', false, false, 0),
       ('photo-make-up-eyelashes-eyeshadow-thumbnails', 'Photo Makeup Eye Lashes', false, false, 0),
       ('photo-make-up-lips-thumbnails', 'Photo Makeup Lips', false, false, 0),
       ('photo-make-up-skin-thumbnails', 'Photo Makeup Skin', false, false, 0),
       ('photo-ace-plus', 'Ace Plus', false, false, 0),
       ('sonic-text', 'Sonic Text', false, false, 0),
       ('sonic-audio', 'Sonic Audio', false, false, 0),
       ('latent-sync', 'Lip Sync', false, false, 0),
       ('latent-sync-text', 'Lip Sync Text', false, false, 0),
       ('video-on-output-audio', 'Audio To Video', false, false, 0),
       ('video-on-output-text', 'Text To Video', false, false, 0)
;

commit;