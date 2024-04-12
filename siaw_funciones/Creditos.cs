using Microsoft.CodeAnalysis.RulesetToEditorconfig;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Creditos
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
        private Funciones funciones = new Funciones();
        private Cliente cliente = new Cliente();
        private Empresa empresa = new Empresa();
        private TipoCambio tipocambio = new TipoCambio();
        private Seguridad seguridad = new Seguridad();
        private Configuracion configuracion = new Configuracion();
        public async Task<bool> Cliente_Tiene_Linea_De_Credito_Valida(DBContext _context, string codcliente)
        {
            try
            {
                bool resultado = false;
                DateTime fecha_hoy = await funciones.FechaDelServidor(_context);
                DateTime fechaAux = new DateTime(fecha_hoy.Year, fecha_hoy.Month, fecha_hoy.Day);

                string cliente_principal = "";
                string codigoPrincipal = "";
                string CodigosIguales = "";
                
                 codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);

                if (await cliente.NIT(_context, codigoPrincipal) ==  await cliente.NIT(_context, codcliente))
                {
                    cliente_principal = codigoPrincipal;
                    CodigosIguales = await cliente.CodigosIgualesMismoNIT(_context, codcliente);
                }
                else
                {
                    cliente_principal = codcliente;
                    CodigosIguales = "'" + codcliente + "'";
                }

                //using (_context)
                ////using (var _context = DbContextFactory.Create(_context))
                //{
                    var creditoCliente = (from credito in _context.vehcredito
                                          join cliente in _context.vecliente on credito.codcliente equals cliente.codigo
                                          where CodigosIguales.Contains(credito.codcliente) &&
                                                credito.codtipocredito == "FIJO" &&
                                                credito.credito > 0 &&
                                                !(credito.revertido??false) &&
                                                (fechaAux <= credito.fechavenc && fechaAux <= credito.fecha_vence_garantia)
                                          select credito).FirstOrDefault();

                    if (creditoCliente != null)
                    {
                        if (fechaAux > creditoCliente.fechavenc || fechaAux > creditoCliente.fecha_vence_garantia)
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        return false;
                    }
               //}
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<(bool resultado_func, object data)> ValidarCreditoDisponible_en_Bs(DBContext _context, bool mostrar_detalle, string codcliente, bool incluir_proformas, double totaldoc, string codempresa, string usuario, string monedae, string moneda_pf)
        {
            string codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);
            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
            string CodigosIguales = "";

            if (await cliente.NIT(_context,codigoPrincipal) == await cliente.NIT(_context,codcliente))
            {
                codcliente = codigoPrincipal;
                CodigosIguales = await cliente.CodigosIgualesMismoNIT(_context, codcliente);
            }
            else
            {
                CodigosIguales = "'" + codcliente + "'";
            }
            //actualizar el credito (falta implementar)
            //await Actualizar_Credito_2023(codcliente, usuario, codempresa, false);

            //sacar su credito disponible
            decimal cred_actual = await _context.vecliente.Where(i=> i.codigo == codcliente).Select(i=> i.credito).FirstOrDefaultAsync()??0;


            //saacr moneda actual
            string moneda_actual = "";
            string moneda_base = await empresa.monedabase(_context, codempresa);
            try
            {
                moneda_actual = await Credito_Fijo_Asignado_Vigente_Moneda(_context, codcliente);
            }
            catch (Exception)
            {
                moneda_actual = await empresa.monedabase(_context, codempresa);
            }
            //sacar cuanto debe localmente
            decimal deuda_actual_bs = 0;
            decimal deuda_actual_us = 0;

            // Desde 17-04-2023 la moneda debe obtenerse segun el credito del cliente
            try
            {
                var codigos = await _context.veremision
                    .Join(_context.coplancuotas,
                          p1 => p1.codigo,
                          p2 => p2.coddocumento,
                          (p1, p2) => new { Veremision = p1, Coplancuotas = p2 })
                    .Where(joinResult =>
                           CodigosIguales.Contains(joinResult.Veremision.codcliente)
                           && joinResult.Veremision.tipopago == 1
                           && joinResult.Veremision.contra_entrega == false
                           && joinResult.Veremision.anulada == false)
                    .Select(joinResult => joinResult.Veremision.codigo)
                    .ToListAsync();

                deuda_actual_bs = await _context.coplancuotas
                    .Where(c => CodigosIguales.Contains(c.cliente) 
                    && codigos.Contains(c.coddocumento)
                    && c.monto > c.montopagado
                    && c.moneda == moneda_base
                    )
                    .SumAsync(c => c.monto - c.montopagado) ?? 0;
            }
            catch (Exception)
            {
                deuda_actual_bs = 0;
            }


            try
            {
                var codigos = await _context.veremision
                    .Join(_context.coplancuotas,
                          p1 => p1.codigo,
                          p2 => p2.coddocumento,
                          (p1, p2) => new { Veremision = p1, Coplancuotas = p2 })
                    .Where(joinResult =>
                           CodigosIguales.Contains(joinResult.Veremision.codcliente)
                           && joinResult.Veremision.tipopago == 1
                           && joinResult.Veremision.contra_entrega == false
                           && joinResult.Veremision.anulada == false)
                    .Select(joinResult => joinResult.Veremision.codigo)
                    .ToListAsync();

                deuda_actual_us = await _context.coplancuotas
                    .Where(c => CodigosIguales.Contains(c.cliente)
                    && codigos.Contains(c.coddocumento)
                    && c.monto > c.montopagado
                    && c.moneda == monedae
                    )
                    .SumAsync(c => c.monto - c.montopagado) ?? 0;
            }
            catch (Exception)
            {
                deuda_actual_us = 0;
            }

            //busca el SALDO NACIONAL si tiene sucursales en otras agencia
            //implementado el 09-05-2020

            double saldo_x_pagar_demas_ags_us = 0;

            var resul1 = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, codcliente, monedae);
            if (resul1.message != "")
            {
                return (false, new {resp = resul1.message});  // devolver error de mensaje REVISAR
            }
            saldo_x_pagar_demas_ags_us = resul1.resp;

            double saldo_x_pagar_demas_ags_bs = 0;
            var resul2 = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, codcliente, moneda_base);
            if (resul2.message != "")
            {
                return (false, new { resp = resul1.message });   // devolver error de mensaje REVISAR
            }
            saldo_x_pagar_demas_ags_bs = resul2.resp;


            //busca el SALDO NACIONAL si tiene sucursales en otras agencia
            //implementado el 09-05-2020
            double ttl_proformas_aprobadas_demas_ags_us = 0;
            var resul3 = await cliente.Cliente_Proformas_Aprobadas_Nacional(_context, codcliente, monedae);
            if (resul3.message != "")
            {
                return (false, new { resp = resul3.message });  // devolver error de mensaje REVISAR
            }
            ttl_proformas_aprobadas_demas_ags_us = resul3.resp;

            double ttl_proformas_aprobadas_demas_ags_bs = 0;
            var resul4 = await cliente.Cliente_Proformas_Aprobadas_Nacional(_context, codcliente, moneda_base);
            if (resul4.message != "")
            {
                return (false, new { resp = resul4.message });  // devolver error de mensaje REVISAR
            }
            ttl_proformas_aprobadas_demas_ags_bs = resul4.resp;

            //si es necesario sacar de proformas aprobadas

            double monto_prof_aprobadas_us = 0;
            if (incluir_proformas)
            {
                try
                {
                    monto_prof_aprobadas_us = (double)await _context.veproforma
                        .Where(i => i.tipopago == 1 &&
                        CodigosIguales.Contains(i.codcliente) &&
                        i.aprobada == true &&
                        i.transferida == false &&
                        i.anulada == false &&
                        i.contra_entrega == false &&
                        i.codmoneda == monedae
                        ).SumAsync(i => i.total);
                }
                catch (Exception)
                {
                    monto_prof_aprobadas_us = 0;
                }
            }
            double monto_prof_aprobadas_bs = 0;
            if (incluir_proformas)
            {
                try
                {
                    monto_prof_aprobadas_bs = (double)await _context.veproforma
                        .Where(i => i.tipopago == 1 &&
                        CodigosIguales.Contains(i.codcliente) &&
                        i.aprobada == true &&
                        i.transferida == false &&
                        i.anulada == false &&
                        i.contra_entrega == false &&
                        i.codmoneda == moneda_base
                        ).SumAsync(i => i.total);
                }
                catch (Exception)
                {
                    monto_prof_aprobadas_bs = 0;
                }
            }

            //sacar anticipos sin distribuir(solo los que NOOO SON PARA VENTA CONTADO)

            double anticipos_bs = 0;
            try
            {
                anticipos_bs = (double)await _context.coanticipo
                    .Where(i => i.anulado == false &&
                        CodigosIguales.Contains(i.codcliente) &&
                        i.codmoneda == moneda_base)
                    .SumAsync(i => i.montorest);
            }
            catch (Exception)
            {
                anticipos_bs = 0;
            }

            double anticipos_us = 0;
            try
            {
                anticipos_us = (double)await _context.coanticipo
                    .Where(i => i.anulado == false &&
                        CodigosIguales.Contains(i.codcliente) &&
                        i.codmoneda == monedae)
                    .SumAsync(i => i.montorest);
            }
            catch (Exception)
            {
                anticipos_us = 0;
            }
            double resultado = 0;
            if (moneda_pf == moneda_base)
            {
                //si la moneda de la proforma es en moneda base osea BS se debe convertir todo lo que es a US a BS
                //si el credito es en US convertir a BS
                if (moneda_actual != moneda_base)
                {
                    cred_actual = await tipocambio._conversion(_context, moneda_base, monedae, DateTime.Now, cred_actual);
                }
                resultado = (double)cred_actual + anticipos_bs + (double)await tipocambio._conversion(_context,moneda_base,monedae, DateTime.Now, (decimal)anticipos_us) - ((double)deuda_actual_bs + (double)await tipocambio._conversion(_context,moneda_base,monedae, DateTime.Now, deuda_actual_us) + monto_prof_aprobadas_bs + (double)await tipocambio._conversion(_context,moneda_base, monedae, DateTime.Now, (decimal)monto_prof_aprobadas_us) + ttl_proformas_aprobadas_demas_ags_bs + (double)await tipocambio._conversion(_context,moneda_base,monedae, DateTime.Now, (decimal)ttl_proformas_aprobadas_demas_ags_us) + saldo_x_pagar_demas_ags_bs + (double)await tipocambio._conversion(_context,moneda_base,monedae,DateTime.Now, (decimal)saldo_x_pagar_demas_ags_us)) ;
                resultado = Math.Round(resultado, 2);
            }
            else
            {
                //si la moneda de la proforma NO es en moneda base osea US se debe convertir todo lo que es a BS a US
                //si el credito es en US convertir a BS
                if (moneda_actual == moneda_base)
                {
                    cred_actual = await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, cred_actual);
                }

                resultado = (double)cred_actual + anticipos_us + (double)await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, (decimal)anticipos_bs) - ((double)deuda_actual_us + (double)await tipocambio._conversion(_context,  monedae, moneda_base, DateTime.Now, deuda_actual_bs) + monto_prof_aprobadas_us + (double)await tipocambio._conversion(_context,  monedae, moneda_base, DateTime.Now, (decimal)monto_prof_aprobadas_bs) + ttl_proformas_aprobadas_demas_ags_us + (double)await tipocambio._conversion(_context,  monedae, moneda_base, DateTime.Now, (decimal)ttl_proformas_aprobadas_demas_ags_bs) + saldo_x_pagar_demas_ags_us + (double)await tipocambio._conversion(_context,  monedae, moneda_base, DateTime.Now, (decimal)saldo_x_pagar_demas_ags_bs));
                resultado = Math.Round(resultado, 2);
            }

            double monto_credito_disponible = resultado;
            bool resultado_func = false;
            // si el total del doc es mayor a 0, significa que son ventas que requieren credito disponible
            //y por lo tanto se debe validar el credito disponible
            //añadido por aldrin en fecha: 29-04-2015
            if (totaldoc > 0)
            {
                if (totaldoc > resultado)
                {
                    //no alcanza el credito y muestro el detalle si es que hay que mostrar
                    resultado_func = false;
                }
                else
                {
                    resultado_func = true;
                }
            }
            else
            {
                resultado_func = true;
            }


            double deuda_actual_total = 0;
            double saldo_x_pagar_demas_ags_total = 0;
            double monto_prof_aprobadas_total = 0;
            double ttl_proformas_aprobadas_demas_ags_total = 0;
            double ttl_anticipos = 0;

            if (moneda_pf == moneda_base)
            {
                // si la moneda de la proforma es en moneda base osea BS se debe convertir todo lo que es a US a BS
                deuda_actual_total = (double)(deuda_actual_bs + await tipocambio._conversion(_context,moneda_base, monedae, DateTime.Now, deuda_actual_us));
                saldo_x_pagar_demas_ags_total = saldo_x_pagar_demas_ags_bs + (double)await tipocambio._conversion(_context, moneda_base, monedae, DateTime.Now, (decimal)saldo_x_pagar_demas_ags_us);
                monto_prof_aprobadas_total = monto_prof_aprobadas_bs + (double)await tipocambio._conversion(_context, moneda_base, monedae, DateTime.Now, (decimal)monto_prof_aprobadas_us);
                ttl_proformas_aprobadas_demas_ags_total = ttl_proformas_aprobadas_demas_ags_bs + (double)await tipocambio._conversion(_context, moneda_base, monedae, DateTime.Now, (decimal)ttl_proformas_aprobadas_demas_ags_us);
                ttl_anticipos = anticipos_bs + (double) await tipocambio._conversion(_context, moneda_base, monedae, DateTime.Now, (decimal)anticipos_us);
            }
            else
            {
                // si la moneda de la proforma NO es en moneda base osea US se debe convertir todo lo que es a BS a US
                deuda_actual_total = (double)(await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, deuda_actual_bs) + deuda_actual_us);
                saldo_x_pagar_demas_ags_total = (double)await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, (decimal)saldo_x_pagar_demas_ags_bs) + saldo_x_pagar_demas_ags_us;
                monto_prof_aprobadas_total = (double)await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, (decimal)monto_prof_aprobadas_bs) + monto_prof_aprobadas_us;
                ttl_proformas_aprobadas_demas_ags_total = (double)await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, (decimal)ttl_proformas_aprobadas_demas_ags_bs) + ttl_proformas_aprobadas_demas_ags_us;
                ttl_anticipos = (double)await tipocambio._conversion(_context, monedae, moneda_base, DateTime.Now, (decimal)anticipos_bs) + anticipos_us;
            }

            if (mostrar_detalle)
            {
                var detalle = new
                {
                    titulo = "El Credito del Cliente o Agrupacion Cial. En " + moneda_pf == "BS" ? "BS" : "US",
                    subtitulo = resultado_func == true ? "SI, alcanzara " : "NO, alcanzara ",
                    limite = new {
                        text = "(+)  Limite de Credito: ",
                        sld = cred_actual
                    },
                    anticipo = new {
                        text = "(+)  Anticipos Sin Dist.: ",
                        sld = ttl_anticipos
                    }, 
                    deuLoc = new {
                        text = "(-)  Deuda Local: ",
                        sld = deuda_actual_total
                    },
                    deuAgs = new {
                        text = "(-)  Deuda Otras Ags: ",
                        sld = saldo_x_pagar_demas_ags_total
                    },
                    profApro = new {
                        text = "(-)  Proformas Aprobadas: ",
                        sld = monto_prof_aprobadas_total
                    },
                    profApAgs = new {
                        text = "(-)  Prof. Ap. Otras Ags: ",
                        sld = ttl_proformas_aprobadas_demas_ags_total
                    },
                    profAct = new {
                        text = "(-)  Proforma Actual: ",
                        sld = totaldoc
                    },
                    saldo = new {
                        text = "  Saldo Credito: ",
                        sld = (resultado - totaldoc)
                    }
                };
                return (resultado_func, detalle);
            }

            return (resultado_func, null);

        }



        public async Task<bool> Actualizar_Credito_2023(string codcliente, string usuario, string codempresa, bool arreglar)
        {
            if (arreglar)
            {
                //arreglar datos posiblemente arroneos

            }
            return true;
        }

        public async Task<string> Credito_Fijo_Asignado_Vigente_Moneda(DBContext _context, string codcliente)
        {
            var resultado = await _context.vehcredito
                .Where(v => _context.vetipocredito
                    .Any(t => t.codigo == v.codtipocredito)
                    && v.revertido == false
                    && v.codcliente == codcliente)
                .OrderByDescending(v => v.fecha)
                .ThenByDescending(v => v.horareg)
                .FirstOrDefaultAsync();
            if (resultado!= null)
            {
                return resultado.moneda;
            }
            return "BS";
        }


        ///////////////////////////////////////////////////////////////////////////////
        //Esta funcion devuelve elcredito disponible del cliente expresado en la moneda indicada tdc del dia
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<double> credito(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(c => c.codigo == codcliente).Select(c => c.credito).FirstOrDefaultAsync() ?? 0;
            return (double)resultado;
        }

        public async Task<double> Obtener_Credito_Casa_Matriz(DBContext _context, string cliente_principal_local, string codmoneda)
        {
            double resultado = 0;
            string _codigo_Principal = await cliente.CodigoPrincipal(_context, cliente_principal_local);
            if (await cliente.Cliente_Tiene_Sucursal_Nacional(_context, cliente_principal_local))
            {
                //si el cliente es parte de una agrupacion cial a nivel nacional entre agencias de Pertec a nivel nacional
                string casa_matriz_Nacional = await cliente.CodigoPrincipal_Nacional(_context, cliente_principal_local);
                int CODALM = 0;
                if (casa_matriz_Nacional.Trim().Length > 0)
                {
                    CODALM = await cliente.Almacen_Casa_Matriz_Nacional(_context,casa_matriz_Nacional);
                }
                var dt_sucursales = await _context.veclientesiguales_nacion
                    .Where(i => i.codcliente_a == casa_matriz_Nacional).ToListAsync();

                foreach (var reg in dt_sucursales)
                {
                    if (casa_matriz_Nacional == reg.codcliente_b)
                    {
                        // buscar los datos de conexion de la sucursal
                        var dt_conexion = await _context.ad_conexion_vpn
                            .Where(c => c.agencia.StartsWith("credito") && c.codalmacen == reg.codalmacen_b)
                            .FirstOrDefaultAsync();
                        if (dt_conexion == null)
                        {
                            return 0;
                        }
                        else
                        {
                            //OBTENER CADENA DE CONEXION
                            var newCadConexVPN = seguridad.Getad_conexion_vpnFromDatabase(dt_conexion.contrasena_sql, dt_conexion.servidor_sql, dt_conexion.usuario_sql, dt_conexion.bd_sql);
                            //alistar la cadena de conexion para conectar a la ag
                            using (var _contextVPN = DbContextFactory.Create(newCadConexVPN))
                            {
                                _codigo_Principal = await cliente.CodigoPrincipal(_contextVPN, reg.codcliente_b);
                                string cliente_principal = reg.codcliente_b;
                                string _CodigosIguales = "'" + reg.codcliente_b + "'";
                                // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                                if (await cliente.NIT(_contextVPN,_codigo_Principal)== await cliente.NIT(_contextVPN,reg.codcliente_b))  
                                {
                                    cliente_principal = _codigo_Principal;
                                    _CodigosIguales = await cliente.CodigosIgualesMismoNIT(_contextVPN, reg.codcliente_b);  //<------solo los de mismo NIT
                                }
                                // obtener el saldo pendiente de pago de todo el grupo cial
                                resultado = (double)(await _contextVPN.vecliente.Where(i => _CodigosIguales.Contains(i.codigo)).SumAsync(i => i.credito) ?? 0);
                            }
                        }
                    }
                }

            }
            return resultado;
        }

        public async Task<string> CodigoPrincipalCreditos(DBContext _context, string codcliente)
        {
            try
            {
                string codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);
                if (await cliente.NIT(_context, codigoPrincipal) != await cliente.NIT(_context, codcliente))
                {
                    codigoPrincipal = codcliente;
                }
                return codigoPrincipal;
            }
            catch (Exception)
            {
                return codcliente;
            }
        }

        public async Task<string[]> Credito_Otorgado_Vigente(DBContext _context, string codcliente)
        {
            string[] resultado = new string[3];

            var dt = await _context.vehcredito
                .Where(v => v.codtipocredito == "FIJO"
                    && v.codcliente == codcliente
                    && v.revertido == false)
                .FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.fechavenc < DateTime.Now)
                {
                    resultado[0] = "0";
                    resultado[1] = dt.fecha.ToShortDateString();
                    resultado[2] = dt.fechavenc.ToShortDateString();
                }
                else
                {
                    resultado[0] = dt.credito.ToString() + " (" + dt.moneda + ")";
                    resultado[1] = dt.fecha.ToShortDateString();
                    resultado[2] = dt.fechavenc.ToShortDateString();
                }
            }
            else
            {
                resultado[0] = "0";
                resultado[1] = "";
                resultado[2] = "";
            }
            return resultado;
        }


        public async Task<bool> ClienteEnMora(DBContext _context, string codcliente, string codempresa)
        {
            bool resultado = false;
            int dias_limite_mora = await configuracion.dias_mora_limite(_context, codempresa);
            double monto_minimo_mora = 0;
            int nrocuentas_en_mora = 0;
            string CodigosIguales = "";
            string codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);
            CodigosIguales = await cliente.CodigosIguales(_context, codigoPrincipal);

            try
            {
                //var notasPendientesPago = await _context.coplancuotas
                //    .Where(c => CodigosIguales.Contains(c.cliente) && c.montopagado < c.monto)
                //    .Join(_context.veremision, c => c.coddocumento, r => r.codigo, (c, r) => new { c, r })
                //    .Join(_context.vecliente, x => x.c.cliente, p1 => p1.codigo, (x, p1) => new { x, p1 })
                //    .GroupBy(grp => new { grp.x.r.codigo, grp.x.r.codalmacen, grp.x.r.codcliente, grp.x.r.id, grp.x.r.numeroid, grp.x.r.codmoneda, grp.x.r.total, grp.x.r.fecha })
                //    .Select(grp => new
                //    {
                //        grp.Key.codigo,
                //        grp.Key.codalmacen,
                //        grp.Key.codcliente,
                //        grp.Key.id,
                //        grp.Key.numeroid,
                //        grp.Key.codmoneda,
                //        grp.Key.total,
                //        grp.Key.fecha,
                //        limite = grp.Max(x => x.x.c.vencimiento),
                //        por_pagar = grp.Sum(x => x.x.c.monto - x.x.c.montopagado),
                //        mora = (int)(DateTime.Now.Date - grp.Max(x => x.x.c.vencimiento.Date)).TotalDays
                //        //mora = _context.coplancuotas.Any(c => c.coddocumento == grp.Key.codigo) ?
                //        //(int)_context.coplancuotas.Max(c => DbFunctions.DiffDays(c.vencimiento, DateTime.Now)) : 0

                //    }).ToListAsync();
                var notasPendientesPago = await _context.coplancuotas
                     .Where(c => CodigosIguales.Contains(c.cliente) && c.montopagado < c.monto)
                     .Join(_context.veremision, c => c.coddocumento, r => r.codigo, (c, r) => new { c, r })
                     .Join(_context.vecliente, x => x.c.cliente, p1 => p1.codigo, (x, p1) => new { x, p1 })
                     .GroupBy(grp => new { grp.x.r.codigo, grp.x.r.codalmacen, grp.x.r.codcliente, grp.x.r.id, grp.x.r.numeroid, grp.x.r.codmoneda, grp.x.r.total, grp.x.r.fecha })
                     .Select(grp => new
                     {
                         grp.Key.codigo,
                         grp.Key.codalmacen,
                         grp.Key.codcliente,
                         grp.Key.id,
                         grp.Key.numeroid,
                         grp.Key.codmoneda,
                         grp.Key.total,
                         grp.Key.fecha,
                         limite = grp.Max(x => x.x.c.vencimiento),
                         por_pagar = grp.Sum(x => x.x.c.monto - x.x.c.montopagado),
                         //mora = (int)_context.coplancuotas.Max(c => EF.Functions.DateDiffDay(c.vencimiento, DateTime.Now))
                         //mora = grp.Max(x => (DateTime.Now - x.x.c.vencimiento).TotalDays)
                         mora = grp.Max(x => (EF.Functions.DateDiffDay(x.x.c.vencimiento, DateTime.Now)))
                     }).ToListAsync();

                foreach (var nota in notasPendientesPago)
                {
                    int dias_extension = await _context.veppextension
                        .Where(x => x.id == nota.id && x.numeroid == nota.numeroid)
                        .SumAsync(x => (int?)x.dias) ?? 0;

                    var mora_con_extension = nota.mora - dias_extension;

                    if (mora_con_extension > dias_limite_mora)
                    {
                        monto_minimo_mora = (double)await configuracion.monto_maximo_mora_clientes(_context, codempresa, nota.codmoneda, nota.codalmacen);

                        if (nota.por_pagar > (decimal)monto_minimo_mora)
                        {
                            nrocuentas_en_mora++;
                        }
                    }
                }

                if (nrocuentas_en_mora > 0)
                {
                    resultado = true;
                }
            }
            catch (Exception ex)
            {
                resultado = false;
            }

            return resultado;

        }

        public async Task<string> Cadena_Notas_De_Remision_En_Mora(DBContext _context, string codcliente, string codempresa)
        {
            string resultado = "";
            string Cadena1 = "";
            string Cadena2 = "";
            int dias_limite_mora = await configuracion.dias_mora_limite(_context, codempresa);
            decimal monto_minimo_mora = 0;
            int nrocuentas_en_mora = 0;
            string CodigosIguales = "";
            string codigoPrincipal = "";

            codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);
            CodigosIguales = await cliente.CodigosIguales(_context, codigoPrincipal);

            try
            {
                //var notasConPendientesPago = (from c in _context.coplancuotas
                //                              join r in _context.veremision on c.coddocumento equals r.codigo
                //                              join p1 in _context.vecliente on c.cliente equals p1.codigo
                //                              where CodigosIguales.Contains(c.cliente) && c.montopagado < c.monto
                //                              group new { c, r } by new { r.codigo, r.codalmacen, r.codcliente, r.id, r.numeroid, r.codmoneda, r.total, r.fecha, c.nrocuota } into grp
                //                              select new
                //                              {
                //                                  grp.Key.codigo,
                //                                  grp.Key.codalmacen,
                //                                  grp.Key.codcliente,
                //                                  grp.Key.id,
                //                                  grp.Key.numeroid,
                //                                  grp.Key.codmoneda,
                //                                  grp.Key.total,
                //                                  grp.Key.fecha,
                //                                  grp.Key.nrocuota,
                //                                  limite = grp.Max(x => x.c.vencimiento),
                //                                  por_pagar = grp.Sum(x => x.c.monto - x.c.montopagado),
                //                                  mora = (DateTime.Now - grp.Max(x => x.c.vencimiento)).TotalDays
                //                              }).ToList();
                var notasConPendientesPago = await _context.coplancuotas
                    .Join(_context.veremision, c => c.coddocumento, r => r.codigo, (c, r) => new { c, r })
                    .Join(_context.vecliente, x => x.c.cliente, p1 => p1.codigo, (x, p1) => new { x, p1 })
                    .Where(grp => CodigosIguales.Contains(grp.x.c.cliente) && grp.x.c.montopagado < grp.x.c.monto)
                    .GroupBy(grp => new { grp.x.r.codigo, grp.x.r.codalmacen, grp.x.r.codcliente, grp.x.r.id, grp.x.r.numeroid, grp.x.r.codmoneda, grp.x.r.total, grp.x.r.fecha, grp.x.c.nrocuota })
                    .Select(grp => new
                    {
                        grp.Key.codigo,
                        grp.Key.codalmacen,
                        grp.Key.codcliente,
                        grp.Key.id,
                        grp.Key.numeroid,
                        grp.Key.codmoneda,
                        grp.Key.total,
                        grp.Key.fecha,
                        grp.Key.nrocuota,
                        limite = grp.Max(x => x.x.c.vencimiento),
                        por_pagar = grp.Sum(x => x.x.c.monto - x.x.c.montopagado),
                        //mora = grp.Max(x => (DateTime.Now - x.x.c.vencimiento).TotalDays)
                        mora = grp.Max(x => (EF.Functions.DateDiffDay(x.x.c.vencimiento, DateTime.Now)))
                    })
                    .ToListAsync();

                foreach (var nota in notasConPendientesPago)
                {
                    int dias_extension = await _context.veppextension
                        .Where(x => x.id == nota.id && x.numeroid == nota.numeroid)
                        .SumAsync(x => (int?)x.dias) ?? 0;

                    var mora_con_extension = nota.mora - dias_extension;

                    if (mora_con_extension > dias_limite_mora)
                    {
                        monto_minimo_mora = await configuracion.monto_maximo_mora_clientes(_context, codempresa, nota.codmoneda, nota.codalmacen);

                        if (nota.por_pagar > monto_minimo_mora)
                        {
                            nrocuentas_en_mora++;
                            Cadena2 += funciones.Rellenar(nota.codcliente, 12, " ", false);
                            Cadena2 += "  " + funciones.Rellenar(nota.id + "-" + nota.numeroid, 20, " ", false);
                            Cadena2 += "  " + funciones.Rellenar(nota.nrocuota.ToString(), 5, " ", false);
                            Cadena2 += "  " + funciones.Rellenar(monto_minimo_mora.ToString() + " " + nota.codmoneda, 12, " ", false);
                            Cadena2 += "  " + funciones.Rellenar(nota.por_pagar.ToString(), 12, " ", false) + "\n";
                        }
                    }
                }

                if (nrocuentas_en_mora > 0)
                {
                    Cadena1 = "---------------------------------------------------------------------------\n";
                    Cadena1 += funciones.Rellenar("Cliente      Remision           Cuota   MontoMaxMora       PorPagar", 96, " ", false) + "\n";
                    Cadena1 += "--------------------------------------------------------------------------\n";
                    resultado += Cadena1 + Cadena2;
                }
            }
            catch (Exception ex)
            {
                resultado = "";
            }
            return resultado;
        }

        public async Task<int> NroDeTiendas(DBContext _context, string codcliente)
        {
            int resultado = 0;
            try
            {
                resultado = await _context.vetienda.CountAsync(v => v.codcliente == codcliente);
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }



    }
}
