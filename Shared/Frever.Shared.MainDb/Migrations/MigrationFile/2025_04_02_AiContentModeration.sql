begin;

alter table "AiGeneratedImage"
    add "IsModerationPassed" bool not null default (false),
    add "ModerationResult" json null;

alter table "AiGeneratedVideo"
    add "IsModerationPassed" bool not null default (false),
    add "ModerationResult" json null;

alter table "AiGeneratedVideoClip"
    add "IsModerationPassed" bool not null default (false),
    add "ModerationResult" json null;

commit;