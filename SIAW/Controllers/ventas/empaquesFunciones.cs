﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SIAW.Data;
using SIAW.Models;
using SIAW.Models_Extra;
using System.Collections.Generic;
using System.Text;

namespace SIAW.Controllers.ventas
{
    public class empaquesFunciones
    {

        public string Getad_conexion_vpnFromDatabase(string userConnectionString, string agencia)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.ad_conexion_vpn == null)
                {
                    return null;
                }
                var ad_conexion_vpn = _context.ad_conexion_vpn
                    .Where(a => a.agencia == agencia)
                    .Select(a => new
                    {
                        agencia = a.agencia,
                        servidor_sql = a.servidor_sql,
                        usuario_sql = a.usuario_sql,
                        contrasena_sql = a.contrasena_sql,
                        bd_sql = a.bd_sql
                    })
                    .FirstOrDefault();
                if (ad_conexion_vpn==null)
                {
                    return null;
                }

                var passDesencript = XorString(ad_conexion_vpn.contrasena_sql, "vpn");
                string cadConection = "Data Source=" + ad_conexion_vpn.servidor_sql +
                    ";User ID=" + ad_conexion_vpn.usuario_sql +
                    ";Password=" + passDesencript +
                    ";Connect Timeout=30;Initial Catalog=" + ad_conexion_vpn.bd_sql + ";";

