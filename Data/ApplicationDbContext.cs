using Microsoft.EntityFrameworkCore;
using ParcialProgramacion.Models;

namespace ParcialProgramacion.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Incidencia> Incidencias => Set<Incidencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Incidencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Estacion).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(240);
            entity.Property(e => e.Estado).HasConversion<int>();

            entity.HasIndex(e => e.Estado);

            entity.HasData(
                new Incidencia
                {
                    Id = 1,
                    Estacion = "Estacion Central",
                    Descripcion = "Falla en el torniquete de entrada norte",
                    Estado = EstadoIncidencia.Abierta,
                    FechaApertura = new DateTime(2026, 3, 2, 8, 15, 0, DateTimeKind.Utc)
                },
                new Incidencia
                {
                    Id = 2,
                    Estacion = "Estacion Central",
                    Descripcion = "Elevador fuera de servicio en plataforma 2",
                    Estado = EstadoIncidencia.Abierta,
                    FechaApertura = new DateTime(2026, 3, 3, 9, 40, 0, DateTimeKind.Utc)
                },
                new Incidencia
                {
                    Id = 3,
                    Estacion = "Estacion Norte",
                    Descripcion = "Iluminacion intermitente en el andén",
                    Estado = EstadoIncidencia.Abierta,
                    FechaApertura = new DateTime(2026, 3, 4, 19, 5, 0, DateTimeKind.Utc)
                },
                new Incidencia
                {
                    Id = 4,
                    Estacion = "Estacion Sur",
                    Descripcion = "Maquina expendedora sinambio entregado",
                    Estado = EstadoIncidencia.Abierta,
                    FechaApertura = new DateTime(2026, 3, 6, 14, 30, 0, DateTimeKind.Utc)
                },
                new Incidencia
                {
                    Id = 5,
                    Estacion = "Estacion Norte",
                    Descripcion = "Puerta cortina con cierre lento defectuoso",
                    Estado = EstadoIncidencia.Cerrada,
                    FechaApertura = new DateTime(2026, 3, 1, 7, 0, 0, DateTimeKind.Utc),
                    FechaCierre = new DateTime(2026, 3, 2, 11, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}
