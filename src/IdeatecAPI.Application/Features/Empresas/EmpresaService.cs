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