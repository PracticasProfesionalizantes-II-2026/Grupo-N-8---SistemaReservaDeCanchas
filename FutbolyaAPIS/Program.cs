using Scalar.AspNetCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Repositorios;
using FutbolyaAPIS.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IVentaDetalladaRepository, VentaDetalladaRepository>();
builder.Services.AddScoped<ICanchaRepository, CanchaRepository>();
builder.Services.AddScoped<IHorarioDisponibleRepository, HorarioDisponibleRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaMaterialRepository, ReservaMaterialRepository>();
builder.Services.AddScoped<IMaterialDeportivoRepository, MaterialDeportivoRepository>();


builder.Services.AddScoped<IUsuarioLogica, UsuarioLogica>();
builder.Services.AddScoped<IProductoLogica, ProductoLogica>();
builder.Services.AddScoped<IVentaLogica, VentaLogica>();
builder.Services.AddScoped<ICanchaLogica, CanchaLogica>();
builder.Services.AddScoped<IHorarioDisponibleLogica, HorarioDisponibleLogica>();
builder.Services.AddScoped<IReservaLogica, ReservaLogica>();
builder.Services.AddScoped<IMaterialDeportivoLogica, MaterialDeportivoLogica>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapUsuarioEndpoints();
app.MapProductoEndpoints();
app.MapVentaEndpoints();
app.MapCanchaEndpoints();
app.MapHorarioDisponibleEndpoints();
app.MapReservaEndpoints();
app.MapMaterialDeportivoEndpoints();

app.Run();


