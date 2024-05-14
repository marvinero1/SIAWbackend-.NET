using Microsoft.CodeAnalysis.RulesetToEditorconfig;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
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
        private datosProforma datosProf = new datosProforma();
        private Log log = new Log();
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

        public async Task<(bool resultado_func, object? data, string msgAlertActualiza)> ValidarCreditoDisponible_en_Bs(DBContext _context, bool mostrar_detalle, string codcliente, bool incluir_proformas, double totaldoc, string codempresa, string usuario, string monedae, string moneda_pf)
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
            string msgAlertActualiza = "";
            
            var resultAct = await Actualizar_Credito_2023(_context,codcliente, usuario, codempresa, false);
            if (resultAct.value == false)
            {
                msgAlertActualiza = resultAct.msg;
            }
            

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
                return (false, new {resp = resul1.message}, msgAlertActualiza);  // devolver error de mensaje REVISAR
            }
            saldo_x_pagar_demas_ags_us = resul1.resp;

            double saldo_x_pagar_demas_ags_bs = 0;
            var resul2 = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, codcliente, moneda_base);
            if (resul2.message != "")
            {
                return (false, new { resp = resul1.message }, msgAlertActualiza);   // devolver error de mensaje REVISAR
            }
            saldo_x_pagar_demas_ags_bs = resul2.resp;


            //busca el SALDO NACIONAL si tiene sucursales en otras agencia
            //implementado el 09-05-2020
            double ttl_proformas_aprobadas_demas_ags_us = 0;
            var resul3 = await cliente.Cliente_Proformas_Aprobadas_Nacional(_context, codcliente, monedae);
            if (resul3.message != "")
            {
                return (false, new { resp = resul3.message }, msgAlertActualiza);  // devolver error de mensaje REVISAR
            }
            ttl_proformas_aprobadas_demas_ags_us = resul3.resp;

            double ttl_proformas_aprobadas_demas_ags_bs = 0;
            var resul4 = await cliente.Cliente_Proformas_Aprobadas_Nacional(_context, codcliente, moneda_base);
            if (resul4.message != "")
            {
                return (false, new { resp = resul4.message }, msgAlertActualiza);  // devolver error de mensaje REVISAR
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
                string monRespuesta = moneda_pf == "BS" ? "BS" : "US";

                var detalle = new
                {
                    titulo = "El Credito del Cliente o Agrupacion Cial. En " + monRespuesta,
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
                return (resultado_func, detalle, msgAlertActualiza);
            }

            return (resultado_func, null, msgAlertActualiza);

        }



        public async Task<(bool value,string msg)> Actualizar_Credito_2023(DBContext _context, string codcliente, string usuario, string codempresa, bool arreglar)
        {
            if (arreglar)
            {
                //arreglar datos posiblemente arroneos
                var query = await _context.coplancuotas
                    .Where(cuota => cuota.montopagado < cuota.monto &&
                    cuota.codtipodoc == 4 &&
                    !_context.veremision.Any(v => v.codigo == cuota.coddocumento)).ToListAsync();

                _context.coplancuotas.RemoveRange(query);
                await _context.SaveChangesAsync();

                var cuotasToUpdate = _context.coplancuotas
                    .Where(cuota => cuota.montopagado > cuota.monto);

                foreach (var cuota in cuotasToUpdate)
                {
                    cuota.montopagado = cuota.monto;
                }

                await _context.SaveChangesAsync();
            }

            string codigoPrincipal_local = await cliente.CodigoPrincipal(_context, codcliente);
            string cliente_principal_local = "";
            string CodigosIguales_local = "";

            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
            if (await cliente.NIT(_context, codigoPrincipal_local) == await cliente.NIT(_context, codcliente))
            {
                cliente_principal_local = codigoPrincipal_local;
                CodigosIguales_local = await cliente.CodigosIgualesMismoNIT(_context, codcliente);  // <------solo los de mismo NIT
            }
            else
            {
                cliente_principal_local = codcliente;
                CodigosIguales_local = "'" + codcliente + "'";
            }

            // obtener el credito de el codigo principal
            bool actualizar_sucursales_nal = false;
            double credito_principal = 0;

            // solo si tiene sucursar nacional obtener el credito de la casa matriz
            if (await cliente.Cliente_Tiene_Sucursal_Nacional(_context, cliente_principal_local))
            {
                string casa_matriz_Nacional = await cliente.CodigoPrincipal_Nacional(_context, cliente_principal_local);
                int ag_matriz_nacional = await cliente.Almacen_Casa_Matriz_Nacional(_context, casa_matriz_Nacional);
                if (ag_matriz_nacional == await cliente.almacen_de_cliente(_context,cliente_principal_local))
                {
                    // busca en el credito en la conexion local
                    credito_principal = await credito(_context,cliente_principal_local);
                    // credito_principal = sia_funciones.Creditos.Instancia.Credito_Fijo_Asignado_Vigente(cliente_principal_local)
                }
                else
                {
                    // buscara el credito en la agencia donde esta la casa martriz
                    credito_principal = await Obtener_Credito_Casa_Matriz(_context, codigoPrincipal_local, "US");
                    // credito_principal = sia_funciones.Creditos.Instancia.Obtener_Credito_Casa_Matriz_2023(codigoPrincipal_local, "US")
                }
                actualizar_sucursales_nal = true;
            }
            else
            {
                // busca en el credito en la conexion local
                credito_principal = await credito(_context, cliente_principal_local);
                // credito_principal = sia_funciones.Creditos.Instancia.Credito_Fijo_Asignado_Vigente(cliente_principal_local)
                actualizar_sucursales_nal = false;
            }

            // poner el credito del principal a todos
            try
            {
                var clientesToUpdate = await _context.vecliente
                .Where(cliente => CodigosIguales_local.Contains(cliente.codigo)).ToListAsync();

                foreach (var cliente in clientesToUpdate)
                {
                    cliente.credito = (decimal?)credito_principal;
                    cliente.creditodisp = (decimal?)0.0;
                }

                _context.SaveChanges();

            }
            catch (Exception)
            {
                return (false, "Ocurrio un error al actualizar el credito disponible de los clientes: " + CodigosIguales_local + "Alerta!!!");
            }

            // Ir descontando deudas
            string monedacliente = await cliente.monedacliente(_context, cliente_principal_local, usuario, codempresa);
            string monedaext = await empresa.monedaext(_context, codempresa);
            string monedabase = await empresa.monedabase(_context, codempresa);
            string moneda_credito = await Credito_Fijo_Asignado_Vigente_Moneda(_context, cliente_principal_local);

            // actualizar el credito disponible localmente

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7
            // BUSCA EL SALDO LOCAL en la moneda del clientes
            // obtener el saldo pendiente de pago de todo el grupo cial
            decimal saldo_local_bs = 0;
            try
            {
                //var codigosLocales = CodigosIguales_local.Split(',').ToList();
                var result = await _context.coplancuotas
                    .Join(_context.veremision,
                        p1 => p1.coddocumento,
                        p2 => p2.codigo,
                        (p1, p2) => new { p1, p2 })
                    .Where(joined => CodigosIguales_local.Contains(joined.p1.cliente)
                                    && joined.p1.moneda == monedabase
                                    && joined.p2.anulada == false)
                    .Select(joined => new { joined.p1.monto, joined.p1.montopagado })
                    .ToListAsync();
                // Realizar la operación de resta después de traer los datos de la base de datos
                saldo_local_bs = result.Sum(item => item.monto - item.montopagado) ?? 0;
            }
            catch (Exception)
            {
                saldo_local_bs = 0;
                //throw;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7
            // BUSCA EL SALDO LOCAL en SUS
            // obtener el saldo pendiente de pago de todo el grupo cial
            decimal saldo_local_us = 0;
            
            try
            {
                var result = await _context.coplancuotas
                                        .Join(_context.veremision,
                                            p1 => p1.coddocumento,
                                            p2 => p2.codigo,
                                            (p1, p2) => new { p1, p2 })
                                        .Where(joined => CodigosIguales_local.Contains(joined.p1.cliente)
                                                        && joined.p1.moneda == monedaext
                                                        && joined.p2.anulada == false)
                                        .Select(joined => new { joined.p1.monto, joined.p1.montopagado })
                                        .ToListAsync();
                // Realizar la operación de resta después de traer los datos de la base de datos
                saldo_local_us = result.Sum(item => item.monto - item.montopagado) ?? 0;
            }
            catch (Exception)
            {
                saldo_local_us = 0;
                //throw;
            }

            // busca el SALDO NACIONAL si tiene sucursales en otras agencia
            // implementado el 09-05-2020
            var _result_saldo_x_pagar_nal_bs = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, cliente_principal_local, monedabase);
            double saldo_x_pagar_nal_bs = _result_saldo_x_pagar_nal_bs.resp;

            var _result_saldo_x_pagar_nal_us = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, cliente_principal_local, monedaext);
            double saldo_x_pagar_nal_us = _result_saldo_x_pagar_nal_us.resp;
            double saldo_local_de_bs_a_us = 0;
            double saldo_local_de_us_a_bs = 0;

            try
            {
                if (moneda_credito == "US")
                {
                    // si el credito es en US se debe convertir los saldos de BS a US y sumar el saldo de US
                    saldo_local_de_bs_a_us = (double)await tipocambio._conversion(_context, monedaext, monedabase, DateTime.Now.Date, saldo_local_bs);
                    // 1ro.- actualizar el saldo descontando las deudas locales y de las otras agencias
                    var clientes = await _context.vecliente
                        .Where(c => CodigosIguales_local.Contains(c.codigo))
                        .ToListAsync();

                    foreach (var cliente in clientes)
                    {
                        cliente.creditodisp = (decimal?)((double?)cliente.credito - ((double)saldo_local_us + saldo_x_pagar_nal_us + saldo_local_de_bs_a_us));
                    }

                    _context.SaveChanges();

                }
                else
                {
                    // si el credito es en BS se debe convertir los saldos de US a BS y sumar el saldo de BS
                    saldo_local_de_us_a_bs = (double)await tipocambio._conversion(_context, monedabase, monedaext, DateTime.Now.Date, saldo_local_us);
                    var clientes = await _context.vecliente
                        .Where(c => CodigosIguales_local.Contains(c.codigo))
                        .ToListAsync();

                    foreach (var cliente in clientes)
                    {
                        cliente.creditodisp = (decimal?)((double?)cliente.credito - ((double)saldo_local_bs + saldo_x_pagar_nal_bs + saldo_local_de_us_a_bs));
                    }

                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
                return (false, "Ocurrio un error al actualizar el credito disponible de los clientes: " + CodigosIguales_local + "Alerta!!!");
            }

            // 2do.- Actualizar el saldo en las sucursales
            if (actualizar_sucursales_nal)
            {
                if (moneda_credito == "US")
                {
                    var resultAct = await cliente.Actualizar_Credito_Sucursales_Nacional(_context, cliente_principal_local, monedacliente, credito_principal, (double)saldo_local_us, saldo_x_pagar_nal_us + saldo_local_de_bs_a_us);
                    if (resultAct.value == false)
                    {
                        return (false, resultAct.msg);
                    }
                }
                else
                {
                    var resultAct = await cliente.Actualizar_Credito_Sucursales_Nacional(_context, cliente_principal_local, monedacliente, credito_principal, (double)saldo_local_bs, saldo_x_pagar_nal_bs + saldo_local_de_us_a_bs);
                    if (resultAct.value == false)
                    {
                        return (false, resultAct.msg);
                    }
                }
            }


            return (true,"");
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



        public async Task<(bool resp, string msgAlertOpcional, string msgInfo)> Añadir_Credito_Temporal_Automatico_Nal(DBContext _context, double VAR_MONTO_CREDITO_DISPONIBLE, string moneda_cliente, string codcliente, string usuarioreg, string codempresa, string docproforma, double monto_proforma, string moneda_proforma)
        {
            bool resultado = new bool();
            string CodigosIguales = "";
            string codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);
            if (await cliente.NIT(_context, codigoPrincipal) == await cliente.NIT(_context,codcliente))
            {
                codcliente = codigoPrincipal;
                CodigosIguales = await cliente.CodigosIgualesMismoNIT(_context, codcliente);   // <------solo los de mismo NIT
            }
            else
            {
                CodigosIguales = "'" + codcliente + "'";
            }
            if (await configuracion.Permitir_Añadir_Creditos_Temporales_Automatico(_context, codempresa))
            {
                if (await cliente.Cliente_Tiene_Sucursal_Nacional(_context,codcliente))
                {
                    // Desde 08 - 02 - 2024 Si el cliente tiene su cliente matriz en otra agencia ingresa aqui
                    // verificar si el cliente tiene algun credito temporal vigente a la fecha
                    // si no tiene el sistema añade un credito temporal segun los parametros definidos
                    // ###################################
                    // Obtener de donde es el codigo principal de la matriz del cliente
                    // si el cliente es parte de una agrupacion cial a nivel nacional entre agencias de Pertec a nivel nacional
                    string casa_matriz_Nacional = await cliente.CodigoPrincipal_Nacional(_context,codcliente);
                    int ag_matriz_nacional = await cliente.Almacen_Casa_Matriz_Nacional(_context, codcliente);
                    if (ag_matriz_nacional == await cliente.almacen_de_cliente(_context,codcliente))
                    {
                        //realizar la verificacion del credito temporal en la conexion local
                        // goto es_clientelocal;
                        var result = await RealizarVerificacionCreditoTemporal_es_clientelocal(_context, VAR_MONTO_CREDITO_DISPONIBLE, moneda_cliente, codcliente, usuarioreg, codempresa, docproforma, monto_proforma, moneda_proforma);
                        return (result.resp, result.msgAlertOpcional, result.msgInfo);
                    }
                    // ###################################
                    var respCredTempNalVig = await Cliente_Tiene_Credito_Temporal_Automatico_Vigente_MatrizNal(_context, codcliente, casa_matriz_Nacional, ag_matriz_nacional);
                    if (!respCredTempNalVig.resp)
                    {
                        // los creditos temporales automaticos solo se añaden o se asigana para clientes que tienen linea de credito fija valida
                        var respCliTienCredValNal = await Cliente_Tiene_Linea_De_Credito_Valida_MatrizNal(_context, codcliente, casa_matriz_Nacional, ag_matriz_nacional);
                        if (!respCliTienCredValNal.resp)
                        {
                            return (respCliTienCredValNal.resp, respCliTienCredValNal.msgInfo, "Se intento asignar un credito temporal de manera automatica, pero se verifico no cuenta con linea de credito fija!!!");
                        }
                        else
                        {
                            double monto_credito_fijo = await Credito_Fijo_Asignado_Vigente_MatrizNal()
                        }

                    }
                }
                else
                {
                //verificar si el cliente tiene algun credito temporal vigente a la fecha
                //si no tiene el sistema añade un credito temporal segun los parametros definidos
                es_clientelocal:

                    return true;
                }
            }
            return true;

        }


        public async Task<(bool resp, string msgAlertOpcional, string msgInfo)> RealizarVerificacionCreditoTemporal_es_clientelocal(DBContext _context, double VAR_MONTO_CREDITO_DISPONIBLE, string moneda_cliente, string codcliente, string usuarioreg, string codempresa, string docproforma, double monto_proforma, string moneda_proforma)
        {
            if (! await Cliente_Tiene_Credito_Temporal_Automatico_Vigente(_context,codcliente))
            {
                // los creditos temporales automaticos solo se añaden o se asigana para clientes que tienen linea de credito fija valida
                if (! await Cliente_Tiene_Linea_De_Credito_Valida(_context,codcliente))
                {
                    return (false, "","Se intento asignar un credito temporal de manera automatica, pero se verifico no cuenta con linea de credito fija!!!");
                }

                double monto_credito_fijo = await Credito_Fijo_Asignado_Vigente(_context, codcliente);
                string moneda_credito = await Credito_Fijo_Asignado_Vigente_Moneda(_context, codcliente);

                double monto_max_temporal = await Monto_Credito_Temp_Automatico(_context, monto_credito_fijo, moneda_credito, codempresa);
                string doc_pf = docproforma;

                double maximo_incremento_posible = monto_max_temporal - monto_credito_fijo;

                double credito_faltante = Math.Abs(VAR_MONTO_CREDITO_DISPONIBLE - monto_proforma);
                credito_faltante = Math.Round(credito_faltante, 2);

                if (moneda_credito != moneda_proforma)
                {
                    // convertir a la moneda de la PF
                    maximo_incremento_posible = (double)await tipocambio._conversion(_context, moneda_proforma, moneda_credito, DateTime.Today.Date, (decimal)maximo_incremento_posible);
                }

                // verificar si con el maximo incremento le alcanza
                // si le alcanza se insertara un credito temporal
                if (maximo_incremento_posible > credito_faltante)
                {
                    string msgAlert = "Al cliente le falta credito por un monto de: " + credito_faltante + "(" + moneda_proforma + ") sin embargo se añadira un credito temporal automatico maximo de: " + monto_max_temporal + " (" + moneda_credito + ") con lo cual el cliente tendra credito suficiente para esta proforma.";

                    // obtener los datos el ultimo credito vigente
                    var dtcredito_acual = await _context.vecliente.Where(i => i.codigo == codcliente).FirstOrDefaultAsync();

                    Datos_Credito OBJCREDITO = new Datos_Credito();
                    OBJCREDITO.codcliente = codcliente;
                    OBJCREDITO.codtipocredito = "TEMP";
                    OBJCREDITO.monto_nuevo_credito = monto_max_temporal;
                    // se añade el credito en la moneda que maneja el cliente
                    // OBJCREDITO.moneda_nuevo_credito = sia_funciones.Cliente.Instancia.monedacliente(codcliente, usuarioreg, codempresa, False)
                    OBJCREDITO.moneda_nuevo_credito = moneda_credito;

                    if (dtcredito_acual != null)
                    {
                        OBJCREDITO.monto_credito_ant = (double)(dtcredito_acual.credito ?? 0);
                        // OBJCREDITO.moneda_credito_ant = CStr(dtcredito_acual.Rows(0)("moneda"))
                        OBJCREDITO.moneda_credito_ant = moneda_credito;
                    }
                    else
                    {
                        OBJCREDITO.monto_credito_ant = 0;
                        OBJCREDITO.moneda_credito_ant = await cliente.monedacliente(_context, codcliente, usuarioreg, codempresa);
                    }
                    OBJCREDITO.es_fijo = false;
                    OBJCREDITO.codtipogarantia = 0;
                    OBJCREDITO.obs_garantia = "";
                    OBJCREDITO.fecha_asigna_credito = await funciones.FechaDelServidor(_context);
                    OBJCREDITO.fecha_vence_credito = await funciones.FechaDelServidor(_context);
                    OBJCREDITO.fecha_vence_credito = OBJCREDITO.fecha_vence_credito.AddDays(await duracioncredito(_context, "TEMP"));

                    OBJCREDITO.hay_fecha_vence_garantia = false;
                    OBJCREDITO.fecha_emision_lc = await funciones.FechaDelServidor(_context);
                    OBJCREDITO.fecha_vence_lc = await funciones.FechaDelServidor(_context);
                    OBJCREDITO.fecha_vence_lc = OBJCREDITO.fecha_vence_lc.AddDays(await duracioncredito(_context, "TEMP"));

                    OBJCREDITO.usuarioreg = usuarioreg;
                    OBJCREDITO.autorizado_por = "AUTOMATICO / " + doc_pf;

                    if ( await Insertar_Credito_Cliente(_context, OBJCREDITO))
                    {
                        await log.RegistrarEvento(_context, usuarioreg, Log.Entidades.Ventana, codcliente, codcliente, "", "PROFORMAS", "Adicion de credito temporal automatico cliente: " + codcliente, Log.TipoLog.Creacion);
                        return (true, msgAlert, "Se ha registrado un credito temporal automatico.");
                    }
                    else
                    {
                        return (false, msgAlert, "No se pudo registrar el credito temporal automatico.");
                    }
                }
                else
                {
                    return (false, "", "Al cliente le falta credito por un monto de:" + credito_faltante + " (" + moneda_proforma + ") y añadiendo un credito temporal maximo de:" + monto_max_temporal + " (" + moneda_credito + ") tampoco sera suficiente,verifique su limite de credito maximo.");
                }
            }
            return (false, "", "Se intento asignar un credito temporal de manera automatica, pero se verifico que el cliente ya tiene uno vigente, por tanto no se puede añadir mas creditos automaticos temporales.");
        }



        public async Task<(bool resp, string msgInfo)> Cliente_Tiene_Linea_De_Credito_Valida_MatrizNal(DBContext _context, string codcliente, string casa_matriz_Nacional, int almacen_casa_matriz_nal)
        {
            DateTime fecha_hoy = DateTime.Today.Date;
            DateTime fecha_aux = DateTime.Today.Date;

            string _codigo_Principal = await cliente.CodigoPrincipal(_context, codcliente);
            bool resultado = false;
            string msgInfo = "";

            var dt_sucursales = await _context.veclientesiguales_nacion
                    .Where(i => i.codcliente_a == casa_matriz_Nacional).ToListAsync();

            /////////////////////////////////////
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
                        // no hay datos
                        resultado = false;
                        msgInfo = "No se encontro la configuracion de conexion para la sucursal!!!";
                        return (resultado, msgInfo);
                    }
                    else
                    {
                        //OBTENER CADENA DE CONEXION
                        var newCadConexVPN = seguridad.Getad_conexion_vpnFromDatabase(dt_conexion.contrasena_sql, dt_conexion.servidor_sql, dt_conexion.usuario_sql, dt_conexion.bd_sql);
                        //alistar la cadena de conexion para conectar a la ag
                        using (var _contextVPN = DbContextFactory.Create(newCadConexVPN))
                        {
                            _codigo_Principal = await cliente.CodigoPrincipal(_contextVPN, reg.codcliente_b);
                            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente

                            string cliente_principal = "";
                            string _CodigosIguales = "";

                            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                            if (await cliente.NIT(_contextVPN, _codigo_Principal) == await cliente.NIT(_contextVPN, reg.codcliente_b))
                            {
                                cliente_principal = _codigo_Principal;
                                _CodigosIguales = await cliente.CodigosIgualesMismoNIT(_contextVPN, reg.codcliente_b);  //<------solo los de mismo NIT
                            }
                            else
                            {
                                cliente_principal = reg.codcliente_b;
                                _CodigosIguales = "'" + reg.codcliente_b + "'";
                            }

                            // busca si el cliente tiene linea de credito registrada
                            var dt = await _contextVPN.vehcredito
                                .Join(_contextVPN.vecliente,
                                    p1 => p1.codcliente,
                                    p2 => p2.codigo,
                                    (p1, p2) => new { p1 = p1, p2 = p2 })
                                .Where(joined => _CodigosIguales.Contains(joined.p1.codcliente) &&
                                                  joined.p1.codtipocredito == "FIJO" &&
                                                  joined.p1.credito > 0 &&
                                                  joined.p1.revertido == false)
                                .OrderByDescending(joined => joined.p1.fecha)
                                .Select(joined => new { Cliente = joined.p2.razonsocial, Credito = joined.p1 })
                                .ToListAsync();
                            if (dt.Count() > 0)
                            {
                                foreach (var reg2 in dt)
                                {
                                    // verificar si la linea de credito no se vencio o si la letra de cambio no se vencio
                                    if (fecha_aux > reg2.Credito.fechavenc || fecha_aux > reg2.Credito.fecha_vence_garantia)
                                    {
                                        resultado = false;
                                        break;
                                    }
                                    else
                                    {
                                        resultado = true; break;
                                    }
                                }
                            }
                            else
                            {
                                resultado = false;
                            }
                        }
                    }
                }
            }
            /////////////////////////////////
            return (resultado, msgInfo);
        }



        public async Task<(bool resp, string msgInfo)> Cliente_Tiene_Credito_Temporal_Automatico_Vigente_MatrizNal(DBContext _context, string codcliente, string casa_matriz_Nacional, int almacen_casa_matriz_nal)
        {
            DateTime fecha_actual = DateTime.Today.Date;
            string _codigo_Principal = await cliente.CodigoPrincipal(_context, codcliente);
            bool resultado = false;
            string msgInfo = "";

            var dt_sucursales = await _context.veclientesiguales_nacion
                    .Where(i => i.codcliente_a == casa_matriz_Nacional).ToListAsync();

            /////////////////////////////////////
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
                        // no hay datos
                        resultado = false;
                        msgInfo = "No se encontro la configuracion de conexion para la sucursal!!!";
                        return (resultado, msgInfo);
                    }
                    else
                    {
                        //OBTENER CADENA DE CONEXION
                        var newCadConexVPN = seguridad.Getad_conexion_vpnFromDatabase(dt_conexion.contrasena_sql, dt_conexion.servidor_sql, dt_conexion.usuario_sql, dt_conexion.bd_sql);
                        //alistar la cadena de conexion para conectar a la ag
                        using (var _contextVPN = DbContextFactory.Create(newCadConexVPN))
                        {
                            _codigo_Principal = await cliente.CodigoPrincipal(_contextVPN, reg.codcliente_b);
                            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente

                            string cliente_principal = "";
                            string _CodigosIguales = "";

                            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                            if (await cliente.NIT(_contextVPN, _codigo_Principal) == await cliente.NIT(_contextVPN, reg.codcliente_b))
                            {
                                cliente_principal = _codigo_Principal;
                                _CodigosIguales = await cliente.CodigosIgualesMismoNIT(_contextVPN, reg.codcliente_b);  //<------solo los de mismo NIT
                            }
                            else
                            {
                                cliente_principal = reg.codcliente_b;
                                _CodigosIguales = "'" + reg.codcliente_b + "'";
                            }
                            var consulta_dt = await _contextVPN.vehcredito
                                .Where(vc => _CodigosIguales.Contains(vc.codcliente) &&
                                             _contextVPN.vetipocredito
                                                     .Where(tp => tp.es_fijo == false)
                                                     .Select(tp => tp.codigo)
                                                     .Contains(vc.codtipocredito) &&
                                             vc.autoriza.Contains("AUTOMATICO") &&
                                             vc.revertido == false)
                                .OrderByDescending(vc => vc.fecha)
                                .ThenByDescending(vc => vc.horareg).FirstOrDefaultAsync();
                            if (consulta_dt == null)
                            {
                                resultado = false;
                            }
                            else
                            {
                                DateTime fecha_vence_asigna_credito_temp = consulta_dt.fecha;
                                DateTime fecha_vence_credito_temp = consulta_dt.fechavenc;
                                if (fecha_actual.Date < fecha_vence_credito_temp.Date)
                                {
                                    resultado = true;
                                }
                                else
                                {
                                    resultado = false;
                                }
                                // esto se ejecuta para asegurar que el credito este marcado como revertido
                                var credito = await _contextVPN.vehcredito.FirstOrDefaultAsync(vc => vc.codigo == consulta_dt.codigo);
                                if (credito != null)
                                {
                                    credito.revertido = true;
                                    await _contextVPN.SaveChangesAsync();
                                }
                            }

                        }
                    }
                }
            }
            return (resultado,msgInfo);
        }


        public async Task<bool> Cliente_Tiene_Credito_Temporal_Automatico_Vigente(DBContext _context, string codcliente)
        {
            DateTime fecha_actual = DateTime.Today.Date;

            var tiposCreditoFijo = await _context.vetipocredito.Where(tc => tc.es_fijo == false).Select(tc => tc.codigo).ToListAsync();

            var dt = await _context.vehcredito
                .Where(vc => codcliente.Contains(vc.codcliente) &&
                             tiposCreditoFijo.Contains(vc.codtipocredito) &&
                             vc.autoriza.Contains("AUTOMATICO") &&
                             vc.revertido == false)
                .OrderByDescending(vc => vc.fecha)
                .ThenByDescending(vc => vc.horareg)
                .FirstOrDefaultAsync();

            if (dt == null)
            {
                return false;
            }
            DateTime fecha_vence_asigna_credito_temp = dt.fecha;
            DateTime fecha_vence_credito_temp = dt.fechavenc;
            if (fecha_actual.Date < fecha_vence_credito_temp.Date)
            {
                return true;
            }

            // esto se ejecuta para asegurar que el credito este marcado como revertido
            var credito = await _context.vehcredito.FirstOrDefaultAsync(vc => vc.codigo == dt.codigo);
            if (credito != null)
            {
                credito.revertido = true;
                await _context.SaveChangesAsync();
            }
            return false;
        }



        public async Task<double> Monto_Credito_Temp_Automatico(DBContext _context, double monto_credito_fijo, string moneda_credito, string codempresa)
        {
            string monedabase = await empresa.monedabase(_context, codempresa);
            double resultado = 0;
            if (moneda_credito == monedabase)
            {
                // realizar el calculo en BS
                if (monto_credito_fijo <= 10440)
                {
                    resultado = monto_credito_fijo * 0.3;
                    resultado = resultado + monto_credito_fijo;
                }else if(monto_credito_fijo >= 10446.96 && monto_credito_fijo <= 20880)
                {
                    resultado = monto_credito_fijo * 0.15;
                    resultado = resultado + monto_credito_fijo;
                }
                else
                {
                    resultado = monto_credito_fijo * 0.1;
                    resultado = resultado + monto_credito_fijo;
                }
            }
            else
            {
                // realizar el calculo en US
                if (monto_credito_fijo <= 1500)
                {
                    resultado = monto_credito_fijo * 0.3;
                    resultado = resultado + monto_credito_fijo;
                }else if(monto_credito_fijo >= 1501 && monto_credito_fijo <= 3000)
                {
                    resultado = monto_credito_fijo * 0.15;
                    resultado = resultado + monto_credito_fijo;
                }
                else
                {
                    resultado = monto_credito_fijo * 0.1;
                resultado = resultado + monto_credito_fijo;
                }
            }
            return resultado;
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Esta funcion devuelve elcredito disponible del cliente expresado en la moneda indicada tdc del dia
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<double> Credito_Fijo_Asignado_Vigente(DBContext _context, string codcliente)
        {
            var codigosTipoCreditoFijo = await _context.vetipocredito
                .Where(tp => tp.es_fijo == true)
                .Select(tp => tp.codigo).ToListAsync();

            var creditos = await _context.vehcredito
                .Where(vc => codigosTipoCreditoFijo.Contains(vc.codtipocredito) && vc.revertido == false && codcliente.Contains(vc.codcliente))
                .FirstOrDefaultAsync();

            if (creditos != null)
            {
                return (double)creditos.credito;
            }
            return 0;
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Esta funcion devuelve la duracion de un tipo de credito
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<int> duracioncredito(DBContext _context, string codtipocredito)
        {
            var resultado = await _context.vetipocredito.Where(i => i.codigo == codtipocredito).Select(i => i.duracion).FirstOrDefaultAsync();
            return resultado;
        }



        public async Task<bool> Insertar_Credito_Cliente(DBContext _context, Datos_Credito CRE)
        {
            // si es credito temporal no graba codigo de tipo de garantia y tampoco obs de garantia
            if (CRE.es_fijo == false)
            {
                CRE.codtipogarantia = 0;
                CRE.obs_garantia = "";
            }

            // si el cliente esta en clientesiguales
            if (!await cliente.EstaEnCodigosIguales(_context,CRE.codcliente))
            {
                //todo codigo de cliente tiene que estar en codigos iguales
                //aunque no sea parte de un grupo
                //por rso aqui se inserta en codigos iguales se es que no esta
                await cliente.ClientesIguales_Insertar(_context, CRE.codcliente, CRE.codcliente, await cliente.almacen_de_cliente_Integer(_context, CRE.codcliente));
            }

            if (await cliente.EstaEnCodigosIguales(_context, CRE.codcliente))
            {
                // obtener la lista de clientes del grupo si lo hubiera
                string codigos = "";
                List<string> codigos_lista = new List<string>();
                string codigoPrincipal = await cliente.CodigoPrincipal(_context, CRE.codcliente);

                // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                if (await cliente.NIT(_context, codigoPrincipal) == await cliente.NIT(_context,CRE.codcliente))
                {
                    codigos = await cliente.CodigosIgualesMismoNIT(_context, codigoPrincipal);   // <------solo los de mismo NIT
                    codigos_lista = await cliente.CodigosIgualesMismoNIT_List(_context, CRE.codcliente);
                }
                else
                {
                    codigoPrincipal = CRE.codcliente;
                    // codigos = "'" + codigoPrincipal + "'";
                    codigos_lista = await cliente.CodigosIgualesMismoNIT_List(_context, CRE.codcliente);
                    codigos = "";
                    foreach (var item in codigos_lista)
                    {
                        if (codigos.Trim().Length == 0)
                        {
                            codigos = "'" + item + "'";
                        }
                        else
                        {
                            codigos = codigos + ",'" + item + "'";
                        }
                    }
                }

                // a cada uno ponerle el credito solicitado
                var dataClientes = await _context.vecliente
                    .Where(c => codigos.Contains(c.codigo))
                    .ToListAsync();

                foreach (var cliente in dataClientes)
                {
                    cliente.credito = (decimal?)CRE.monto_nuevo_credito;
                    cliente.creditodisp = 0;
                }

                await _context.SaveChangesAsync();

                // camviar condiciones de venta del cliente
                // Desde la nueva politica de ventas 2022-2023 todos los clientes pueden realizar compras contado - contra entrega y credito
                // por lo tanto no es necesario ya que el parametro contra entrega se deshabilite una vez que se asigna un credito
                // sia_DAL.Datos.Instancia.EjecutarComando("update vecliente set contra_entrega=0 WHERE codigo in (" & codigos & ") ")
                var clientesSinCreditoDisponible = await _context.vecliente
                    .Where(c => c.creditodisp == null)
                    .ToListAsync();
                foreach (var cliente in clientesSinCreditoDisponible)
                {
                    cliente.creditodisp = cliente.credito;
                }
                await _context.SaveChangesAsync();


                var clientesConExcesoCredito = await _context.vecliente
                    .Where(c => c.creditodisp > c.credito)
                    .ToListAsync();
                foreach (var cliente in clientesConExcesoCredito)
                {
                    cliente.creditodisp = cliente.credito;
                }
                await _context.SaveChangesAsync();

                // grabar en vehcredito
                //solo ionsertar el credito para la casa matriz
                var vehcreditoInsert = codigos_lista.Where(i => i == codigoPrincipal).Select(i => new vehcredito
                {
                    codcliente = i,
                    fecha = CRE.fecha_asigna_credito,
                    credant = (decimal)CRE.monto_credito_ant,
                    monedaant = CRE.moneda_credito_ant,
                    credito = (decimal)CRE.monto_nuevo_credito,

                    moneda = CRE.moneda_nuevo_credito,
                    fechavenc = CRE.fecha_vence_credito,
                    codtipocredito = CRE.codtipocredito,
                    usuario = CRE.usuarioreg,
                    autoriza = CRE.autorizado_por,

                    revertido = false,
                    codtipogarantia = CRE.codtipogarantia,
                    obs_garantia = CRE.obs_garantia,
                    horareg = datosProf.getHoraActual(),
                    hay_fecha_vence_garantia = CRE.hay_fecha_vence_garantia,

                    fecha_vence_garantia = CRE.fecha_vence_lc,
                    fecha_emision_garantia = CRE.fecha_emision_lc
                }).ToList();

                try
                {
                    _context.vehcredito.AddRange(vehcreditoInsert);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }

            }
            else
            {
                /*
                 ''//No deberia entrar a esta parte nunca
                ''//cambio de credito disponible
                'Dim nuevocreddisp, credactual, creddisp As Double
                'Dim adaptador As SqlClient.SqlDataAdapter
                'Dim tabla As New DataTable
                'adaptador = sia_DAL.Datos.Instancia.ObtenerDataAdapter("select creditodisp from vecliente where codigo='" & CRE.codcliente & "' ")
                'adaptador.Fill(tabla)
                'If tabla.Rows.Count > 0 Then
                '    creddisp = tabla.Rows(0)("creditodisp")
                'Else
                '    creddisp = 0
                'End If
                'tabla.Dispose()
                'adaptador.Dispose()

                'credactual = CDbl(CRE.monto_credito_ant) * sia_funciones.TipoCambio.Instancia.tipocambio(CRE.moneda_nuevo_credito, CRE.moneda_credito_ant, sia_funciones.Funciones.Instancia.fecha_del_servidor)
                'creddisp = creddisp * sia_funciones.TipoCambio.Instancia.tipocambio(CRE.moneda_nuevo_credito, CRE.moneda_credito_ant, Now.Date)
                'nuevocreddisp = creddisp + (CDbl(CRE.monto_nuevo_credito) - credactual)


                ''//cambiar en cliente y poner en el historico todo dentro de una transaccion
                'Dim coneccion As SqlClient.SqlConnection
                'Dim transaccion As SqlClient.SqlTransaction
                'Dim comando As SqlClient.SqlCommand
                'Dim cadena As String = ""
                'coneccion = sia_DAL.Datos.Instancia.ConeccionIndividual()
                'If coneccion.State = ConnectionState.Open Then
                'Else
                '    'no se pudo conectar a la base de datos
                '    resultado = False
                'End If
                'If resultado Then
                '    transaccion = coneccion.BeginTransaction()
                '    comando = New SqlClient.SqlCommand(cadena, coneccion)
                '    comando.CommandType = CommandType.Text
                '    comando.Transaction = transaccion
                '    '///////////////////////////////////////////////////////////////////////////////////////////////
                '    '///Cambiar credito en cliente
                '    cadena = "UPDATE vecliente SET credito = " & CRE.monto_nuevo_credito & " ,creditodisp = " & CStr(nuevocreddisp) & "  , moneda='" & CRE.moneda_nuevo_credito & "', fvenccred='" & sia_DAL.Datos.Instancia.FechaISO(CRE.fecha_vence_credito) & "' WHERE codigo='" & CRE.codcliente & "' "
                '    comando.CommandText = cadena
                '    Try
                '        comando.ExecuteNonQuery()
                '    Catch e As Exception
                '        resultado = False
                '    End Try

                '    cadena = "UPDATE vecliente SET contra_entrega = 0 WHERE codigo='" & CRE.codcliente & "' "
                '    comando.CommandText = cadena
                '    Try
                '        comando.ExecuteNonQuery()
                '    Catch e As Exception
                '        resultado = False
                '    End Try


                '    '////grabar a historico
                '    cadena = "INSERT INTO vehcredito(codcliente,fecha,credant,monedaant,credito,moneda,fechavenc,codtipocredito,usuario,autoriza,revertido,codtipogarantia,obs_garantia,horareg,hay_fecha_vence_garantia,fecha_vence_garantia,fecha_emision_garantia) VALUES('" + CRE.codcliente + "' , '" + sia_DAL.Datos.Instancia.FechaISO(CRE.fecha_asigna_credito) + "' , " + CRE.monto_credito_ant + " , '" + CRE.moneda_credito_ant + "' , " + CRE.monto_nuevo_credito + " , '" + CRE.moneda_nuevo_credito + "' , '" + sia_DAL.Datos.Instancia.FechaISO(CRE.fecha_vence_credito) + "' , '" + CRE.codtipocredito + "' , '" + CRE.usuarioreg + "', '" + CRE.autorizado_por + "' , '0','" & CRE.codtipogarantia & "','" & CRE.obs_garantia & "','" & Hour(Now()).ToString("00") + ":" + Minute(Now()).ToString("00") & "','" & IIf(CRE.hay_fecha_vence_garantia = True, "1", "0") & "','" & sia_DAL.Datos.Instancia.FechaISO(CRE.fecha_vence_lc) & "','" & sia_DAL.Datos.Instancia.FechaISO(CRE.fecha_emision_lc) & "') "
                '    comando.CommandText = cadena
                '    Try
                '        comando.ExecuteNonQuery()
                '    Catch e As Exception
                '        resultado = False
                '    End Try

                'End If

                'If resultado Then
                '    transaccion.Commit()
                'Else
                '    transaccion.Rollback()
                'End If

                'comando.Dispose()
                'transaccion.Dispose()
                'coneccion.Close()
                'coneccion.Dispose()
                ''////////////
                 */
            }

            return true;

        }
    }

    public class Datos_Credito
    {
        public string codcliente { get; set; }
        public string codtipocredito { get; set; }
        public double monto_nuevo_credito { get; set; }
        public string moneda_nuevo_credito { get; set; }

        public double monto_credito_ant { get; set; }
        public string moneda_credito_ant { get; set; }
        public bool es_fijo { get; set; }
        public int codtipogarantia { get; set; }

        public string obs_garantia { get; set; }
        public DateTime fecha_asigna_credito { get; set; }
        public DateTime fecha_vence_credito { get; set; }
        public bool hay_fecha_vence_garantia { get; set; }

        public DateTime fecha_emision_lc { get; set; }
        public DateTime fecha_vence_lc { get; set; }
        public string usuarioreg { get; set; }
        public string autorizado_por { get; set; }
    }
}
