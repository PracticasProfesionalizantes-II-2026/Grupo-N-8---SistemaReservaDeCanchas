using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutbolyaAPIS.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Canchas",
                columns: table => new
                {
                    Cod_Cancha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canchas", x => x.Cod_Cancha);
                });

            migrationBuilder.CreateTable(
                name: "HorariosDisponibles",
                columns: table => new
                {
                    Cod_Horario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosDisponibles", x => x.Cod_Horario);
                });

            migrationBuilder.CreateTable(
                name: "MaterialesDeportivos",
                columns: table => new
                {
                    Cod_Material = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cant_Material = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialesDeportivos", x => x.Cod_Material);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Cod_Producto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Cod_Producto);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Cod_Usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contraseña = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<bool>(type: "bit", nullable: false),
                    Cambiar_Contraseña = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Cod_Usuario);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Cod_Reserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Dni_Cliente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono_Cliente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cod_Cancha = table.Column<int>(type: "int", nullable: false),
                    Cod_Usuario = table.Column<int>(type: "int", nullable: false),
                    Duracion = table.Column<int>(type: "int", nullable: false),
                    Cod_Horario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Cod_Reserva);
                    table.ForeignKey(
                        name: "FK_Reservas_Canchas_Cod_Cancha",
                        column: x => x.Cod_Cancha,
                        principalTable: "Canchas",
                        principalColumn: "Cod_Cancha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_HorariosDisponibles_Cod_Horario",
                        column: x => x.Cod_Horario,
                        principalTable: "HorariosDisponibles",
                        principalColumn: "Cod_Horario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_Cod_Usuario",
                        column: x => x.Cod_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Cod_Usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Cod_Venta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hora = table.Column<TimeSpan>(type: "time", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Cod_Usuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Cod_Venta);
                    table.ForeignKey(
                        name: "FK_Ventas_Usuarios_Cod_Usuario",
                        column: x => x.Cod_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Cod_Usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservasMateriales",
                columns: table => new
                {
                    Cod_Reserva_Mat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cod_Reserva = table.Column<int>(type: "int", nullable: false),
                    Cod_Material = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasMateriales", x => x.Cod_Reserva_Mat);
                    table.ForeignKey(
                        name: "FK_ReservasMateriales_MaterialesDeportivos_Cod_Material",
                        column: x => x.Cod_Material,
                        principalTable: "MaterialesDeportivos",
                        principalColumn: "Cod_Material",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasMateriales_Reservas_Cod_Reserva",
                        column: x => x.Cod_Reserva,
                        principalTable: "Reservas",
                        principalColumn: "Cod_Reserva",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VentasDetalladas",
                columns: table => new
                {
                    Cod_Venta_Detallada = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cod_Venta = table.Column<int>(type: "int", nullable: false),
                    Cod_Producto = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentasDetalladas", x => x.Cod_Venta_Detallada);
                    table.ForeignKey(
                        name: "FK_VentasDetalladas_Productos_Cod_Producto",
                        column: x => x.Cod_Producto,
                        principalTable: "Productos",
                        principalColumn: "Cod_Producto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentasDetalladas_Ventas_Cod_Venta",
                        column: x => x.Cod_Venta,
                        principalTable: "Ventas",
                        principalColumn: "Cod_Venta",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Cod_Cancha",
                table: "Reservas",
                column: "Cod_Cancha");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Cod_Horario",
                table: "Reservas",
                column: "Cod_Horario");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Cod_Usuario",
                table: "Reservas",
                column: "Cod_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasMateriales_Cod_Material",
                table: "ReservasMateriales",
                column: "Cod_Material");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasMateriales_Cod_Reserva",
                table: "ReservasMateriales",
                column: "Cod_Reserva");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Cod_Usuario",
                table: "Ventas",
                column: "Cod_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_VentasDetalladas_Cod_Producto",
                table: "VentasDetalladas",
                column: "Cod_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_VentasDetalladas_Cod_Venta",
                table: "VentasDetalladas",
                column: "Cod_Venta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservasMateriales");

            migrationBuilder.DropTable(
                name: "VentasDetalladas");

            migrationBuilder.DropTable(
                name: "MaterialesDeportivos");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Canchas");

            migrationBuilder.DropTable(
                name: "HorariosDisponibles");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
