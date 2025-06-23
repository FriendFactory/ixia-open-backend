begin;

create extension if not exists unaccent;

CREATE OR REPLACE FUNCTION public.f_unaccent(text)
    RETURNS text
    LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT AS
$func$
SELECT public.unaccent('public.unaccent', $1)
$func$;

create index if not exists idx_trgm_external_song_artist_name_unaccent ON public."ExternalSong" USING gin (f_unaccent("ArtistName") public.gin_trgm_ops) WHERE ((NOT "IsDeleted") AND (NOT "IsManuallyDeleted") AND ("NotClearedSince" IS NULL));

create index if not exists idx_trgm_external_song_song_name_unaccent ON public."ExternalSong" USING gin (f_unaccent("SongName") public.gin_trgm_ops) WHERE ((NOT "IsDeleted") AND (NOT "IsManuallyDeleted") AND ("NotClearedSince" IS NULL));

commit;