namespace IdeatecAPI.Application.Features.GuiaRemision.DTOs;

public class CreateGuiaDto
{
    public int? SucursalId { get; set; }
    public int Version { get; set; } = 2022;
    public string TipoDoc { get; set; } = "09";
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }

    public CreateGuiaCompanyDto Company { get; set; } = new();
    public CreateGuiaDestinatarioDto Destinatario { get; set; } = new();
    public CreateGuiaTerceroDto? Tercero { get; set; }

    public string? Observacion { get; set; }
    public CreateGuiaDocBajaDto? DocBaja { get; set; }
    public CreateGuiaRelDocDto? RelDoc { get; set; }

    public CreateGuiaEnvioDto Envio { get; set; } = new();
    public List<CreateGuiaDetalleDto> Details { get; set; } = new();
}

public class CreateGuiaCompanyDto
{
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public CreateGuiaDireccionDto? Address { get; set; }
}

public class CreateGuiaDestinatarioDto
{
    public string TipoDoc { get; set; } = string.Empty;
    public string NumDoc { get; set; } = string.Empty;
    public string RznSocial { get; set; } = string.Empty;
}

public class CreateGuiaTerceroDto
{
    public string TipoDoc { get; set; } = string.Empty;
    public string NumDoc { get; set; } = string.Empty;
    public string RznSocial { get; set; } = string.Empty;
}

public class CreateGuiaDocBajaDto
{
    public string TipoDoc { get; set; } = string.Empty;
    public string NroDoc { get; set; } = string.Empty;
}

public class CreateGuiaRelDocDto
{
    public string TipoDoc { get; set; } = string.Empty;
    public string NroDoc { get; set; } = string.Empty;
}

public class CreateGuiaEnvioDto
{
    public string CodTraslado { get; set; } = string.Empty;
    public string DesTraslado { get; set; } = string.Empty;
    public string ModTraslado { get; set; } = string.Empty;
    public DateTime FecTraslado { get; set; }
    public string? CodPuerto { get; set; }
    public bool IndTransbordo { get; set; } = false;
    public decimal PesoTotal { get; set; }
    public string UndPesoTotal { get; set; } = "KGM";
    public string? NumContenedor { get; set; }
    public string? MatPeligrosoClase { get; set; }
    public string? MatPeligrosoNroONU { get; set; }

    public CreateGuiaDireccionDto Llegada { get; set; } = new();
    public CreateGuiaDireccionDto Partida { get; set; } = new();

    public CreateGuiaTransportistaDto? Transportista { get; set; }
}

public class CreateGuiaDireccionDto
{
    public string Ubigueo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Provincia { get; set; }
    public string? Departamento { get; set; }
    public string? Distrito { get; set; }
}

public class CreateGuiaTransportistaDto
{
    public string? TipoDoc { get; set; }
    public string? NumDoc { get; set; }
    public string? RznSocial { get; set; }
    public string? RegistroMTC { get; set; }
    public bool IndVehiculoM1L { get; set; } = false;
    public string? Placa { get; set; }
    public string? AutorizacionVehiculoEntidad { get; set; }
    public string? AutorizacionVehiculoNumero { get; set; }
    public string? PlacaSecundaria1 { get; set; }  // ← nuevo
    public string? PlacaSecundaria2 { get; set; }  // ← nuevo
    public string? PlacaSecundaria3 { get; set; }  // ← nuevo
    public string? ChoferTipoDoc { get; set; }
    public string? ChoferDoc { get; set; }
    public string? ChoferNombres { get; set; }
    public string? ChoferApellidos { get; set; }
    public string? ChoferLicencia { get; set; }
    public string? ChoferSecundarioTipoDoc { get; set; }
    public string? ChoferSecundarioDoc { get; set; }
    public string? ChoferSecundarioNombres { get; set; }
    public string? ChoferSecundarioApellidos { get; set; }
    public string? ChoferSecundarioLicencia { get; set; }
    public string? ChoferSecundario2TipoDoc { get; set; }
    public string? ChoferSecundario2Doc { get; set; }
    public string? ChoferSecundario2Nombres { get; set; }
    public string? ChoferSecundario2Apellidos { get; set; }
    public string? ChoferSecundario2Licencia { get; set; }
}

public class CreateGuiaDetalleDto
{
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Codigo { get; set; }
}