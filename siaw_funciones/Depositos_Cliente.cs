﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;


namespace siaw_funciones
{
    public class Depositos_Cliente
    {
        TipoCambio tipocambio = new TipoCambio();
        Ventas ventas = new Ventas();
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }


        public async Task<bool> Cobranza_Credito_Se_Dio_Descto_Deposito(DBContext _context, int codcobranza)
        {
            var resultadoes = await _context.cocobranza_deposito
                .Where(i => i.codcobranza == codcobranza)
                .CountAsync();
            if (resultadoes > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> Cobranza_Contado_Ce_Se_Dio_Descto_Deposito(DBContext _context, int codcobranza_contado)
        {
            var resultadoes = await _context.cocobranza_deposito
                .Where(i => i.codcobranza_contado == codcobranza_contado)
                .CountAsync();
            if (resultadoes > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> Anticipo_Contado_Aplicado_A_Proforma_Se_Dio_Descto_Deposito(DBContext _context, int codanticipo)
        {
            var resultadoes = await _context.cocobranza_deposito
                .Where(i => i.codanticipo == codanticipo)
                .CountAsync();
            if (resultadoes > 0)
            {
                return true;
            }
            return false;
        }

        public async Task<double> Total_Descuentos_Por_Deposito_Aplicado_De_Cobranza_En_Proforma(DBContext _context, int codcobranza, int codproforma, string moneda)
        {
            double monto = 0;
            double resultado = 0;
            var resultados = await _context.veproforma
                .Join(_context.vedesextraprof,
                      p1 => p1.codigo,
                      p2 => p2.codproforma,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza,
                      join1 => join1.p2.codcobranza,
                      p3 => p3.codigo,
                      (join1, p3) => new { join1.p1, join1.p2, p3 })
                .Where(join2 => join2.p2.codcobranza == codcobranza &&
                                join2.p1.anulada == false &&
                                join2.p3.reciboanulado == false)
                .OrderBy(join2 => join2.p1.fecha)
                .Select(join2 => new
                {
                    id = join2.p1.id,
                    numeroid = join2.p1.numeroid,
                    codalmacen = join2.p1.codalmacen,
                    fecha = join2.p1.fecha,
                    total = join2.p1.total,
                    monedaprof = join2.p1.codmoneda,
                    monto_descto = join2.p2.montodoc,
                    codproforma = join2.p2.codproforma
                }).ToListAsync();
            if (codproforma!=0)
            {
                resultados = resultados.Where(i => i.codproforma != codproforma).ToList();
            }

            foreach (var reg in resultados)
            {
                if (reg.monedaprof == moneda)
                {
                    monto = (double)reg.monto_descto;
                }
                else
                {
                    monto = (double)(await tipocambio._conversion(_context, moneda, reg.monedaprof, reg.fecha, reg.monto_descto));
                }
                resultado += monto;
            }
            resultado = Math.Round(resultado, 2);
            return resultado;
        }



        public async Task<double> Total_Ajustes_Por_Deposito_Aplicado_De_Cobranza(DBContext _context, int codcobranza, int codproforma, string moneda)
        {
            double monto = 0;
            double resultado = 0;
            var resultados = await _context.cocobranza_deposito_ajuste
                .Join(_context.cocobranza,
                      p1 => p1.codcobranza_ajustada,
                      p2 => p2.codigo,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza,
                      join1 => join1.p1.codcobranza,
                      p3 => p3.codigo,
                      (join1, p3) => new { join1.p1, join1.p2, p3 })
                .Where(join2 => join2.p1.codcobranza == codcobranza)
                .OrderBy(join2 => join2.p1.codcobranza_ajustada)
                .Select(join2 => new
                {
                    join2.p1.codcobranza_ajustada,
                    idcbza_ajustada = join2.p2.id,
                    nroidcbza_ajustada = join2.p2.numeroid,
                    monto_aplicado = join2.p1.monto,
                    join2.p1.codcobranza,
                    join2.p3.id,
                    join2.p3.numeroid,
                    fechacbza = join2.p3.fecha,
                    join2.p3.moneda
                }).ToListAsync();
            foreach (var reg in resultados)
            {
                if (reg.moneda == moneda)
                {
                    monto = (double)reg.monto_aplicado;
                }
                else
                {
                    monto = (double)(await tipocambio._conversion(_context, moneda, reg.moneda, reg.fechacbza, reg.monto_aplicado));
                }
                resultado += monto;
            }
            resultado = Math.Round(resultado, 2);
            return resultado;
        }

        public async Task<double> Total_Recargos_Por_Deposito_Aplicado_De_Cobranza_En_Proforma(DBContext _context, int codcobranza, int codproforma, string moneda)
        {
            double monto = 0;
            double resultado = 0;

            var resultados = await _context.veproforma
                .Join(_context.verecargoprof,
                      p1 => p1.codigo,
                      p2 => p2.codproforma,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza,
                      temp => temp.p2.codcobranza,
                      p3 => p3.codigo,
                      (temp, p3) => new { temp.p1, temp.p2, p3 })
                .Where(result => result.p2.codcobranza == 0 &&
                                 result.p1.aprobada == true &&
                                 result.p1.transferida == true &&
                                 result.p1.anulada == false &&
                                 result.p3.reciboanulado == false)
                .OrderBy(result => result.p1.id)
                .ThenBy(result => result.p1.numeroid)
                .Select(result => new
                {
                    id_prof = result.p1.id,
                    numeroid_prof = result.p1.numeroid,
                    fecha = result.p1.fecha,
                    monedaprof = result.p1.codmoneda,
                    codproforma = result.p2.codproforma,
                    codrecargo = result.p2.codrecargo,
                    monto_recargo = result.p2.montodoc,
                    codcobranza = result.p2.codcobranza
                }).ToListAsync();

            if (codproforma != 0)
            {
                resultados = resultados.Where(i => i.codproforma != codproforma).ToList();
            }

            foreach (var reg in resultados)
            {
                if (reg.monedaprof == moneda)
                {
                    monto = (double)reg.monto_recargo;
                }
                else
                {
                    monto = (double)(await tipocambio._conversion(_context, moneda, reg.monedaprof, reg.fecha, reg.monto_recargo));
                }
                resultado += monto;
            }
            resultado = Math.Round(resultado, 2);
            return resultado;
        }
        public async Task<bool> Cobranza_Se_Aplico_Para_Descuento_Por_Deposito_2(DBContext _context, int codcobranza, int codproforma)
        {
            if (codcobranza==0)
            {
                return false;
            }

            var query1 = await _context.vedesextraprof
                .Where(v => v.codcobranza == codcobranza && v.codproforma != codcobranza)
                .Select(v => new { 
                    tipo = "PF", 
                    coddoc = v.codproforma,
                    coddesextra = v.coddesextra,
                    porcen = v.porcen,
                    montodoc = v.montodoc,
                    codcobranza = v.codcobranza,
                    codcobranza_contado = v.codcobranza_contado,
                    codanticipo = v.codanticipo
                }).ToListAsync();

            var query2 = await _context.vedesextraremi
                .Where(v => v.codcobranza == codcobranza)
                .Select(v => new { 
                    tipo = "NR", 
                    coddoc = v.codremision,
                    coddesextra = v.coddesextra,
                    porcen = v.porcen,
                    montodoc = v.montodoc,
                    codcobranza = v.codcobranza,
                    codcobranza_contado = v.codcobranza_contado,
                    codanticipo = v.codanticipo
                }).ToListAsync();

            var dt = query1
                .Union(query2).ToList();
            var dt_aux = dt;
            dt_aux.Clear();

            foreach (var reg in dt)
            {
                if (reg.tipo == "PF")
                {
                    //si es proforma verifica en veproforma
                    if (await ventas.Existe_Proforma1(_context, reg.coddoc))
                    {
                        if (! await ventas.proforma_anulada(_context, reg.coddoc))
                        {
                            dt_aux.Add(reg);
                        }
                    }
                }
                else
                {
                    //si es remision verifica en veremision
                    if (await ventas.Existe_NotaRemision1(_context, reg.coddoc))
                    {
                        if (!await ventas.remision_anulada(_context, reg.coddoc))
                        {
                            dt_aux.Add(reg);
                        }
                    }
                }
            }
            dt.Clear();
            dt = dt_aux;
            //si hay registros el resultado es falso
            if (dt.Count() > 0)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> Deposito_Esta_Excluido_por_Venta_Casual_Referencial(DBContext _context, int codcobranza)
        {
            var dt = await _context.cocobranza_deposito_excluidos.Where(i => i.codcobranza == codcobranza).CountAsync();
            if (dt > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> Insertar_Cobranza_Deposito(DBContext _context, Datos_Cbza_Deposito Datos_Deposito, string nomb_ventana, string usuarioreg, DateTime fechareg, string horareg)
        {
            var dt = await _context.cocobranza_deposito.Where(i => i.codcobranza==Datos_Deposito.cod_cbza).ToListAsync();
            if (dt.Count()>0)
            {
                _context.cocobranza_deposito.RemoveRange(dt);
                await _context.SaveChangesAsync();
                // grabar logs

            }
            cocobranza_deposito newReg = new cocobranza_deposito();
            newReg.fechareg = fechareg;
            newReg.horareg = horareg;
            newReg.usuarioreg = usuarioreg;
            newReg.aplicado = false;
            newReg.codcobranza = Datos_Deposito.cod_cbza;

            newReg.codproforma = Datos_Deposito.codproforma;
            newReg.montodist = (decimal)Datos_Deposito.monto_dist;
            newReg.montodescto = (decimal)Datos_Deposito.monto_descto;
            newReg.montorest = (decimal)Datos_Deposito.monto_rest;

            newReg.codcobranza_contado = Datos_Deposito.cod_cbza_contado;
            newReg.codanticipo = Datos_Deposito.cod_anticipo;
            _context.cocobranza_deposito.Add(newReg);
            await _context.SaveChangesAsync();
            // grabar logs
            return true;
        }
    }


    public class Datos_Cbza_Deposito
    {
        public string codcliente { get; set; }
        public string idcbza { get; set; }
        public int nroidcbza { get; set; }
        public int cod_cbza { get; set; }
        public int cod_cbza_contado { get; set; }

        public int cod_anticipo { get; set; }
        public int codproforma { get; set; }
        public double monto_dist { get; set; }
        public double monto_descto { get; set; }
        public double monto_rest { get; set; }
    }
}