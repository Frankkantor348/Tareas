using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tareas.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreacionTablaEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tarea",
                table: "Tarea");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Tarea");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Tarea");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Tarea");

            migrationBuilder.DropColumn(
                name: "Prioridad",
                table: "Tarea");

            migrationBuilder.RenameTable(
                name: "Tarea",
                newName: "Tareas");

            migrationBuilder.RenameColumn(
                name: "FechaCreacion",
                table: "Tareas",
                newName: "FechaPublicacion");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Tareas",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Curso",
                table: "Tareas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocenteId",
                table: "Tareas",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimite",
                table: "Tareas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InstruccionesAdicionales",
                table: "Tareas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "Tareas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tareas",
                table: "Tareas",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComentarioEstudiante = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RutaArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NombreArchivoOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Calificacion = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RetroalimentacionDocente = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCalificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entregas_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_DocenteId",
                table: "Tareas",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_FechaLimite",
                table: "Tareas",
                column: "FechaLimite");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_EstudianteId",
                table: "Entregas",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId",
                table: "Entregas",
                column: "TareaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entregas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tareas",
                table: "Tareas");

            migrationBuilder.DropIndex(
                name: "IX_Tareas_DocenteId",
                table: "Tareas");

            migrationBuilder.DropIndex(
                name: "IX_Tareas_FechaLimite",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "Curso",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "DocenteId",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "FechaLimite",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "InstruccionesAdicionales",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "Titulo",
                table: "Tareas");

            migrationBuilder.RenameTable(
                name: "Tareas",
                newName: "Tarea");

            migrationBuilder.RenameColumn(
                name: "FechaPublicacion",
                table: "Tarea",
                newName: "FechaCreacion");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Tarea",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Tarea",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Tarea",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Tarea",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Prioridad",
                table: "Tarea",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tarea",
                table: "Tarea",
                column: "Id");
        }
    }
}
