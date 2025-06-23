begin;

alter table "AiMakeUp" add "Files" json null;
alter table "AiMakeUp" alter column "FilesInfo" drop not null;
alter table "InAppProduct" add "Files" json null;
alter table "InAppProductDetails" add "Files" json null;
alter table "PromotedSong" add "Files" json null;
alter table "Song" add "Files" json null;
alter table "UserSound"
    add "Files" json null,
    add "ModifiedTime" timestamp with time zone default CURRENT_TIMESTAMP not null;

update "UserSound"
set "Files" = json_build_array(
        json_build_object(
                'type', 'main',
                'version', "FilesInfo" -> 0 -> 'version',
                'path',
                'Assets/UserSound/' || ("Id"::text) || '/Main/iOS/' || ("FilesInfo" -> 0 ->> 'version') || '/content.mp3'
        )
)
where "Files" is null;

update "Song"
set "Files" =
        json_build_array(
                json_build_object(
                        'version', "FilesInfo" -> 0 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 0 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 0 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 0 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Main/' || ("FilesInfo" -> 0 ->> 'version') ||
                                '/content.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail/' || ("FilesInfo" -> 0 ->> 'resolution') ||
                                 '/' ||
                                 ("FilesInfo" -> 0 ->> 'version') || '/content.png'
                            end
                ),
                json_build_object(
                        'version', "FilesInfo" -> 1 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 1 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 1 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 1 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Main/' || ("FilesInfo" -> 1 ->> 'version') ||
                                '/content.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail/' || ("FilesInfo" -> 1 ->> 'resolution') ||
                                 '/' ||
                                 ("FilesInfo" -> 1 ->> 'version') || '/content.png'
                            end
                ),
                json_build_object(
                        'version', "FilesInfo" -> 2 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 2 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 2 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 2 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Main/' || ("FilesInfo" -> 2 ->> 'version') ||
                                '/content.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail/' || ("FilesInfo" -> 2 ->> 'resolution') ||
                                 '/' ||
                                 ("FilesInfo" -> 2 ->> 'version') || '/content.png'
                            end
                ),
                json_build_object(
                        'version', "FilesInfo" -> 3 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 3 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 3 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 3 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Main/' || ("FilesInfo" -> 3 ->> 'version') ||
                                '/content.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail/' || ("FilesInfo" -> 3 ->> 'resolution') ||
                                 '/' ||
                                 ("FilesInfo" -> 3 ->> 'version') || '/content.png'
                            end
                )
        )
where "CreatedTime" > '2021-06-01';

update "Song"
set "Files" =
        json_build_array(
                json_build_object(
                        'version', "FilesInfo" -> 0 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 0 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 0 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 0 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Audio.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail_' || ("FilesInfo" -> 0 ->> 'resolution') ||
                                 '.png'
                            end
                ),
                json_build_object(
                        'version', "FilesInfo" -> 1 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 1 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 1 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 1 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Audio.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail_' || ("FilesInfo" -> 1 ->> 'resolution') ||
                                 '.png'
                            end
                ),
                json_build_object(
                        'version', "FilesInfo" -> 2 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 2 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 2 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 2 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Audio.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail_' || ("FilesInfo" -> 2 ->> 'resolution') ||
                                 '.png'
                            end
                ),
                json_build_object(
                        'version', "FilesInfo" -> 3 -> 'version',
                        'type',
                        case
                            when "FilesInfo" -> 3 ->> 'file' = 'MainFile' then 'main'
                            else 'thumbnail' || substring("FilesInfo" -> 3 ->> 'resolution' from 1 for 3)
                            end,
                        'path',
                        case
                            when "FilesInfo" -> 3 ->> 'file' = 'MainFile' then
                                'Assets/Song/' || "Id"::text || '/Audio.mp3'
                            else 'Assets/Song/' || "Id"::text || '/Thumbnail_' || ("FilesInfo" -> 3 ->> 'resolution') ||
                                 '.png'
                            end
                )
        )
where "CreatedTime" <= '2021-06-01';

update "AiMakeUp"
set "Files" = json_build_array(
        json_build_object(
                'version', "FilesInfo" -> 0 -> 'version',
                'type',
                case
                    when "FilesInfo" -> 0 ->> 'file' = 'MainFile' then 'main'
                    else 'thumbnail' || substring("FilesInfo" -> 0 ->> 'resolution' from 1 for 3)
                    end,
                'path',
                case
                    when "FilesInfo" -> 0 ->> 'file' = 'MainFile' then
                        'Assets/AiMakeUp/' || "Id"::text || '/Main/' || ("FilesInfo" -> 0 ->> 'version') ||
                                '/content.jpeg'
                    else 'Assets/AiMakeUp/' || "Id"::text || '/Thumbnail/' || ("FilesInfo" -> 0 ->> 'resolution') ||
                                 '/' ||
                                 ("FilesInfo" -> 0 ->> 'version') || '/content.jpeg'
                    end
        ),
        json_build_object(
                'version', "FilesInfo" -> 1 -> 'version',
                'type',
                case
                    when "FilesInfo" -> 1 ->> 'file' = 'MainFile' then 'main'
                    else 'thumbnail' || substring("FilesInfo" -> 1 ->> 'resolution' from 1 for 3)
                    end,
                'path',
                case
                    when "FilesInfo" -> 1 ->> 'file' = 'MainFile' then
                        'Assets/AiMakeUp/' || "Id"::text || '/Main/' || ("FilesInfo" -> 1 ->> 'version') ||
                                '/content.jpeg'
                    else 'Assets/AiMakeUp/' || "Id"::text || '/Thumbnail/' || ("FilesInfo" -> 1 ->> 'resolution') ||
                                 '/' ||
                                 ("FilesInfo" -> 1 ->> 'version') || '/content.jpeg'
                    end
        )
              )
where "Files" is null;

commit;