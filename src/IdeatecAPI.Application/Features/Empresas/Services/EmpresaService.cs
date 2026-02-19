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
    Task<Base64ToFileResponseDto> ConvertBase64ToFileAsync(Base64ToFileRequestDto dto);

    Task<Base64ToFileResponseDto> GenerarCertificadoFreeAsync(CertificadoFreeRequestDto dto);


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
            Provincia = dto.Provincia,
            Departamento = dto.Departamento,
            Distrito = dto.Distrito,
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
        empresa.Provincia = dto.Provincia ?? empresa.Provincia;
        empresa.Departamento = dto.Departamento ?? empresa.Departamento;
        empresa.Distrito = dto.Distrito ?? empresa.Distrito;
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


    public Task<Base64ToFileResponseDto> ConvertBase64ToFileAsync(Base64ToFileRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.Base64))
            throw new ArgumentException("El Base64 está vacío");

        var extensionesPermitidas = new[] { ".pfx", ".p12", ".png", ".jpg", ".jpeg" };
        var extension = dto.Extension.ToLower();

        if (!extensionesPermitidas.Any(e => e == extension))
            throw new ArgumentException($"Extensión {extension} no permitida. Use: {string.Join(", ", extensionesPermitidas)}");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(dto.Base64);
        }
        catch
        {
            throw new ArgumentException("El Base64 proporcionado no es válido");
        }

        var fileName = string.IsNullOrEmpty(dto.FileName)
            ? $"archivo_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}"
            : dto.FileName.EndsWith(extension) ? dto.FileName : $"{dto.FileName}{extension}";

        var contentType = extension switch
        {
            ".pfx" => "application/x-pkcs12",
            ".p12" => "application/x-pkcs12",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        return Task.FromResult(new Base64ToFileResponseDto
        {
            Bytes = bytes,
            FileName = fileName,
            ContentType = contentType
        });
    }

    public Task<Base64ToFileResponseDto> GenerarCertificadoFreeAsync(CertificadoFreeRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.Ruc))
            throw new ArgumentException("El RUC es requerido");

        if (string.IsNullOrEmpty(dto.Password))
            throw new ArgumentException("La contraseña es requerida");

        var keyPairGenerator = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();
        keyPairGenerator.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(
            new Org.BouncyCastle.Security.SecureRandom(), 2048));
        var keyPair = keyPairGenerator.GenerateKeyPair();

        var certGenerator = new Org.BouncyCastle.X509.X509V3CertificateGenerator();
        var subject = new Org.BouncyCastle.Asn1.X509.X509Name($"CN={dto.Ruc}, O=EMPRESA DE PRUEBA, C=PE");
        certGenerator.SetSubjectDN(subject);
        certGenerator.SetIssuerDN(subject);
        certGenerator.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(DateTime.UtcNow.Ticks));
        certGenerator.SetNotBefore(DateTime.UtcNow);
        certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(2));
        certGenerator.SetPublicKey(keyPair.Public);

        var signatureFactory = new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory(
            "SHA256WithRSA", keyPair.Private);
        var certificate = certGenerator.Generate(signatureFactory);

        var pkcs12Store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
        var certEntry = new Org.BouncyCastle.Pkcs.X509CertificateEntry(certificate);
        pkcs12Store.SetCertificateEntry(dto.Ruc, certEntry);
        pkcs12Store.SetKeyEntry(dto.Ruc,
            new Org.BouncyCastle.Pkcs.AsymmetricKeyEntry(keyPair.Private),
            new[] { certEntry });

        using var pfxStream = new MemoryStream();
        pkcs12Store.Save(pfxStream, dto.Password.ToCharArray(),
            new Org.BouncyCastle.Security.SecureRandom());

        return Task.FromResult(new Base64ToFileResponseDto
        {
            Bytes = pfxStream.ToArray(),
            FileName = $"CERTIFICADO-DEMO-{dto.Ruc}.pfx",
            ContentType = "application/x-pkcs12"
        });
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
        Provincia = e.Provincia,
        Departamento = e.Departamento,
        Distrito = e.Distrito,
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