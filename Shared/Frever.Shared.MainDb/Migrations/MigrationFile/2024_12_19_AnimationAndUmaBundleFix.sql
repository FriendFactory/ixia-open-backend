begin;

update "BodyAnimation"
set "BackgroundAnimationForRaceIds" = '{1,2}'
where "AvailableForBackground";

update "BodyAnimation"
set "EditingAnimationForRaceIds" = '{1,2}'
where "Name" = 'Selfie photo' or "Name" = 'Idle face';

update "UmaBundle"
set "FilesInfo" =
        '[{"source":{"uploadId":"5f9ec623-cb0e-4299-9b0f-f69e45dae66b"},"version":"20240612T094615Ube01030319df4e30b92d383d27a00b74","file":"MainFile","extension":"Empty","platform":"iOS"},{"source":{"uploadId":"cb0ee19a-47e0-4977-996c-88d4941c6fb8"},"version":"20240612T094615U0e641bb7244a4984a4aa891af134ba6e","file":"MainFile","extension":"Empty","platform":"Android"}]'
where "Id" = 580;

update "UmaBundle"
set "FilesInfo" =
        '[{"source":{"uploadId":"5da7cfa8-823e-4ce2-bc73-27d82060c619"},"version":"20240612T094510Ub970aff4378a42aebfe022c70d51fb25","file":"MainFile","extension":"Empty","platform":"iOS"},{"source":{"uploadId":"a53b4d09-ca00-444a-9837-98ee3e83f487"},"version":"20240612T094510U020ae1913bf64a3dbe68f592bb014131","file":"MainFile","extension":"Empty","platform":"Android"}]'
where "Id" = 581;

update "UmaBundle"
set "FilesInfo" =
        '[{"source":{"uploadId":"180c59ea-5624-4e41-b4ff-afb9f5208fde"},"version":"20240612T094351Uc1974285c27e4cc79c371951c978d2d9","file":"MainFile","extension":"Empty","platform":"iOS"},{"source":{"uploadId":"8099ae6d-35e6-464b-8f0c-e8b24f928c4b"},"version":"20240612T094351Ua9bd688828904a03a0cd26d089ed3d69","file":"MainFile","extension":"Empty","platform":"Android"}]'
where "Id" = 582;

commit;