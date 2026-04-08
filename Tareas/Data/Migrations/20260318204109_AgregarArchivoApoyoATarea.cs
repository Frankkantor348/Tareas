using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tareas.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarArchivoApoyoATarea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSubidaArchivo",
                table: "Tareas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivoApoyo",
                table: "Tareas",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaArchivoApoyo",
                table: "Tareas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TamañoArchivoApoyo",
                table: "Tareas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoArchivoApoyo",
                table: "Tareas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaSubidaArchivo",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "NombreArchivoApoyo",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "RutaArchivoApoyo",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "TamañoArchivoApoyo",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "TipoArchivoApoyo",
                table: "Tareas");
        }
    }
}