                return cadConection;
            }
        }

        static string XorString(string targetString, string maskValue)
        {
            int index = 0;
            StringBuilder returnValue = new StringBuilder();

            foreach (char charValue in targetString.ToCharArray())
            {
                int maskCharCode = maskValue[index % maskValue.Length];
                int xorResult = charValue ^ maskCharCode;
                returnValue.Append((char)xorResult);

                index = (index + 1) % maskValue.Length;
            }

            return returnValue.ToString();
        }



        public async Task<instoactual> GetSaldosActual(string userConnectionString, int codalmacen, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var instoactual = await _context.instoactual
                                .Where(a => a.codalmacen == codalmacen && a.coditem == coditem)
                                .Select(a => new instoactual
                                {
                                    codalmacen = a.codalmacen,
                                    coditem = a.coditem,
                                    cantidad = a.cantidad
                                })
                                .FirstOrDefaultAsync();
                return instoactual;
            }
        }

        public async Task<bool> GetEsKit(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var kit = await _context.initem
                                .Where(a => a.codigo == coditem)
                                .Select(a => new { kit = a.kit })
                                .FirstOrDefaultAsync();
                return kit.kit;
            }
        }

        // para determinar si el item es parte de un kit o no 
        public async Task<bool> IteminKits(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inctrlstock
                    .Where(c => c.coditem == coditem)
                    .Select(c => new { nro = c.coditem != null ? 1 : 0 })
                    .Union(_context.inreserva
                        .Where(r => r.coditem == coditem && r.codalmacen == codalmacen)
                        .Select(r => new { nro = r.coditem != null ? 1 : 0 }))
                    .Select(x => x.nro)
                    .SumAsync();
                if (result > 0) { return true; }
                return false;
            }
        }







        // para obtener lo reservado de un item si forma parte de un kit  CASO TUERCA     CASO 1 SIA ANTIGUO
        public async Task<List<inctrlstock>> ReservaItemsinKit1(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inctrlstock
                    .Where(c => c.coditem == coditem)
                    .Select(c => new inctrlstock
                    {
                        coditemcontrol = c.coditemcontrol,
                        porcentaje = c.porcentaje
                    })
                    .ToListAsync();
                return result;
            }
        }



        // para obtener lo reservado de un item si forma parte de un kit  2     CASO 2 SIA ANTIGUO
        public async Task<List<inreserva>> ReservaItemsinKit2(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inreserva
                    .Where(r => r.coditem == coditem && r.codalmacen == codalmacen)
                    .ToListAsync();
                return result;
            }
        }


        // para obtener lo reservado de un item si forma parte de un kit  3     CASO 3 SIA ANTIGUO
        public async Task<List<inreserva_area>> ReservaItemsinKit3(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var subquery = await _context.inalmacen
                    .Where(a => a.codigo == codalmacen)
                    .Select(a => a.codarea)
                    .FirstOrDefaultAsync();


                var result = await _context.inreserva_area
                    .Where(ra => ra.codarea == subquery && ra.coditem == coditem)
                    .Select(ra => new inreserva_area
                    {
                        codarea = ra.codarea,
                        coditem = ra.coditem,
                        promvta = ra.promvta,
                        smin = ra.smin,
                        porcenvta = ra.porcenvta,
                        saldo = ra.saldo
                    })
                    .ToListAsync();
                return result;
            }
        }








        public async Task<List<inkit>> GetItemsKit(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var items = await _context.inkit
                                .Where(a => a.codigo == coditem)
                                .Select(a => new inkit
                                {
                                    codigo = a.codigo,
                                    item = a.item,
                                    cantidad = a.cantidad,
                                    unidad = a.unidad
                                })
                                .ToListAsync();
                return items;
            }
        }

       

        public async Task<bool> IfGetCantidadAprobadasProformas(string userConnectionString, string codempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adparametros = await _context.adparametros
                                .Where(a => a.codempresa == codempresa)
                                .Select(a => a.obtener_cantidades_aprobadas_de_proformas)
                                .FirstOrDefaultAsync();
                return (bool)adparametros;
            }
        }



        public async Task<List<saldosObj>> GetSaldosReservaProforma(string userConnectionString, int codalmacen, string coditem, bool eskit)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                List<saldosObj> query = null;
                if (eskit)
                {
                    query = await _context.veproforma
                    .Join(_context.veproforma1, p1 => p1.codigo, p2 => p2.codproforma, (p1, p2) => new { p1, p2 })
                    .Join(_context.inkit, j => j.p2.coditem, p3 => p3.codigo, (j, p3) => new { j.p1, j.p2, p3 })
                    .Join(_context.inkit, j => j.p3.item, p4 => p4.item, (j, p4) => new { j.p1, j.p2, j.p3, p4 })
                    .Where(j => j.p4.codigo == coditem
                                && j.p1.anulada == false
                                && j.p1.transferida == false
                                && j.p1.aprobada == true
                                && j.p1.codalmacen == codalmacen)
                    .GroupBy(j => j.p3.item)
                    .Select(g => new saldosObj
                    {
                        coditem = g.Key,
                        TotalP = (decimal)g.Sum(x => x.p2.cantaut * x.p3.cantidad)
                    })
                    .ToListAsync();
                }
                else
                {
                    query = await _context.veproforma
                    .Join(_context.veproforma1, p1 => p1.codigo, p2 => p2.codproforma, (p1, p2) => new { p1, p2 })
                    .Join(_context.inkit, j => j.p2.coditem, p3 => p3.codigo, (j, p3) => new { j.p1, j.p2, p3 })
                    .Where(j => j.p3.item == coditem
                        && _context.inkit.Where(k => k.item == coditem).Select(k => k.codigo).Contains(j.p2.coditem)
                        && j.p1.anulada == false
                        && j.p1.transferida == false
                        && j.p1.aprobada == true
                        && j.p1.codalmacen == codalmacen)
                    .GroupBy(j => j.p3.item)
                    .Select(g => new saldosObj
                    {
                        coditem = g.Key,
                        TotalP = (decimal)g.Sum(x => x.p2.cantaut * x.p3.cantidad)
                    })
                    .ToListAsync();
                }
                return query;

            }
        }

        public async Task<List<saldosObj>> GetSaldosReservaProformaFromInstoactual(string userConnectionString, int codalmacen, string coditem, bool eskit)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                List<saldosObj> query = null;
                if (eskit)
                {
                    query = await _context.instoactual
                                .Join(_context.inkit, s => s.coditem, k => k.item, (s, k) => new { s, k })
                .Where(j => j.s.codalmacen == codalmacen &&
                            j.k.codigo == coditem)
                .Select(j => new saldosObj
                {
                    coditem = j.s.coditem,
                    Cantidad = (decimal)(j.s.cantidad / j.k.cantidad),
                    TotalP = (decimal)(j.s.proformas.HasValue ? j.s.proformas.Value / j.k.cantidad : 0)
                })
                .ToListAsync();
                }
                else
                {
                    query = await _context.instoactual
                        .Where(i => i.coditem == coditem && i.codalmacen == codalmacen)
                        .Select(i => new saldosObj
                        {
                            TotalP = i.proformas ?? 0,
                            coditem = i.coditem
                        })
                        .ToListAsync(); 
                }

                return query;
            }
        }

        public async Task<float> getEmpaqueMinimo(string userConnectionString, string coditem, int codintarifa, int codvedescuento)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                float canttarif = 0, cantdesc = 0;
                
                var resCanttarif = await _context.veempaque1
                    .Join(_context.intarifa,
                          e => e.codempaque,
                          t => t.codempaque,
                          (e, t) => new { e, t })
                    .Where(j => j.e.item == coditem && j.t.codigo == codintarifa)
                    .Select(j => new
                    {
                        cantidad = j.e.cantidad
                    })
                    .FirstOrDefaultAsync();
                if (resCanttarif!=null)
                {
                    canttarif = (float)resCanttarif.cantidad;
                }
                var resCantdesc = await _context.veempaque1
                    .Join(_context.vedescuento,
                          e => e.codempaque,
                          d => d.codempaque,
                          (e, d) => new { e, d })
                    .Where(j => j.e.item == coditem && j.d.codigo == codvedescuento)
                    .Select(j => new
                    {
                        cantidad = j.e.cantidad
                    })
                    .FirstOrDefaultAsync();
                if (resCantdesc != null)
                {
                    cantdesc = (float)resCantdesc.cantidad;
                }


                if (cantdesc > canttarif)
                {
                    return cantdesc;
                }
                return canttarif;
            }
        }


        public async Task<float> getPesoItem(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                float peso = 0;
                var result = await _context.initem
                   .Where(item => item.codigo == coditem)
                   .Select(item => new
                   {
                       peso = item.peso
                   })
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    peso = (float)result.peso;
                }
                return peso;
            }
        }


        public async Task<float> getPorcentMaxVenta(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                float porcentaje = 0;
                var result = await _context.initem_max_vta
                   .Where(x => x.coditem == coditem && x.codalmacen == codalmacen)
                   .Select(x => new
                   {
                       porcen_maximo = x.porcen_maximo
                   })
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    porcentaje = (float)result.porcen_maximo;
                }
                return porcentaje;
            }
        }
    }
}
