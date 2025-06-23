begin;

do
$$
    declare
        fk_curs cursor for
            select tc.table_schema,
                   tc.constraint_name,
                   tc.table_name,
                   kcu.column_name,
                   ccu.table_schema as foreign_table_schema,
                   ccu.table_name   as foreign_table_name,
                   ccu.column_name  as foreign_column_name
            from information_schema.table_constraints as tc
                     join information_schema.key_column_usage as kcu
                          on tc.constraint_name = kcu.constraint_name
                              and tc.table_schema = kcu.table_schema
                     join information_schema.constraint_column_usage as ccu
                          on ccu.constraint_name = tc.constraint_name
            where tc.constraint_type = 'FOREIGN KEY'
              and tc.table_schema in ('public', 'rollup', 'stats', 'cms')
              and tc.table_name not like '%_2%';
        rec record;
    begin
        for rec in fk_curs
            loop
                execute format(
                        'alter table %s."%s" drop constraint if exists "%s"',
                        rec.table_schema,
                        rec.table_name,
                        rec.constraint_name);
            end loop;

    end;

$$;

commit;