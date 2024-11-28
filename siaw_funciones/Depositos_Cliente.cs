using System;
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
        private readonly TipoCambio tipocambio = new TipoCambio();
        private readonly Log log = new Log();
        private readonly Configuracion configuracion = new Configuracion();
        //private readonly Ventas ventas = new Ventas();
        //private readonly IVentas ventas;
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        /*
        public Depositos_Cliente(IVentas _ventas)
        {
            ventas = _ventas;
        }*/
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
        public async Task<string[]> IdNroid_Deposito_Asignado_Anticipo(DBContext _context, string id, int numeroid)
        {
            //List<string> resultado = new List<string>();
            string[] resultado = new string[2];
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.coanticipo
                .Where(v => v.id == id && v.numeroid == numeroid && v.deposito_cliente == true && v.anulado == false)
                .Select(v => new { v.iddeposito, v.numeroiddeposito })
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado[0] = result.iddeposito.ToString();
                    resultado[1] = result.numeroiddeposito.ToString();
                }
                else
                {
                    resultado[0] = "NSE";
                    resultado[1] = "0";
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                resultado[0] = "NSE";
                resultado[1] = "0";
            }
            return resultado;
        }

        public async Task<string[]> IdNroid_Deposito_Asignado_Cobranza(DBContext _context, string idcbza, int nroidcbza)
        {
            //List<string> resultado = new List<string>();
            string[] resultado = new string[2];
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.cocobranza
                .Where(v => v.id == idcbza && v.numeroid == nroidcbza
                && v.deposito_cliente == true && v.reciboanulado == false)
                .Select(v => new { iddeposito = v.iddeposito, numeroiddeposito = v.numeroiddeposito ?? 0 })
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado[0] = result.iddeposito.ToString();
                    resultado[1] = result.numeroiddeposito.ToString();
                }
                else
                {
                    resultado[0] = "NSE";
                    resultado[1] = "0";
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                resultado[0] = "NSE";
                resultado[1] = "0";
            }
            return resultado;
        }

        public async Task<string[]> IdNroid_Deposito_Asignado_Cobranza_Contado(DBContext _context, string idcbza, int nroidcbza)
        {
            //List<string> resultado = new List<string>();
            string[] resultado = new string[2];
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.cocobranza_contado
                .Where(v => v.id == idcbza && v.numeroid == nroidcbza
                && v.deposito_cliente == true && v.reciboanulado == false)
                .Select(v => new { iddeposito = v.iddeposito, numeroiddeposito = v.numeroiddeposito ?? 0 })
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado[0] = result.iddeposito.ToString();
                    resultado[1] = result.numeroiddeposito.ToString();
                }
                else
                {
                    resultado[0] = "NSE";
                    resultado[1] = "0";
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                resultado[0] = "NSE";
                resultado[1] = "0";
            }
            return resultado;
        }

        public async Task<string> Anticipo_Asignado_A_Deposito_a_Proforma(DBContext _context, string iddeposito, string nroiddeposito, bool para_pf)
        {
            string resultado = "";
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var dtanticipos = _context.coanticipo
                .Where(a => a.anulado == false && a.iddeposito == iddeposito && a.numeroiddeposito == int.Parse(nroiddeposito))
                .Select(v => new { v.id, v.numeroid })
                .ToList();

                if (dtanticipos.Count == 0)
                {
                    resultado = "";
                }
                else if (dtanticipos.Count > 0)
                {
                    foreach (var anticipo in dtanticipos)
                    {
                        resultado = anticipo.id + "-" + anticipo.numeroid;
                        if (para_pf == true)
                        {
                            var qry = from p1 in _context.coanticipo
                                      join p2 in _context.cocobranza_anticipo on p1.codigo equals p2.codanticipo
                                      join p3 in _context.cocobranza on p2.codcobranza equals p3.codigo
                                      where p1.id == anticipo.id && p1.numeroid == anticipo.numeroid && p3.reciboanulado == false
                                      orderby p3.fecha
                                      select new { p1.id, p1.numeroid, p1.monto, monto_rever = p2.monto, IdCb = p3.id, NroidCb = p3.numeroid, p3.fecha, Monto_cb = p3.monto };

                            foreach (var rever in qry)
                            {
                                resultado += "->" + rever.IdCb + "-" + rever.NroidCb + "  ";
                            }
                        }
                        var qry2 = from p1 in _context.coanticipo
                                   join p2 in _context.veproforma_anticipo on p1.codigo equals p2.codanticipo
                                   join p3 in _context.veproforma on p2.codproforma equals p3.codigo
                                   where p1.id == anticipo.id && p1.numeroid == anticipo.numeroid && p3.anulada == false && p3.aprobada == true && p3.paraaprobar == true
                                   orderby p3.fecha
                                   select new { p1.id, p1.numeroid, p1.monto, MontoRever = p2.monto, IdPf = p3.id, NroidPf = p3.numeroid, p3.fecha, p3.total };

                        foreach (var proforma in qry2)
                        {
                            resultado += "->" + proforma.IdPf + "-" + proforma.NroidPf + "  ";
                        }
                    }
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                resultado = "NSE";
            }
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
                    if (await Ventas.Existe_Proforma1(_context, reg.coddoc))
                    {
                        if (! await Ventas.proforma_anulada(_context, reg.coddoc))
                        {
                            dt_aux.Add(reg);
                        }
                    }
                }
                else
                {
                    //si es remision verifica en veremision
                    if (await Ventas.Existe_NotaRemision1(_context, reg.coddoc))
                    {
                        if (!await Ventas.remision_anulada(_context, reg.coddoc))
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
                await log.RegistrarEvento(_context, usuarioreg, Log.Entidades.SW_Ventana, Datos_Deposito.codcliente, Datos_Deposito.codcliente, "", nomb_ventana, "Se elimino la cobranza:" + Datos_Deposito.idcbza + "-" + Datos_Deposito.nroidcbza + " en cocobranza_deposito.", Log.TipoLog.Edicion);

            }
            // insertar el registro en cocobranza_deposito
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
            await log.RegistrarEvento(_context, usuarioreg, Log.Entidades.SW_Ventana, Datos_Deposito.codcliente, Datos_Deposito.codcliente, "", nomb_ventana, "Se registro la  cobranza " + Datos_Deposito.idcbza + "-" + Datos_Deposito.nroidcbza + " en cocobranza deposito con el monto descuento de: " + Datos_Deposito.monto_descto.ToString(), Log.TipoLog.Edicion);
            return true;
        }

        public async Task<double> Total_Cobranza_Credito_Ajustado(DBContext _context, int codcobranza, string codmoneda)
        {
            var dt = await _context.cocobranza_deposito_ajuste.Where(i => i.codcobranza_ajustada == codcobranza).SumAsync(i => i.monto);
            return (double)dt;
        }

        
        public async Task<(bool result, string msgAlert)> Validar_Desctos_x_Deposito_Otorgados_De_Cobranzas_Credito(DBContext _context, string idpf, int nroidpf, string codempresa)
        {
            bool resultado = false;
            string msgAlert = "";
            int coddesextra_deposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);

            //TODO EL ANALISIS DE LOS MONTOS APLICADOS DE UNA CBZA-DEPOSITO SE REALIZA EN LA MONEDA DE LA CBZA-DPOSITO
            //ES DECIR SI HAY UNA PROROFMA EN bs Y LOS MONTOS DE DESCTO POR DEPOSITO ESTAN EN BS
            //SE CONVERTIRAN A $US

            /////////////////////////////////////////////////////////////////////////////////////
            //1ª primero verificar si la proforma tiene desctos por deposito y con que cbzas
            /////////////////////////////////////////////////////////////////////////////////////
            var dt1 = await _context.vedesextraprof
                .Join(_context.veproforma,
                      p1 => p1.codproforma,
                      p2 => p2.codigo,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza,
                      combined => combined.p1.codcobranza,
                      p3 => p3.codigo,
                      (combined, p3) => new
                      {
                          combined.p1,
                          combined.p2,
                          p3
                      })
                .Where(x => x.p2.id == idpf
                         && x.p2.numeroid == nroidpf
                         && x.p1.coddesextra == coddesextra_deposito)
                .Select(x => new
                {
                    id = x.p2.id,
                    numeroid = x.p2.numeroid,
                    fecha_pf = x.p2.fecha,
                    montodoc = x.p1.montodoc,
                    codmoneda_pf = x.p2.codmoneda,
                    codmoneda_cb = x.p3.moneda,
                    codcobranza = x.p1.codcobranza
                })
                .ToListAsync();

            // si la proforma no tiene desctos por deposito se sale de la rutina, porque no hay nada que controlar
            if (dt1.Count() == 0)
            {
                return (true, "");
            }
            foreach (var reg in dt1)
            {
                double MONTO_APLICADO_ESTA_PROF = 0;
                string MON_APLICADO = "";
                double dif_tolerancia = 0;

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //si tiene desctos por deposito revisar de cada CBZA si esta bien aplicado el descto por deposito
                if (reg.codmoneda_pf != reg.codmoneda_cb)
                {
                    MONTO_APLICADO_ESTA_PROF = (double)await tipocambio._conversion(_context, reg.codmoneda_cb, reg.codmoneda_pf, reg.fecha_pf, reg.montodoc);
                    MON_APLICADO = reg.codmoneda_cb;
                    dif_tolerancia = 0.05;
                }
                else
                {
                    MONTO_APLICADO_ESTA_PROF = (double)reg.montodoc;
                    MON_APLICADO = reg.codmoneda_cb;
                    dif_tolerancia = 0.05;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //2º verificar cuanto es el monto dist y el monto que se puede aplicar de cada cbza de la cual se aplico descto por deposito
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var dt2 = await _context.cocobranza_deposito
                    .Join(_context.cocobranza,
                          p1 => p1.codcobranza,
                          p2 => p2.codigo,
                          (p1, p2) => new { p1, p2 })
                    .Where(x => x.p1.codcobranza == reg.codcobranza)
                    .Select(x => new
                    {
                        codcbza = x.p2.codigo,
                        id = x.p2.id,
                        numeroid = x.p2.numeroid,
                        fecha = x.p2.fecha,
                        montobza = x.p2.monto,
                        moneda = x.p2.moneda,
                        //p1 = x.p1,
                        montodescto = x.p1.montodescto,
                    })
                    .ToListAsync();
                double MONTO_LIMITE = 0;
                string MON_MONTO_LIMITE = "";
                if (dt2.Count() > 0)
                {
                    MONTO_LIMITE = (double)dt2[0].montodescto;
                    MON_MONTO_LIMITE = dt2[0].moneda;
                }
                else
                {
                    // si no hay registro en cocobranza_deposito algo esta mal y mejor no aprobar la
                    // proforma
                    return (false, "");
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // 3ª totalizar los desctos por depositos que se otorgaron con esta CBZA-DEPOSITO
                // y que ya estan facturados
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var dt3 = await _context.vedesextraprof
                    .Join(_context.veproforma,
                          p1 => p1.codproforma,
                          p2 => p2.codigo,
                          (p1, p2) => new { p1, p2 })
                    .Join(_context.veremision,
                          combined => combined.p2.codigo,
                          p3 => p3.codproforma,
                          (combined, p3) => new { combined.p1, combined.p2, p3 })
                    .Where(x => x.p1.codcobranza == reg.codcobranza && x.p2.anulada == false && x.p3.anulada == false)
                    .Select(x => new
                    {
                        codproforma = x.p2.codigo,
                        idpf = x.p2.id,
                        nroidpf = x.p2.numeroid,
                        fecha_pf = x.p2.fecha,
                        codcliente = x.p2.codcliente,
                        id = x.p3.id,
                        numeroid = x.p3.numeroid,
                        fecha = x.p3.fecha,
                        montodoc = x.p1.montodoc,
                        codmoneda = x.p2.codmoneda
                    })
                    .ToListAsync();

                double MONTO_TTL_APLICADO = 0;
                foreach (var reg1 in dt3)
                {
                    double MONTO_aux_LIMITE = 0;
                    // conversion de moneda
                    if (reg1.codmoneda != MON_MONTO_LIMITE)
                    {
                        MONTO_aux_LIMITE = (double)await tipocambio._conversion(_context, MON_MONTO_LIMITE, reg1.codmoneda, reg1.fecha_pf, reg1.montodoc);
                    }
                    else
                    {
                        MONTO_aux_LIMITE = (double)reg1.montodoc;
                    }
                    MONTO_TTL_APLICADO += MONTO_aux_LIMITE;
                }
                // EL TOTAL EFECTIVAMENTE APLICADO Y FACTURADO MAS LO QUE SE QUIERE APROBAR
                MONTO_TTL_APLICADO += MONTO_APLICADO_ESTA_PROF;
                MONTO_TTL_APLICADO = Math.Round(MONTO_TTL_APLICADO, 2);

                // CONTROLAR SI EL TOTAL FACTUTRADO MAS LO QUE FACTURARA SI ESTO SE APRUEBA
                MONTO_LIMITE = MONTO_LIMITE + dif_tolerancia;
                if (MONTO_TTL_APLICADO > MONTO_LIMITE)
                {
                    msgAlert = "El monto total de descuentos por deposito aplicados del Deposito: " + dt2[0].id + "-" + dt2[0].numeroid + " es mayor al disponible. Se aplico en esta proforma y otras un total de: " + MONTO_TTL_APLICADO + "(" + MON_MONTO_LIMITE + ") y el maximo aplicable es: " + MONTO_LIMITE + " verifique los descuentos por deposito aplicados del mencionado deposito.";
                    resultado = false;
                }
                else
                {
                    resultado = true;
                }
            }

            return (resultado, msgAlert);
        }



        public async Task<(bool result, string msgAlert)> Validar_Desctos_x_Deposito_Otorgados_De_Cbzas_Contado_CE(DBContext _context, string idpf, int nroidpf, string codempresa)
        {
            bool resultado = false;
            string msgAlert = "";
            int coddesextra_deposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);

            //TODO EL ANALISIS DE LOS MONTOS APLICADOS DE UNA CBZA-DEPOSITO SE REALIZA EN LA MONEDA DE LA CBZA-DPOSITO
            //ES DECIR SI HAY UNA PROROFMA EN bs Y LOS MONTOS DE DESCTO POR DEPOSITO ESTAN EN BS
            //SE CONVERTIRAN A $US

            /////////////////////////////////////////////////////////////////////////////////////
            //1ª primero verificar si la proforma tiene desctos por deposito y con que cbzas
            /////////////////////////////////////////////////////////////////////////////////////
            var dt1 = await _context.vedesextraprof
                .Join(_context.veproforma,
                      p1 => p1.codproforma,
                      p2 => p2.codigo,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza_contado,
                      combined => combined.p1.codcobranza_contado,
                      p3 => p3.codigo,
                      (combined, p3) => new
                      {
                          combined.p1,
                          combined.p2,
                          p3
                      })
                .Where(x => x.p2.id == idpf
                         && x.p2.numeroid == nroidpf
                         && x.p1.coddesextra == coddesextra_deposito)
                .Select(x => new
                {
                    id = x.p2.id,
                    numeroid = x.p2.numeroid,
                    fecha_pf = x.p2.fecha,
                    montodoc = x.p1.montodoc,
                    codmoneda_pf = x.p2.codmoneda,
                    codmoneda_cb = x.p3.moneda,
                    codcobranza_contado = x.p1.codcobranza_contado
                })
                .ToListAsync();

            // si la proforma no tiene desctos por deposito se sale de la rutina, porque no hay nada que controlar
            if (dt1.Count() == 0)
            {
                return (true, "");
            }
            foreach (var reg in dt1)
            {
                double MONTO_APLICADO_ESTA_PROF = 0;
                string MON_APLICADO = "";
                double dif_tolerancia = 0;

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //si tiene desctos por deposito revisar de cada CBZA si esta bien aplicado el descto por deposito
                if (reg.codmoneda_pf != reg.codmoneda_cb)
                {
                    MONTO_APLICADO_ESTA_PROF = (double)await tipocambio._conversion(_context, reg.codmoneda_cb, reg.codmoneda_pf, reg.fecha_pf, reg.montodoc);
                    MON_APLICADO = reg.codmoneda_cb;
                }
                else
                {
                    MONTO_APLICADO_ESTA_PROF = (double)reg.montodoc;
                    MON_APLICADO = reg.codmoneda_cb;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //2º verificar cuanto es el monto dist y el monto que se puede aplicar de cada cbza de la cual se aplico descto por deposito
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var dt2 = await _context.cocobranza_deposito
                    .Join(_context.cocobranza_contado,
                          p1 => p1.codcobranza_contado,
                          p2 => p2.codigo,
                          (p1, p2) => new { p1, p2 })
                    .Where(x => x.p1.codcobranza_contado == reg.codcobranza_contado)
                    .Select(x => new
                    {
                        codcbza = x.p2.codigo,
                        id = x.p2.id,
                        numeroid = x.p2.numeroid,
                        fecha = x.p2.fecha,
                        montobza = x.p2.monto,
                        moneda = x.p2.moneda,
                        //p1 = x.p1,
                        montodescto = x.p1.montodescto,
                    })
                    .ToListAsync();
                double MONTO_LIMITE = 0;
                string MON_MONTO_LIMITE = "";
                if (dt2.Count() > 0)
                {
                    MONTO_LIMITE = (double)dt2[0].montodescto;
                    MON_MONTO_LIMITE = dt2[0].moneda;
                }
                else
                {
                    // si no hay registro en cocobranza_deposito algo esta mal y mejor no aprobar la
                    // proforma
                    return (false, "");
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // 3ª totalizar los desctos por depositos que se otorgaron con esta CBZA-DEPOSITO
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var dt3 = await _context.vedesextraprof
                    .Join(_context.veproforma,
                          p1 => p1.codproforma,
                          p2 => p2.codigo,
                          (p1, p2) => new { p1, p2 })
                    .Join(_context.veremision,
                          combined => combined.p2.codigo,
                          p3 => p3.codproforma,
                          (combined, p3) => new { combined.p1, combined.p2, p3 })
                    .Where(x => x.p1.codcobranza_contado == reg.codcobranza_contado && x.p2.anulada == false && x.p3.anulada == false)
                    .Select(x => new
                    {
                        codproforma = x.p2.codigo,
                        idpf = x.p2.id,
                        nroidpf = x.p2.numeroid,
                        fecha_pf = x.p2.fecha,
                        codcliente = x.p2.codcliente,
                        id = x.p3.id,
                        numeroid = x.p3.numeroid,
                        fecha = x.p3.fecha,
                        montodoc = x.p1.montodoc,
                        codmoneda = x.p2.codmoneda
                    })
                    .ToListAsync();

                double MONTO_TTL_APLICADO = 0;
                foreach (var reg1 in dt3)
                {
                    double MONTO_aux_LIMITE = 0;
                    // conversion de moneda
                    if (reg1.codmoneda != MON_MONTO_LIMITE)
                    {
                        MONTO_aux_LIMITE = (double)await tipocambio._conversion(_context, MON_MONTO_LIMITE, reg1.codmoneda, reg1.fecha_pf, reg1.montodoc);
                    }
                    else
                    {
                        MONTO_aux_LIMITE = (double)reg1.montodoc;
                    }
                    MONTO_TTL_APLICADO += MONTO_aux_LIMITE;
                }
                // EL TOTAL EFECTIVAMENTE APLICADO Y FACTURADO MAS LO QUE SE QUIERE APROBAR
                MONTO_TTL_APLICADO += MONTO_APLICADO_ESTA_PROF;
                MONTO_TTL_APLICADO = Math.Round(MONTO_TTL_APLICADO, 2);

                // CONTROLAR SI EL TOTAL FACTUTRADO MAS LO QUE FACTURARA SI ESTO SE APRUEBA
                if (MONTO_TTL_APLICADO > MONTO_LIMITE)
                {
                    msgAlert = "El monto total de descuentos por deposito aplicados del Deposito: " + dt2[0].id + "-" + dt2[0].numeroid + " es mayor al disponible. Se aplico en esta proforma y otras un total de: " + MONTO_TTL_APLICADO + "(" + MON_MONTO_LIMITE + ") y el maximo aplicable es: " + MONTO_LIMITE + " verifique los descuentos por deposito aplicados del mencionado deposito.";
                    resultado = false;
                }
                else
                {
                    resultado = true;
                }
            }

            return (resultado, msgAlert);
        }






        public async Task<(bool result, string msgAlert)> Validar_Desctos_x_Deposito_Otorgados_De_Anticipos_Que_Pagaron_Proformas_Contado(DBContext _context, string idpf, int nroidpf, string codempresa)
        {
            bool resultado = false;
            string msgAlert = "";
            int coddesextra_deposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);

            //TODO EL ANALISIS DE LOS MONTOS APLICADOS DE UNA CBZA-DEPOSITO SE REALIZA EN LA MONEDA DE LA CBZA-DPOSITO
            //ES DECIR SI HAY UNA PROROFMA EN bs Y LOS MONTOS DE DESCTO POR DEPOSITO ESTAN EN BS
            //SE CONVERTIRAN A $US

            /////////////////////////////////////////////////////////////////////////////////////
            //1ª primero verificar si la proforma tiene desctos por deposito y con que cbzas
            /////////////////////////////////////////////////////////////////////////////////////
            var dt1 = await _context.vedesextraprof
                .Join(_context.veproforma,
                      p1 => p1.codproforma,
                      p2 => p2.codigo,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.coanticipo,
                      combined => combined.p1.codanticipo,
                      p3 => p3.codigo,
                      (combined, p3) => new
                      {
                          combined.p1,
                          combined.p2,
                          p3
                      })
                .Where(x => x.p2.id == idpf
                         && x.p2.numeroid == nroidpf
                         && x.p1.coddesextra == coddesextra_deposito)
                .Select(x => new
                {
                    id = x.p2.id,
                    numeroid = x.p2.numeroid,
                    fecha_pf = x.p2.fecha,
                    montodoc = x.p1.montodoc,
                    codmoneda_pf = x.p2.codmoneda,
                    codmoneda_cb = x.p3.codmoneda,
                    codanticipo = x.p1.codanticipo
                })
                .ToListAsync();

            // si la proforma no tiene desctos por deposito se sale de la rutina, porque no hay nada que controlar
            if (dt1.Count() == 0)
            {
                return (true, "");
            }
            foreach (var reg in dt1)
            {
                double MONTO_APLICADO_ESTA_PROF = 0;
                string MON_APLICADO = "";
                double dif_tolerancia = 0;

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //si tiene desctos por deposito revisar de cada CBZA si esta bien aplicado el descto por deposito
                if (reg.codmoneda_pf != reg.codmoneda_cb)
                {
                    MONTO_APLICADO_ESTA_PROF = (double)await tipocambio._conversion(_context, reg.codmoneda_cb, reg.codmoneda_pf, reg.fecha_pf, reg.montodoc);
                    MON_APLICADO = reg.codmoneda_cb;
                }
                else
                {
                    MONTO_APLICADO_ESTA_PROF = (double)reg.montodoc;
                    MON_APLICADO = reg.codmoneda_cb;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //2º verificar cuanto es el monto dist y el monto que se puede aplicar de cada cbza de la cual se aplico descto por deposito
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var dt2 = await _context.cocobranza_deposito
                    .Join(_context.coanticipo,
                          p1 => p1.codanticipo,
                          p2 => p2.codigo,
                          (p1, p2) => new { p1, p2 })
                    .Where(x => x.p1.codanticipo == reg.codanticipo)
                    .Select(x => new
                    {
                        codcbza = x.p2.codigo,
                        id = x.p2.id,
                        numeroid = x.p2.numeroid,
                        fecha = x.p2.fecha,
                        montobza = x.p2.monto,
                        moneda = x.p2.codmoneda,
                        //p1 = x.p1,
                        montodescto = x.p1.montodescto,
                    })
                    .ToListAsync();
                double MONTO_LIMITE = 0;
                string MON_MONTO_LIMITE = "";
                if (dt2.Count() > 0)
                {
                    MONTO_LIMITE = (double)dt2[0].montodescto;
                    MON_MONTO_LIMITE = dt2[0].moneda;
                }
                else
                {
                    // si no hay registro en cocobranza_deposito algo esta mal y mejor no aprobar la
                    // proforma
                    return (false, "");
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // 3ª totalizar los desctos por depositos que se otorgaron con esta CBZA-DEPOSITO
                // y que ya estan facturados
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var dt3 = await _context.vedesextraprof
                    .Join(_context.veproforma,
                          p1 => p1.codproforma,
                          p2 => p2.codigo,
                          (p1, p2) => new { p1, p2 })
                    .Join(_context.veremision,
                          combined => combined.p2.codigo,
                          p3 => p3.codproforma,
                          (combined, p3) => new { combined.p1, combined.p2, p3 })
                    .Where(x => x.p1.codanticipo == reg.codanticipo && x.p2.anulada == false && x.p3.anulada == false)
                    .Select(x => new
                    {
                        codproforma = x.p2.codigo,
                        idpf = x.p2.id,
                        nroidpf = x.p2.numeroid,
                        fecha_pf = x.p2.fecha,
                        codcliente = x.p2.codcliente,
                        id = x.p3.id,
                        numeroid = x.p3.numeroid,
                        fecha = x.p3.fecha,
                        montodoc = x.p1.montodoc,
                        codmoneda = x.p2.codmoneda
                    })
                    .ToListAsync();

                double MONTO_TTL_APLICADO = 0;
                foreach (var reg1 in dt3)
                {
                    double MONTO_aux_LIMITE = 0;
                    // conversion de moneda
                    if (reg1.codmoneda != MON_MONTO_LIMITE)
                    {
                        MONTO_aux_LIMITE = (double)await tipocambio._conversion(_context, MON_MONTO_LIMITE, reg1.codmoneda, reg1.fecha_pf, reg1.montodoc);
                    }
                    else
                    {
                        MONTO_aux_LIMITE = (double)reg1.montodoc;
                    }
                    MONTO_TTL_APLICADO += MONTO_aux_LIMITE;
                }
                // EL TOTAL EFECTIVAMENTE APLICADO Y FACTURADO MAS LO QUE SE QUIERE APROBAR
                MONTO_TTL_APLICADO += MONTO_APLICADO_ESTA_PROF;
                MONTO_TTL_APLICADO = Math.Round(MONTO_TTL_APLICADO, 2);

                // CONTROLAR SI EL TOTAL FACTUTRADO MAS LO QUE FACTURARA SI ESTO SE APRUEBA
                if (MONTO_TTL_APLICADO > MONTO_LIMITE)
                {
                    msgAlert = "El monto total de descuentos por deposito aplicados del Deposito: " + dt2[0].id + "-" + dt2[0].numeroid + " es mayor al disponible. Se aplico en esta proforma y otras un total de: " + MONTO_TTL_APLICADO + "(" + MON_MONTO_LIMITE + ") y el maximo aplicable es: " + MONTO_LIMITE + " verifique los descuentos por deposito aplicados del mencionado deposito.";
                    resultado = false;
                }
                else
                {
                    resultado = true;
                }
            }

            return (resultado, msgAlert);
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
