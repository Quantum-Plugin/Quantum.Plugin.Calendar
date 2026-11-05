using Microsoft.EntityFrameworkCore;
using NOF.Infrastructure.EntityFrameworkCore;

namespace Quantum.CalendarPlugin.Tests.Fixtures;

public sealed class PluginTestDb(DbContextOptions<PluginTestDb> options) : NOFDbContext(options);
