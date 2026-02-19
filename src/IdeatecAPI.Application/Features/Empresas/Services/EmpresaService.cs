using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Empresas.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Empresas.Services;

public interface IEmpresaService
{
    Task<IEnumerable<EmpresaDto>> GetAllEmpresasAsync();
    Task<EmpresaDto?> GetEmpresaByIdAsync(int id);
    Task<EmpresaDto> CreateEmpresaAsync(CreateEmpresaDto dto);
    Task<EmpresaDto> UpdateEmpresaAsync(int id, UpdateEmpresaDto dto);
    Task DeleteEmpresaAsync(int id);
    Task<FileToBase64Dto> ConvertFileToBase64Async(Stream fileStream, string fileName, string contentType, long sizeBytes);
    Task<CertificadoResponseDto> ConvertCertificadoAsync(CertificadoRequestDto dto);
}



public class EmpresaService : IEmpresaService
{
    private readonly IUnitOfWork _unitOfWork;

    public EmpresaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EmpresaDto>> GetAllEmpresasAsync()
    {
        var empresas = await _unitOfWork.Empresas.GetAllEmpresasAsync();
        return empresas.Select(MapToDto);
    }

    public async Task<EmpresaDto?> GetEmpresaByIdAsync(int id)
    {
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(id);
        return empresa is null ? null : MapToDto(empresa);
    }

    public async Task<EmpresaDto> CreateEmpresaAsync(CreateEmpresaDto dto)
    {
        if (await _unitOfWork.Empresas.ExisteRucAsync(dto.Ruc))
            throw new InvalidOperationException($"Ya existe una empresa con RUC {dto.Ruc}");

        var empresa = new Empresa
        {
            Ruc = dto.Ruc,
            RazonSocial = dto.RazonSocial,
            NombreComercial = dto.NombreComercial,
            Direccion = dto.Direccion,
            Ubigeo = dto.Ubigeo,
            Urbanizacion = dto.Urbanizacion,
            Telefono = dto.Telefono,
            Email = dto.Email,
            LogoBase64 = dto.LogoBase64,
            CertificadoPem = dto.CertificadoPem,
            CertificadoPassword = dto.CertificadoPassword,
            SolUsuario = dto.SolUsuario,
            SolClave = dto.SolClave,
            ClienteId = dto.ClienteId,
            ClientSecret = dto.ClientSecret,
            Plan = dto.Plan,
            Environment = dto.Environment,
            Activo = true,
            CreadoEn = DateTime.UtcNow
        };

        var newId = await _unitOfWork.Empresas.CreateEmpresaAsync(empresa);
        empresa.Id = newId;
        return MapToDto(empresa);
    }

    public async Task<EmpresaDto> UpdateEmpresaAsync(int id, UpdateEmpresaDto dto)
    {
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(id)
            ?? throw new KeyNotFoundException($"Empresa {id} no encontrada");

        empresa.RazonSocial = dto.RazonSocial ?? empresa.RazonSocial;
        empresa.NombreComercial = dto.NombreComercial ?? empresa.NombreComercial;
        empresa.Direccion = dto.Direccion ?? empresa.Direccion;
        empresa.Ubigeo = dto.Ubigeo ?? empresa.Ubigeo;
        empresa.Urbanizacion = dto.Urbanizacion ?? empresa.Urbanizacion;
        empresa.Telefono = dto.Telefono ?? empresa.Telefono;
        empresa.Email = dto.Email ?? empresa.Email;
        empresa.LogoBase64 = dto.LogoBase64 ?? empresa.LogoBase64;
        empresa.CertificadoPem = dto.CertificadoPem ?? empresa.CertificadoPem;
        empresa.CertificadoPassword = dto.CertificadoPassword ?? empresa.CertificadoPassword;
        empresa.SolUsuario = dto.SolUsuario ?? empresa.SolUsuario;
        empresa.SolClave = dto.SolClave ?? empresa.SolClave;
        empresa.ClienteId = dto.ClienteId ?? empresa.ClienteId;
        empresa.ClientSecret = dto.ClientSecret ?? empresa.ClientSecret;
        empresa.Plan = dto.Plan ?? empresa.Plan;
        empresa.Environment = dto.Environment ?? empresa.Environment;
        empresa.ActualizadoEn = DateTime.UtcNow;

        await _unitOfWork.Empresas.UpdateEmpresaAsync(empresa);
        return MapToDto(empresa);
    }


