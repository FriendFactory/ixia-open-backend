# Read and write separation

We use two databases with two different connection strings.
Please follow next principles on decide what database to use:

- Write database is a source of truth.
  If you need to get data you believe must exist, do this from write database.
- Consider those database as containing different data.
  No guarantee that databases contain the same data at the certain moment of time.
- Consider read database as a some sort of cache.
  Cache will be filled up at some moment of time.
- Don't mix access to read and write databases.
  Having both `IReadDb` and `IWriteDb` as dependencies usually an error.
  (But not always, for example checking if user is blocked against write db
  to filter out videos got from read db is fine)
- Read db obviously can't run transactions
- If you need versatile data, if you need read/write mixing -- use write db.
- Avoid using `Read/WriteDbContext` directly unless you're writing some infrastructure code.
- In admin, bots, etc. use write db. No reason to speed up access there.
- Good examples of using write db:
    - Admin service (just don't use read db there)
    - Chat (frequent adding/reading, need fresh data)
    - Checking user permissions (need access to fresh data, needs user existence)
    - Get follower/following/stats for current user (user might want to see newly added friend in his list)
- Good examples of using read db:
    - Video feeds (except user own videos): if video is not in read db yet it's fine
    - Group stats (except your own stats)
    - Assets access (some delay after asset publishing is okay in most cases)
    - Accessing countries, languages, geo-clusters and other more or less static data
