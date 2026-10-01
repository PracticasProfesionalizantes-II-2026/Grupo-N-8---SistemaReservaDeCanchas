namespace FutbolYaAPI.Seguridad;

// Abstrae el envío de correo para que Logica no dependa de MailKit directamente
// (implementación real: SmtpEmailService).
public interface IEmailService
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpo);
}