    public async Task<FileToBase64Dto> ConvertFileToBase64Async(Stream fileStream, string fileName, string contentType, long sizeBytes)
    {
        if (fileStream == null || sizeBytes == 0)
            throw new ArgumentException("El archivo está vacío o no fue enviado");

        var extensionesPermitidas = new[] { ".pfx", ".p12", ".png", ".jpg", ".jpeg" };
        var extension = Path.GetExtension(fileName).ToLower();

        if (!extensionesPermitidas.Any(e => e == extension))
            throw new ArgumentException($"Extensión {extension} no permitida. Use: {string.Join(", ", extensionesPermitidas)}");

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());

        return new FileToBase64Dto
        {
            FileName = fileName,
            ContentType = contentType,
            Base64 = base64,
            SizeBytes = sizeBytes
        };
    }
    
    


    public Task<CertificadoResponseDto> ConvertCertificadoAsync(CertificadoRequestDto dto)
{
    try
    {
        // 1. Decodifica el Base64 a bytes del PFX
        var pfxBytes = Convert.FromBase64String(dto.Cert);
        var password = dto.CertPass.ToCharArray();

        // 2. Carga el PFX con BouncyCastle
        var pkcs12Store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
        using var stream = new MemoryStream(pfxBytes);

        try
        {
            pkcs12Store.Load(stream, password);
        }
        catch
        {
            throw new ArgumentException("Contraseña del certificado incorrecta");
        }

        // 3. Extrae la clave privada y el certificado
        string? alias = null;
        foreach (string a in pkcs12Store.Aliases)
        {
            if (pkcs12Store.IsKeyEntry(a))
            {
                alias = a;
                break;
            }
        }

        if (alias == null)
            throw new ArgumentException("No se encontró clave privada en el certificado");

        var privateKey = pkcs12Store.GetKey(alias).Key;
        var certChain = pkcs12Store.GetCertificateChain(alias);
        var certificate = certChain[0].Certificate;

        // 4. Construye el PEM (certificado + clave privada)
        var pemBuilder = new StringBuilder();
        var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(new StringWriter(pemBuilder));
        pemWriter.WriteObject(certificate);
        pemWriter.WriteObject(privateKey);
        pemWriter.Writer.Flush();

        // 5. Construye el CER (solo certificado público)
        var cerBuilder = new StringBuilder();
        var cerWriter = new Org.BouncyCastle.OpenSsl.PemWriter(new StringWriter(cerBuilder));
        cerWriter.WriteObject(certificate);
        cerWriter.Writer.Flush();

        // 6. Codifica ambos en Base64
        var pemBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(pemBuilder.ToString()));
        var cerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(cerBuilder.ToString()));

        return Task.FromResult(new CertificadoResponseDto
        {
            Pem = pemBase64,
            Cer = cerBase64
        });
    }
    catch (ArgumentException)
    {
        throw;
    }
    catch (Exception)
    {
        throw new ArgumentException("Error al procesar el certificado. Verifica que sea un archivo PFX/P12 válido");
    }
}
   
   
   
   
   
   
   
   
   
   
   
    public async Task DeleteEmpresaAsync(int id)
    {
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(id)
            ?? throw new KeyNotFoundException($"Empresa {id} no encontrada");

        // Soft delete
        empresa.Activo = false;
        empresa.ActualizadoEn = DateTime.UtcNow;
        await _unitOfWork.Empresas.UpdateEmpresaAsync(empresa);
    }

    private static EmpresaDto MapToDto(Empresa e) => new()
    {
        Id = e.Id,
        Ruc = e.Ruc,
        RazonSocial = e.RazonSocial,
        NombreComercial = e.NombreComercial,
        Direccion = e.Direccion,
        Ubigeo = e.Ubigeo,
        Urbanizacion = e.Urbanizacion,
        Telefono = e.Telefono,
        Email = e.Email,
        LogoBase64 = e.LogoBase64,
        Plan = e.Plan,
        Environment = e.Environment,
        TieneCertificado = !string.IsNullOrEmpty(e.CertificadoPem),
        TieneCredencialesSunat = !string.IsNullOrEmpty(e.SolClave),
        Activo = e.Activo,
        CreadoEn = e.CreadoEn,
        ActualizadoEn = e.ActualizadoEn
    };
}