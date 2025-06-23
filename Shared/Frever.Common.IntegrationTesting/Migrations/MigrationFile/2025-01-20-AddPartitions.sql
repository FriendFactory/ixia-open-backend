begin;

create table if not exists public."ChatMessageLike_2030" partition of "ChatMessageLike"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."CommentLikes_2030" partition of "CommentLikes"
    FOR VALUES FROM ('2026-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."ChatMessage_2030" partition of "ChatMessage"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."Comments_2030" partition of "Comments"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."GranularLikes_2030" partition of "GranularLikes"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."Likes_2030" partition of "Likes"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."Remixes_2030" partition of "Remixes"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."Shares_2030" partition of "Shares"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

create table if not exists public."Views_2030" partition of "Views"
    FOR VALUES FROM ('2025-01-01 00:00:00+00') TO ('2030-01-01 00:00:00+00');

commit;