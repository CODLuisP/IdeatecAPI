using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class EmpresaRepository : DapperRepository<Empresa>, IEmpresaRepository
{
    public EmpresaRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<Empresa>> GetAllEmpresasAsync()
    {
        var sql = "SELECT *, empresaID AS Id FROM empresa WHERE activo = 1 ORDER BY razonSocial";
        return await _connection.QueryAsync<Empresa>(sql, transaction: _transaction);
    }

    public async Task<Empresa?> GetEmpresaByIdAsync(int id)
    {
        var sql = "SELECT *, empresaID AS Id FROM empresa WHERE empresaID = @Id AND activo = 1";
        return await _connection.QueryFirstOrDefaultAsync<Empresa>(sql, new { Id = id }, _transaction);
    }

    public async Task<Empresa?> GetEmpresaByRucAsync(string ruc)
    {
        var sql = "SELECT *, empresaID AS Id FROM empresa WHERE ruc = @Ruc AND activo = 1";
        return await _connection.QueryFirstOrDefaultAsync<Empresa>(sql, new { Ruc = ruc }, _transaction);
    }

    public async Task<bool> ExisteRucAsync(string ruc)
    {
        var sql = "SELECT COUNT(1) FROM empresa WHERE ruc = @Ruc AND activo = 1";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { Ruc = ruc }, _transaction);
        return count > 0;
    }

    public async Task<int> CreateEmpresaAsync(Empresa empresa)
    {
        var sql = @"INSERT IGNORE INTO empresa 
        (ruc, igv, razonSocial, nombreComercial, direccion, ubigeo, urbanizacion,
        provincia, departamento, distrito,
        solUsuario, solClave, activo, creadoEn, telefono, email, logoBase64,
        certificadoPem, certificadoPassword, clientId, clientSecret, plan, environment)
        VALUES 
        (@Ruc, @Igv, @RazonSocial, @NombreComercial, @Direccion, @Ubigeo, @Urbanizacion,
        @Provincia, @Departamento, @Distrito,
        @SolUsuario, @SolClave, @Activo, @CreadoEn, @Telefono, @Email, @LogoBase64,
        @CertificadoPem, @CertificadoPassword, @ClientId, @ClientSecret, @Plan, @Environment)";

        await _connection.ExecuteAsync(sql, empresa, _transaction);
        return await _connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();", _transaction);
    }

    public async Task UpdateEmpresaAsync(Empresa empresa)
    {
        var sql = @"UPDATE empresa SET
    razonSocial = @RazonSocial,
    igv = @Igv,
    nombreComercial = @NombreComercial,
    direccion = @Direccion,
    ubigeo = @Ubigeo,
    urbanizacion = @Urbanizacion,
    provincia = @Provincia,
    departamento = @Departamento,
    distrito = @Distrito,
    solUsuario = @SolUsuario,
    solClave = @SolClave,
    activo = @Activo,
    telefono = @Telefono,
    email = @Email,
    logoBase64 = @LogoBase64,
    certificadoPem = @CertificadoPem,
    certificadoPassword = @CertificadoPassword,
    clientId = @ClientId,
    clientSecret = @ClientSecret,
    plan = @Plan,
    environment = @Environment,
    actualizadoEn = @ActualizadoEn
    WHERE empresaID = @Id";

        await _connection.ExecuteAsync(sql, empresa, _transaction);
    }

    public async Task DeleteEmpresaAsync(string ruc)
    {
        var sql = "DELETE FROM empresa WHERE ruc = @Ruc";
        await _connection.ExecuteAsync(sql, new { Ruc = ruc }, _transaction);
    }
}