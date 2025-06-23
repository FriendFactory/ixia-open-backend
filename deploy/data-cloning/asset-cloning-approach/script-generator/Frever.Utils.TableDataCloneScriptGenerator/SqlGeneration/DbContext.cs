using Microsoft.EntityFrameworkCore;

namespace Frever.Utils.TableDataCloneScriptGenerator.SqlGeneration;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options);