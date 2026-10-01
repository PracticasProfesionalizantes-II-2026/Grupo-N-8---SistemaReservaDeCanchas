using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FutbolYaAPI.Seguridad;

/// <summary>
/// Envía correos reales por SMTP usando MailKit (no System.Net.Mail.SmtpClient, que Microsoft
/// marca como obsoleto y con fallas de confiabilidad conocidas en apps de servidor). Cada paso
/// (conectar, autenticar, enviar) tira una excepción específica y con detalle si algo falla,
/// en vez de fallar en silencio. Configuración en appsettings.json / User Secrets, sección "Smtp".
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        var host = _config["Smtp:Host"];
        var puerto = _config.GetValue<int>("Smtp:Puerto");
        var usuario = _config["Smtp:Usuario"];
        // "Contrasena" sin ñ para poder cargarla como variable de entorno en hosts Linux;
        // "Contraseña" se mantiene por compatibilidad con configuraciones existentes.
        var contraseña = _config["Smtp:Contrasena"] ?? _config["Smtp:Contraseña"];
        var remitenteCorreo = _config["Smtp:RemitenteCorreo"] ?? usuario;
        var remitenteNombre = _config["Smtp:RemitenteNombre"] ?? "Fútbol Ya";

        // Sin credenciales se informa como error (no se ignora en silencio): quien llama decide qué
        // hacer, p. ej. el restablecimiento por admin revierte el cambio y avisa que no se envió.
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
        {
            throw new InvalidOperationException(
                "El envío de correos no está configurado (sección Smtp: Usuario/Contraseña).");
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(remitenteNombre, remitenteCorreo));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        // 15 s en vez de los 2 minutos por defecto de MailKit, para que un SMTP caído
        // no deje la pantalla del usuario colgada.
        using var cliente = new SmtpClient { Timeout = 15_000 };
        try
        {
            await cliente.ConnectAsync(host, puerto, SecureSocketOptions.StartTls);
            await cliente.AuthenticateAsync(usuario, contraseña);
            await cliente.SendAsync(mensaje);

            _logger.LogInformation("Correo enviado correctamente a {Destinatario}: {Asunto}", destinatario, asunto);
        }
        finally
        {
            if (cliente.IsConnected)
                await cliente.DisconnectAsync(true);
        }
    }
}
