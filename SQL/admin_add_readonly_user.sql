-- You should manually replace user name, password and database
-- I don't find any way to use variables in Postgres scripts  
CREATE USER ro_user WITH PASSWORD 'ropass';
GRANT CONNECT ON DATABASE "server-dev" TO ro_user;
GRANT USAGE ON SCHEMA public TO ro_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO ro_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO ro_user;
