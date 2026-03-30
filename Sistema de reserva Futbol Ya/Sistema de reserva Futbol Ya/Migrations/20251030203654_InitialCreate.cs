using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_de_reserva_Futbol_Ya.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Canchas",
                columns: table => new
                {
                    CodigoCancha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canchas", x => x.CodigoCancha);
                });

            migrationBuilder.CreateTable(
                name: "MaterialesDeportivos",
                columns: table => new
                {
                    CodigoMaterial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CantidadDisponible = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialesDeportivos", x => x.CodigoMaterial);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    CodigoProducto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.CodigoProducto);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    CodigoUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contraseña = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<bool>(type: "bit", nullable: false),
                    CambiarContraseña = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.CodigoUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    CodigoReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hora = table.Column<TimeSpan>(type: "time", nullable: false),
                    Duracion = table.Column<int>(type: "int", nullable: false),
                    CodigoCancha = table.Column<int>(type: "int", nullable: false),
                    CodigoUsuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.CodigoReserva);
                    table.ForeignKey(
                        name: "FK_Reservas_Canchas_CodigoCancha",
                        column: x => x.CodigoCancha,
                        principalTable: "Canchas",
                        principalColumn: "CodigoCancha",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_CodigoUsuario",
                        column: x => x.CodigoUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "CodigoUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    CodigoVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaVenta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoraVenta = table.Column<TimeSpan>(type: "time", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodigoUsuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.CodigoVenta);
                    table.ForeignKey(
                        name: "FK_Ventas_Usuarios_CodigoUsuario",
                        column: x => x.CodigoUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "CodigoUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservaMateriales",
                columns: table => new
                {
                    CodigoReservaMaterial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoReserva = table.Column<int>(type: "int", nullable: false),
                    CodigoMaterial = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaMateriales", x => x.CodigoReservaMaterial);
                    table.ForeignKey(
                        name: "FK_ReservaMateriales_MaterialesDeportivos_CodigoMaterial",
                        column: x => x.CodigoMaterial,
                        principalTable: "MaterialesDeportivos",
                        principalColumn: "CodigoMaterial",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservaMateriales_Reservas_CodigoReserva",
                        column: x => x.CodigoReserva,
                        principalTable: "Reservas",
                        principalColumn: "CodigoReserva",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VentasDetalladas",
                columns: table => new
                {
                    CodigoVentaDetallada = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoVenta = table.Column<int>(type: "int", nullable: false),
                    CodigoProducto = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentasDetalladas", x => x.CodigoVentaDetallada);
                    table.ForeignKey(
                        name: "FK_VentasDetalladas_Productos_CodigoProducto",
                        column: x => x.CodigoProducto,
                        principalTable: "Productos",
                        principalColumn: "CodigoProducto",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VentasDetalladas_Ventas_CodigoVenta",
                        column: x => x.CodigoVenta,
                        principalTable: "Ventas",
                        principalColumn: "CodigoVenta",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaMateriales_CodigoMaterial",
                table: "ReservaMateriales",
                column: "CodigoMaterial");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaMateriales_CodigoReserva",
                table: "ReservaMateriales",
                column: "CodigoReserva");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_CodigoCancha",
                table: "Reservas",
                column: "CodigoCancha");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_CodigoUsuario",
                table: "Reservas",
                column: "CodigoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_CodigoUsuario",
                table: "Ventas",
                column: "CodigoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_VentasDetalladas_CodigoProducto",
                table: "VentasDetalladas",
                column: "CodigoProducto");

            migrationBuilder.CreateIndex(
                name: "IX_VentasDetalladas_CodigoVenta",
                table: "VentasDetalladas",
                column: "CodigoVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservaMateriales");

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
                name: "Usuarios");
        }
    }
}
