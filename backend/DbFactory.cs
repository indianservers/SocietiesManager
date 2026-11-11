using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Society.Api;

public class DbFactory : IDesignTimeDbContextFactory<AppDb> { public AppDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDb>().UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? "Host=127.0.0.1;Port=55439;Database=assemblia;Username=postgres").Options); }
