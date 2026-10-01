using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FutbolYaAPI.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoHorariosGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HorariosDisponibles_Canchas_Cod_Cancha",
                table: "HorariosDisponibles");

            migrationBuilder.DropIndex(
                name: "IX_HorariosDisponibles_Cod_Cancha",
                table: "HorariosDisponibles");

            migrationBuilder.DropColumn(
                name: "Cod_Cancha",
                table: "HorariosDisponibles");

            migrationBuilder.CreateTable(
                name: "CanchaHorarios",
                columns: table => new
                {
                    Cod_Cancha_Horario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cod_Cancha = table.Column<int>(type: "int", nullable: false),
                    Cod_Horario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanchaHorarios", x => x.Cod_Cancha_Horario);
                    table.ForeignKey(
                        name: "FK_CanchaHorarios_Canchas_Cod_Cancha",
                        column: x => x.Cod_Cancha,
                        principalTable: "Canchas",
                        principalColumn: "Cod_Cancha",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanchaHorarios_HorariosDisponibles_Cod_Horario",
                        column: x => x.Cod_Horario,
                        principalTable: "HorariosDisponibles",
                        principalColumn: "Cod_Horario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "HorariosDisponibles",
                columns: new[] { "Cod_Horario", "Activo", "HoraFin", "HoraInicio" },
                values: new object[,]
                {
                    { 1, true, new TimeSpan(0, 1, 0, 0, 0), new TimeSpan(0, 0, 0, 0, 0) },
                    { 2, true, new TimeSpan(0, 2, 0, 0, 0), new TimeSpan(0, 1, 0, 0, 0) },
                    { 3, true, new TimeSpan(0, 3, 0, 0, 0), new TimeSpan(0, 2, 0, 0, 0) },
                    { 4, true, new TimeSpan(0, 4, 0, 0, 0), new TimeSpan(0, 3, 0, 0, 0) },
                    { 5, true, new TimeSpan(0, 5, 0, 0, 0), new TimeSpan(0, 4, 0, 0, 0) },
                    { 6, true, new TimeSpan(0, 6, 0, 0, 0), new TimeSpan(0, 5, 0, 0, 0) },
                    { 7, true, new TimeSpan(0, 7, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0) },
                    { 8, true, new TimeSpan(0, 8, 0, 0, 0), new TimeSpan(0, 7, 0, 0, 0) },
                    { 9, true, new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 8, 0, 0, 0) },
                    { 10, true, new TimeSpan(0, 10, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0) },
                    { 11, true, new TimeSpan(0, 11, 0, 0, 0), new TimeSpan(0, 10, 0, 0, 0) },
                    { 12, true, new TimeSpan(0, 12, 0, 0, 0), new TimeSpan(0, 11, 0, 0, 0) },
                    { 13, true, new TimeSpan(0, 13, 0, 0, 0), new TimeSpan(0, 12, 0, 0, 0) },
                    { 14, true, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0) },
                    { 15, true, new TimeSpan(0, 15, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0) },
                    { 16, true, new TimeSpan(0, 16, 0, 0, 0), new TimeSpan(0, 15, 0, 0, 0) },
                    { 17, true, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 16, 0, 0, 0) },
                    { 18, true, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 17, 0, 0, 0) },
                    { 19, true, new TimeSpan(0, 19, 0, 0, 0), new TimeSpan(0, 18, 0, 0, 0) },
                    { 20, true, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 19, 0, 0, 0) },
                    { 21, true, new TimeSpan(0, 21, 0, 0, 0), new TimeSpan(0, 20, 0, 0, 0) },
                    { 22, true, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 21, 0, 0, 0) },
                    { 23, true, new TimeSpan(0, 23, 0, 0, 0), new TimeSpan(0, 22, 0, 0, 0) },
                    { 24, true, new TimeSpan(1, 0, 0, 0, 0), new TimeSpan(0, 23, 0, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanchaHorarios_Cod_Cancha_Cod_Horario",
                table: "CanchaHorarios",
                columns: new[] { "Cod_Cancha", "Cod_Horario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanchaHorarios_Cod_Horario",
                table: "CanchaHorarios",
                column: "Cod_Horario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanchaHorarios");

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "HorariosDisponibles",
                keyColumn: "Cod_Horario",
                keyValue: 24);

            migrationBuilder.AddColumn<int>(
                name: "Cod_Cancha",
                table: "HorariosDisponibles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosDisponibles_Cod_Cancha",
                table: "HorariosDisponibles",
                column: "Cod_Cancha");

            migrationBuilder.AddForeignKey(
                name: "FK_HorariosDisponibles_Canchas_Cod_Cancha",
                table: "HorariosDisponibles",
                column: "Cod_Cancha",
                principalTable: "Canchas",
                principalColumn: "Cod_Cancha",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
