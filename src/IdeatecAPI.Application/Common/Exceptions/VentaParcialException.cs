namespace IdeatecAPI.Application.Common.Exceptions;

// Se lanza al intentar cambiar la fecha de vencimiento de un lote con ventas parciales
// (0 < saldoCantidad < cantidadOriginal) sin haber confirmado el aviso. Un lote sin ventas
// o totalmente vendido (saldoCantidad == 0) no la dispara: en ambos casos no hay stock
// físico mixto cuya fecha quede ambigua.
public class VentaParcialException : Exception
{
    public decimal CantidadVendida { get; }
    public decimal CantidadOriginal { get; }
    public decimal SaldoCantidad { get; }

    public VentaParcialException(decimal cantidadVendida, decimal cantidadOriginal, decimal saldoCantidad)
        : base($"Se vendieron {cantidadVendida} de {cantidadOriginal} unidades de este lote. " +
               "El cambio de fecha también se reflejará en los reportes de lo ya vendido. ¿Deseas continuar?")
    {
        CantidadVendida = cantidadVendida;
        CantidadOriginal = cantidadOriginal;
        SaldoCantidad = saldoCantidad;
    }
}
