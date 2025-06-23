DO
$$
    DECLARE
        q               TEXT;
        r               RECORD;
        ns              text[];
        excluded_tables text[];
    BEGIN
        ns := ARRAY ['public', 'cms', 'rollup', 'stats'];
        excluded_tables := ARRAY ['spatial_ref_sys', 'us_gaz', 'us_lex', 'us_rules', 'ur'];

        -- triggers
        FOR r IN (SELECT pns.nspname, pc.relname, pt.tgname
                  FROM pg_catalog.pg_trigger pt,
                       pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pc.oid = pt.tgrelid
                    AND pns.nspname = ANY (ns)
                    AND pt.tgisinternal = false)
            LOOP
                EXECUTE format('DROP TRIGGER %I ON %I.%I;',
                               r.tgname, r.nspname, r.relname);
            END LOOP;

        -- constraints #1: foreign key
        FOR r IN (SELECT pns.nspname, pc.relname, pcon.conname
                  FROM pg_catalog.pg_constraint pcon,
                       pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pc.oid = pcon.conrelid
                    AND pns.nspname = ANY (ns)
                    AND pcon.contype = 'f'
                    and pc.relispartition = false
                    and not (pc.relname = ANY (excluded_tables)))
            LOOP
                EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT %I;',
                               r.nspname, r.relname, r.conname);
            END LOOP;

        -- constraints #1.1: partition FK
        FOR r IN (SELECT pns.nspname, pc.relname, pcon.conname
                  FROM pg_catalog.pg_constraint pcon,
                       pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pc.oid = pcon.conrelid
                    AND pns.nspname = ANY (ns)
                    AND pcon.contype = 'f'
                    and pc.relispartition = true
                    and not (pc.relname = ANY (excluded_tables)))
            LOOP
                EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT %I;',
                               r.nspname, r.relname, r.conname);
            END LOOP;

        -- constraints #2: the rest
        FOR r IN (SELECT pns.nspname, pc.relname, pcon.conname
                  FROM pg_catalog.pg_constraint pcon,
                       pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pc.oid = pcon.conrelid
                    AND pns.nspname = ANY (ns)
                    AND pcon.contype <> 'f'
                    and pc.relispartition = false
                    and not (pc.relname = ANY (excluded_tables)))
            LOOP
                EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT %I;',
                               r.nspname, r.relname, r.conname);
            END LOOP;

        -- constraints #2: the rest partitioned
        FOR r IN (SELECT pns.nspname, pc.relname, pcon.conname
                  FROM pg_catalog.pg_constraint pcon,
                       pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pc.oid = pcon.conrelid
                    AND pns.nspname = ANY (ns)
                    AND pcon.contype <> 'f'
                    and pc.relispartition = true
                    and not (pc.relname = ANY (excluded_tables)))
            LOOP
                EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT %I;',
                               r.nspname, r.relname, r.conname);
            END LOOP;

        -- indicēs
        FOR r IN (select i.schemaname, i.tablename, i.indexname
                  from pg_indexes i
                  where i.schemaname = ANY (ns)
                    and i.tablename not like '%_%' -- excludes partitioned table like Comments_2024
                    and not (i.tablename = ANY (excluded_tables)))
            LOOP
                EXECUTE format('DROP INDEX %I.%I;',
                               r.schemaname, r.indexname);
            END LOOP;

        -- normal views
        FOR r IN (SELECT pns.nspname, pc.relname
                  FROM pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pns.nspname = ANY (ns)
                    AND pc.relkind IN ('v')
                    AND pc.relname not in
                        ('geography_columns', 'geometry_columns', 'pg_stat_statements', 'pg_stat_statements_info',
                         'raster_columns', 'raster_overviews'))
            LOOP
                EXECUTE format('DROP VIEW %I.%I;',
                               r.nspname, r.relname);
            END LOOP;

        -- materialized views
        FOR r IN (SELECT pns.nspname, pc.relname
                  FROM pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pns.nspname = ANY (ns)
                    AND pc.relkind IN ('m')
                    AND pc.relname not in
                        ('geography_columns', 'geometry_columns', 'pg_stat_statements', 'pg_stat_statements_info',
                         'raster_columns', 'raster_overviews'))
            LOOP
                EXECUTE format('DROP MATERIALIZED VIEW %I.%I;',
                               r.nspname, r.relname);
            END LOOP;

        -- tables
        FOR r IN (SELECT pns.nspname, pc.relname
                  FROM pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pns.nspname = ANY (ns)
                    AND pc.relkind in ('p', 'r')
                    and not (pc.relname = ANY (excluded_tables)))
            LOOP
                EXECUTE format('DROP TABLE IF EXISTS %I.%I;',
                               r.nspname, r.relname);
            END LOOP;

        -- sequences
        FOR r IN (SELECT pns.nspname, pc.relname
                  FROM pg_catalog.pg_class pc,
                       pg_catalog.pg_namespace pns
                  WHERE pns.oid = pc.relnamespace
                    AND pns.nspname = ANY (ns)
                    AND pc.relkind = 'S'
                    AND pc.relname not like 'us_%')
            LOOP
                EXECUTE format('DROP SEQUENCE %I.%I;',
                               r.nspname, r.relname);
            END LOOP;

        RAISE NOTICE 'Database cleared!';
    END;
$$;