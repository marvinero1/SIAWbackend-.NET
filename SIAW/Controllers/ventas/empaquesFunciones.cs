using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SIAW.Data;
using SIAW.Models;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

        public bool GetEsKit(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var kit = _context.initem
                                .Where(a => a.codigo == coditem)
                                .Select(a => new { kit = a.kit })
                                .FirstOrDefault();
                return kit.kit;
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

        public int GetItemReservaParaConjuntos(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var total = _context.inctrlstock
                             .Where(a => a.coditem == coditem)
                             .Select(a => a.coditem)
                             .DefaultIfEmpty()
                             .Count() +
                        _context.inreserva
                             .Where(a => a.coditem == coditem && a.codalmacen == codalmacen)
                             .Select(a => a.coditem)
                             .DefaultIfEmpty()
                             .Count();
                return total;
            }
        }

        public bool IfGetCantidadAprobadasProformas(string userConnectionString, string codempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adparametros = _context.adparametros
                                .Where(a => a.codempresa == codempresa)
                                .Select(a => a.obtener_cantidades_aprobadas_de_proformas)
                                .FirstOrDefault();
                return (bool)adparametros;
            }
        }



        public async Task<List<object>> GetSaldosReservaProforma(string userConnectionString, int codalmacen, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var query = await _context.veproforma
                    .Join(_context.veproforma1, p1 => p1.codigo, p2 => p2.codproforma, (p1, p2) => new { p1, p2 })
                    .Join(_context.inkit, j => j.p2.coditem, p3 => p3.codigo, (j, p3) => new { j.p1, j.p2, p3 })
                    .Join(_context.inkit, j => j.p3.item, p4 => p4.item, (j, p4) => new { j.p1, j.p2, j.p3, p4 })
                    .Where(j => j.p4.codigo == coditem
                                && j.p1.anulada == false
                                && j.p1.transferida == false
                                && j.p1.aprobada == true
                                && j.p1.codalmacen == codalmacen)
                    .GroupBy(j => j.p3.item)
                    .Select(g => new
                    {
                        Item = g.Key,
                        TotalP = g.Sum(x => x.p2.cantaut * x.p3.cantidad)
                    })
                    .ToListAsync();
                return query.Cast<object>().ToList();
            }
        }

        public async Task<List<object>> GetSaldosReservaProformaFromInstoactual(string userConnectionString, int codalmacen, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var query = await _context.instoactual
                                .Join(_context.inkit, s => s.coditem, k => k.item, (s, k) => new { s, k })
                .Where(j => j.s.codalmacen == codalmacen &&
                            j.k.codigo == coditem)
                .Select(j => new
                {
                    j.s.coditem,
                    Cantidad = j.s.cantidad / j.k.cantidad,
                    TotalP = j.s.proformas.HasValue ? j.s.proformas.Value / j.k.cantidad : 0
                })
                .ToListAsync();


                return query.Cast<object>().ToList();
            }
        }

    }
}
