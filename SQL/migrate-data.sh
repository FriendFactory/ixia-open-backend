SRC_DB_NAME=server-dev
SRC_SERVER=xxxxxxxxx
SRC_USER=xxxxxxxxx

DST_SERVER=127.0.0.1
DST_DB_NAME=frever_data
DST_USER=root

pg_dump --dbname=$SRC_DB_NAME \
        --host=$SRC_SERVER \
        --username=$SRC_USER \
        --no-owner \
        --file=data.sql \
        $SRC_DB_NAME

psql    --host=$DST_SERVER \
        --dbname=$DST_DB_NAME \
        --username=$DST_USER \
        --file=data.sql \
        $SRC_DB_NAME



