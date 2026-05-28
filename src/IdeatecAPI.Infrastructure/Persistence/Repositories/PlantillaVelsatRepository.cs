// IdeatecAPI.Infrastructure.Persistence.Repositories/PlantillaVelsatRepository.cs
using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.PlantillaVelsat.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class PlantillaVelsatRepository : IPlantillaVelsatRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public PlantillaVelsatRepository(IDbConnection connection, IDbTransaction? transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<PlantillaVelsat>> GetAllAsync(string? periodo = null)
    {
        const string sql = @"
        SELECT id, numdoc, razonSocial, periodo, concepto, moneda,
               importe, fechaini, fechafin, placa, estado
        FROM plantillavelsat
        WHERE (@Periodo IS NULL OR periodo = @Periodo)";

        return await _connection.QueryAsync<PlantillaVelsat>(
            sql, new { Periodo = periodo }, transaction: _transaction);
    }

    public async Task<PlantillaVelsat?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, numdoc, razonSocial, periodo, concepto, moneda,
                   importe, fechaini, fechafin, placa, estado
            FROM plantillavelsat
            WHERE id = @Id";

        return await _connection.QueryFirstOrDefaultAsync<PlantillaVelsat>(
            sql, new { Id = id }, transaction: _transaction);
    }

    public async Task<PlantillaVelsat> CrearAsync(PlantillaVelsat plantilla)
    {
        const string sql = @"
            INSERT INTO plantillavelsat 
                (numdoc, razonSocial, periodo, concepto, moneda, importe, fechaini, fechafin, placa, estado)
            VALUES 
                (@Numdoc, @RazonSocial, @Periodo, @Concepto, @Moneda, @Importe, @Fechaini, @Fechafin, @Placa, 1);
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, plantilla, transaction: _transaction);
        plantilla.Id = newId;
        plantilla.Estado = 1;
        return plantilla;
    }

    public async Task<bool> EditarAsync(PlantillaVelsat plantilla, EditarPlantillaVelsatDTO dto)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", plantilla.Id);

        if (dto.Numdoc != null) { setClauses.Add("numdoc = @Numdoc"); parameters.Add("Numdoc", dto.Numdoc); }
        if (dto.RazonSocial != null) { setClauses.Add("razonSocial = @RazonSocial"); parameters.Add("RazonSocial", dto.RazonSocial); }
        if (dto.Periodo != null) { setClauses.Add("periodo = @Periodo"); parameters.Add("Periodo", dto.Periodo); }
        if (dto.Concepto != null) { setClauses.Add("concepto = @Concepto"); parameters.Add("Concepto", dto.Concepto); }
        if (dto.Importe != null) { setClauses.Add("importe = @Importe"); parameters.Add("Importe", dto.Importe); }
        if (dto.Fechaini != null) { setClauses.Add("fechaini = @Fechaini"); parameters.Add("Fechaini", dto.Fechaini); }
        if (dto.Fechafin != null) { setClauses.Add("fechafin = @Fechafin"); parameters.Add("Fechafin", dto.Fechafin); }
        if (dto.Placa != null) { setClauses.Add("placa = @Placa"); parameters.Add("Placa", dto.Placa); }
        if (dto.Moneda != null) { setClauses.Add("moneda = @Moneda"); parameters.Add("Moneda", dto.Moneda); }

        if (!setClauses.Any())
            throw new ArgumentException("Debes enviar al menos un campo para actualizar.");

        var sql = $"UPDATE plantillavelsat SET {string.Join(", ", setClauses)} WHERE id = @Id";

        var rows = await _connection.ExecuteAsync(sql, parameters, transaction: _transaction);
        return rows > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        const string sql = @"
            UPDATE plantillavelsat 
            SET estado = 0 
            WHERE id = @Id";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id }, transaction: _transaction);
        return rows > 0;
    }
}