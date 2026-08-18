using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Application.Features.Inventario.Services;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.UnitTests;

// Cubre el consumo PEPS en lote: reparto correcto entre lotes, saldos de kardex y la
// promesa de rendimiento (lotes y saldos se leen una sola vez para toda la venta).
public class ConsumirFifoBatchTests
{
    private static InventarioLote Lote(int id, int sucursalProductoId, decimal saldo, decimal costo, int dia) => new()
    {
        InventarioLoteId = id,
        SucursalProductoId = sucursalProductoId,
        SaldoCantidad = saldo,
        CantidadOriginal = saldo,
        CostoUnitario = costo,
        FechaLote = new DateTime(2026, 1, dia),
        Estado = true
    };

    private static ConsumoPepsRequestDTO Consumo(int sucursalProductoId, decimal cantidad) => new()
    {
        SucursalProductoId = sucursalProductoId,
        Cantidad = cantidad,
        TipoMovimiento = "SALIDA_VENTA",
        ReferenciaTipo = "COMPROBANTE",
        ReferenciaId = 77
    };

    [Fact]
    public async Task Consume_lotes_en_orden_fifo_y_calcula_costo_promedio()
    {
        var repo = new FakeInventarioLoteRepository([Lote(1, 10, 3m, 2m, 1), Lote(2, 10, 5m, 4m, 2)]);
        var servicio = new InventarioPepsService(new FakeUnitOfWork(repo));

        var resultados = await servicio.ConsumirFifoBatchAsync([Consumo(10, 5m)], idUsuario: null);

        var resultado = Assert.Single(resultados);
        // 3 unidades a 2 + 2 unidades a 4 = 14
        Assert.Equal(14m, resultado.CostoTotal);
        Assert.Equal(2.8m, resultado.CostoUnitarioPromedio);

        Assert.Equal([(1, 3m), (2, 2m)], repo.Descuentos);
    }

    [Fact]
    public async Task Saldo_post_del_kardex_descuenta_sobre_el_saldo_previo()
    {
        var repo = new FakeInventarioLoteRepository([Lote(1, 10, 3m, 2m, 1), Lote(2, 10, 5m, 4m, 2)]);
        var servicio = new InventarioPepsService(new FakeUnitOfWork(repo));

        await servicio.ConsumirFifoBatchAsync([Consumo(10, 5m)], idUsuario: null);

        var movimiento = Assert.Single(repo.MovimientosRegistrados).Movimiento;
        // Antes habia 8 unidades valorizadas en 3*2 + 5*4 = 26.
        Assert.Equal(3m, movimiento.SaldoCantidadPost);
        Assert.Equal(12m, movimiento.SaldoValorPost);
    }

    [Fact]
    public async Task Dos_lineas_del_mismo_producto_no_consumen_dos_veces_el_mismo_saldo()
    {
        var repo = new FakeInventarioLoteRepository([Lote(1, 10, 4m, 2m, 1), Lote(2, 10, 4m, 5m, 2)]);
        var servicio = new InventarioPepsService(new FakeUnitOfWork(repo));

        var resultados = await servicio.ConsumirFifoBatchAsync([Consumo(10, 3m), Consumo(10, 3m)], idUsuario: null);

        Assert.Equal(2, resultados.Count);
        // La primera linea toma 3 del lote 1; la segunda arranca donde quedo: 1 del lote 1 y 2 del lote 2.
        Assert.Equal(6m, resultados[0].CostoTotal);
        Assert.Equal(12m, resultados[1].CostoTotal);

        // El lote 1 se descuenta una sola vez, con el total acumulado de ambas lineas.
        Assert.Equal([(1, 4m), (2, 2m)], repo.Descuentos);

        // Y los saldos del kardex avanzan de forma encadenada, no repetida.
        Assert.Equal([5m, 2m], repo.MovimientosRegistrados.Select(m => m.Movimiento.SaldoCantidadPost));
    }

    [Fact]
    public async Task Falla_cuando_los_lotes_no_cubren_la_cantidad_vendida()
    {
        var repo = new FakeInventarioLoteRepository([Lote(1, 10, 2m, 2m, 1)]);
        var servicio = new InventarioPepsService(new FakeUnitOfWork(repo));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ConsumirFifoBatchAsync([Consumo(10, 5m)], idUsuario: null));

        Assert.Contains("Stock insuficiente en lotes PEPS", ex.Message);
        Assert.Empty(repo.MovimientosRegistrados);
    }

    [Fact]
    public async Task Lee_lotes_y_saldos_una_sola_vez_para_toda_la_venta()
    {
        var lotes = Enumerable.Range(1, 20).Select(i => Lote(i, i, 10m, 3m, 1)).ToList();
        var repo = new FakeInventarioLoteRepository(lotes);
        var servicio = new InventarioPepsService(new FakeUnitOfWork(repo));

        var consumos = Enumerable.Range(1, 20).Select(i => Consumo(i, 2m)).ToList();
        await servicio.ConsumirFifoBatchAsync(consumos, idUsuario: null);

        // Este es el nucleo del arreglo de rendimiento: 20 productos no pueden costar
        // 20 lecturas de lotes ni 40 de saldos, y el kardex se escribe de una sola vez.
        Assert.Equal(1, repo.LecturasDeLotes);
        Assert.Equal(1, repo.LecturasDeSaldos);
        Assert.Equal(1, repo.EscriturasDeLotes);
        Assert.Equal(1, repo.EscriturasDeKardex);
        Assert.Equal(20, repo.MovimientosRegistrados.Count);
    }

    [Fact]
    public async Task Revierte_si_un_lote_dejo_de_tener_saldo_suficiente()
    {
        var repo = new FakeInventarioLoteRepository([Lote(1, 10, 5m, 2m, 1)])
        {
            // Otra venta se lleva el saldo entre la lectura bloqueante y el UPDATE.
            AntesDeDescontarLotes = lotes => lotes[0].SaldoCantidad = 1m
        };
        var servicio = new InventarioPepsService(new FakeUnitOfWork(repo));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ConsumirFifoBatchAsync([Consumo(10, 4m)], idUsuario: null));

        // El conteo de filas afectadas es lo que delata la carrera; sin el, la venta
        // quedaria registrada sin respaldo de stock.
        Assert.Contains("cambio de forma concurrente", ex.Message);
        Assert.Empty(repo.MovimientosRegistrados);
    }
}
