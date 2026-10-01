namespace FutbolYaAPI.Logica.Excepciones;

// Se usa para abortar una transacción (rollback) cuando una regla de negocio
// falla a mitad de una operación con varias escrituras (ej.: perder la carrera por el stock
// dentro de una transacción de Venta/Reserva). La Logica la atrapa y la convierte de nuevo
// en el tuple (resultado, error) que ya usan los endpoints; nunca debería llegar al endpoint.
public class OperacionInvalidaException : Exception
{
    public OperacionInvalidaException(string mensaje) : base(mensaje) { }
}
