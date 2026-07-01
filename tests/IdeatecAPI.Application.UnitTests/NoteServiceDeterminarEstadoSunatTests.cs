using IdeatecAPI.Application.Features.Notas.Services;

namespace IdeatecAPI.Application.UnitTests;

public class NoteServiceDeterminarEstadoSunatTests
{
    [Fact]
    public void Exitoso_sin_observaciones_es_ACEPTADO()
    {
        var response = new SunatResponse { Success = true, TieneObservaciones = false };

        var estado = NoteService.DeterminarEstadoSunat(response);

        Assert.Equal("ACEPTADO", estado);
    }

    [Fact]
    public void Exitoso_con_observaciones_es_ACEPTADO_CON_OBSERVACIONES()
    {
        var response = new SunatResponse { Success = true, TieneObservaciones = true };

        var estado = NoteService.DeterminarEstadoSunat(response);

        Assert.Equal("ACEPTADO_CON_OBSERVACIONES", estado);
    }

    [Theory]
    [InlineData("SOAP_FAULT")]
    [InlineData("SUNAT_ERROR_HTML")]
    [InlineData("ERROR_RED")]
    [InlineData("SIN_RESPUESTA")]
    [InlineData("ERROR_PARSE")]
    [InlineData("CDR_ERROR")]
    public void Fallas_de_comunicacion_sin_CDR_quedan_PENDIENTE(string codigoRespuesta)
    {
        var response = new SunatResponse
        {
            Success = false,
            CodigoRespuesta = codigoRespuesta,
            CdrBase64 = null
        };

        var estado = NoteService.DeterminarEstadoSunat(response);

        Assert.Equal("PENDIENTE", estado);
    }

    [Fact]
    public void Codigo_desconocido_con_CDR_real_es_RECHAZADO()
    {
        // Simula un código de rechazo de validación real emitido por SUNAT dentro del CDR
        var response = new SunatResponse
        {
            Success = false,
            CodigoRespuesta = "2335",
            CdrBase64 = "Zm9v" // CDR presente => veredicto real de SUNAT
        };

        var estado = NoteService.DeterminarEstadoSunat(response);

        Assert.Equal("RECHAZADO", estado);
    }

    [Fact]
    public void Codigo_desconocido_sin_CDR_queda_PENDIENTE()
    {
        // Ningún CDR real de por medio: no se asume rechazo aunque el código no esté en la lista
        var response = new SunatResponse
        {
            Success = false,
            CodigoRespuesta = "ALGO_NUEVO_NO_CONTEMPLADO",
            CdrBase64 = null
        };

        var estado = NoteService.DeterminarEstadoSunat(response);

        Assert.Equal("PENDIENTE", estado);
    }
}
