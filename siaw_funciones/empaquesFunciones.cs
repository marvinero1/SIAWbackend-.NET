using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class empaquesFunciones
    {
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
                if (ad_conexion_vpn == null)
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
        public string Getad_conexion_vpnFromDatabase_Sam(DBContext _context, int agencia)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            if (_context.ad_conexion_vpn == null)
            {
                return null;
            }
            var ad_conexion_vpn = _context.ad_conexion_vpn
                .Where(a => a.agencia == agencia.ToString())
                .Select(a => new
                {
                    agencia = a.agencia,
                    servidor_sql = a.servidor_sql,
                    usuario_sql = a.usuario_sql,
                    contrasena_sql = a.contrasena_sql,
                    bd_sql = a.bd_sql
                })
                .FirstOrDefault();
            if (ad_conexion_vpn == null)
            {
                return null;
            }

            var passDesencript = XorString(ad_conexion_vpn.contrasena_sql, "vpn");
            string cadConection = "Data Source=" + ad_conexion_vpn.servidor_sql +
                ";User ID=" + ad_conexion_vpn.usuario_sql +
                ";Password=" + passDesencript +
                ";Connect Timeout=30;Initial Catalog=" + ad_conexion_vpn.bd_sql + ";";

            return cadConection;
            //}
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
        public async Task<instoactual> GetSaldosActual_Sam(DBContext _context, int codalmacen, string coditem)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
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
            //}
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
        public async Task<bool> GetEsKit_sam(DBContext _context, string coditem)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var kit = await _context.initem
                            .Where(a => a.codigo == coditem)
                            .Select(a => new { kit = a.kit })
                            .FirstOrDefaultAsync();
            return kit.kit;
            //}
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
        public async Task<bool> reserv_tuer_porcen(string userConnectionString, string codempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adparametros
                    .Where(c => c.codempresa == codempresa)
                    .Select(c => c.reservar_tuerca_en_porcentaje)
                    .FirstOrDefaultAsync();
                return (bool)result;
            }
        }
        public async Task<bool> reserv_tuer_porcen_Sam(DBContext _context, string codempresa)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.adparametros
                .Where(c => c.codempresa == codempresa)
                .Select(c => c.reservar_tuerca_en_porcentaje)
                .FirstOrDefaultAsync();
            return (bool)result;
            //}
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
        public async Task<List<inctrlstock>> ReservaItemsinKit1_Sam(DBContext _context, string coditem)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.inctrlstock
                .Where(c => c.coditem == coditem)
                .Select(c => new inctrlstock
                {
                    coditemcontrol = c.coditemcontrol,
                    porcentaje = c.porcentaje
                })
                .ToListAsync();
            return result;
            //}
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
        public async Task<List<inreserva>> ReservaItemsinKit2_Sam(DBContext _context, string coditem, int codalmacen)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.inreserva
                .Where(r => r.coditem == coditem && r.codalmacen == codalmacen)
                .ToListAsync();
            return result;
            //}
        }

        // para obtener lo reservado de un item si forma parte de un kit  3     CASO 3 SIA ANTIGUO
        public async Task<inreserva_area> Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta(string userConnectionString, string coditem, int codalmacen)
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
                    .FirstOrDefaultAsync();
                return result;
            }
        }
        public async Task<inreserva_area> Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta_Sam(DBContext _context, string coditem, int codalmacen)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
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
                .FirstOrDefaultAsync();
            return result;
            //}
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
        public async Task<List<inkit>> GetItemsKit_Sam(DBContext _context, string coditem)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
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
            //}
        }


        public async Task<bool> IfGetCantidadAprobadasProformas(string userConnectionString, string codempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adparametros = await _context.adparametros
                                .Where(a => a.codempresa == codempresa)
                                .Select(a => a.obtener_cantidades_aprobadas_de_proformas)
                                .FirstOrDefaultAsync() ?? false;
                return adparametros;
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
        public async Task<List<saldosObj>> GetSaldosReservaProforma_Sam(DBContext _context, int codalmacen, string coditem, bool eskit)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
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

            // }
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
        public async Task<List<saldosObj>> GetSaldosReservaProformaFromInstoactual_Sam(DBContext _context, int codalmacen, string coditem, bool eskit)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
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
            //}
        }
        public async Task<double> getEmpaqueMinimo(string userConnectionString, string coditem, int codintarifa, int codvedescuento)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                double canttarif = 0, cantdesc = 0;

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
                if (resCanttarif != null)
                {
                    canttarif = (double)resCanttarif.cantidad;
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
                    cantdesc = (double)resCantdesc.cantidad;
                }


                if (cantdesc > canttarif)
                {
                    return cantdesc;
                }
                return canttarif;
            }
        }


        public async Task<double> getPesoItem(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                double peso = 0;
                var result = await _context.initem
                   .Where(item => item.codigo == coditem)
                   .Select(item => new
                   {
                       peso = item.peso
                   })
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    peso = (double)result.peso;
                }
                return peso;
            }
        }


        public async Task<double> getPorcentMaxVenta(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                double porcentaje = 0;
                var result = await _context.initem_max_vta
                   .Where(x => x.coditem == coditem && x.codalmacen == codalmacen)
                   .Select(x => new
                   {
                       porcen_maximo = x.porcen_maximo
                   })
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    porcentaje = (double)result.porcen_maximo;
                }
                return porcentaje;
            }
        }


        public async Task<double> getSaldoMinimo(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.initem
                   .Where(item => item.codigo == coditem)
                   .Select(item => item.saldominimo)
                   .FirstOrDefaultAsync();

                return (double)result;
            }
        }
        public async Task<double> getSaldoMinimo_Sam(DBContext _context, string coditem)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.initem
               .Where(item => item.codigo == coditem)
               .Select(item => item.saldominimo)
               .FirstOrDefaultAsync();

            return (double)result;
            //}
        }
        public async Task<bool> getValidaIngreSolurgente(string userConnectionString, string codempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adparametros
                   .Where(item => item.codempresa == codempresa)
                   .Select(item => item.validar_ingresos_solurgentes)
                   .FirstOrDefaultAsync();

                return (bool)result;
            }
        }
        public async Task<bool> getValidaIngreSolurgente_Sam(DBContext _context, string codempresa)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.adparametros
               .Where(item => item.codempresa == codempresa)
               .Select(item => item.validar_ingresos_solurgentes)
               .FirstOrDefaultAsync();

            return (bool)result;
            //}
        }
        public async Task<bool> esAlmacen(string userConnectionString, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                bool esAlmacen = false;
                var result = await _context.inalmacen
                   .Where(item => item.codigo == codalmacen && item.tienda == true)
                   .Select(item => new
                   {
                       item.codigo
                   })
                   .FirstOrDefaultAsync();
                if (result == null)
                {
                    esAlmacen = true;
                }

                return esAlmacen;
            }
        }
        public async Task<bool> esAlmacen_Sam(DBContext _context, int codalmacen)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            bool esAlmacen = false;
            var result = await _context.inalmacen
               .Where(item => item.codigo == codalmacen && item.tienda == true)
               .Select(item => new
               {
                   item.codigo
               })
               .FirstOrDefaultAsync();
            if (result == null)
            {
                esAlmacen = true;
            }

            return esAlmacen;
            //}
        }

        public async Task<bool> ve_detalle_saldo_variable(string userConnectionString, string usuario)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adusparametros
                   .Where(item => item.usuario == usuario)
                   .Select(item => new
                   {
                       item.ver_detalle_saldo_variable
                   })
                   .FirstOrDefaultAsync();
                if (result == null)
                {
                    return false;
                }

                return (bool)result.ver_detalle_saldo_variable;
            }
        }

        public async Task<inreserva_area> get_inreserva_area(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var codarea = await _context.inalmacen
                    .Where(a => a.codigo == codalmacen)
                    .Select(a => a.codarea)
                    .FirstOrDefaultAsync();


                var result = await _context.inreserva_area
                   .Where(i => i.coditem == coditem && i.codarea == codarea)
                   .Select(i => new inreserva_area
                   {
                       codarea = i.codarea,
                       coditem = i.coditem,
                       promvta = i.promvta,
                       smin = i.smin,
                       porcenvta = i.porcenvta,
                       saldo = i.saldo
                   })
                   .FirstOrDefaultAsync();

                return result;
            }
        }


        /*
         * var subquery = await _context.inalmacen
                    .Where(a => a.codigo == codalmacen)
                    .Select(a => a.codarea)
                    .FirstOrDefaultAsync();
         * 
         * 
         * 
         * 
         */


    }
}
