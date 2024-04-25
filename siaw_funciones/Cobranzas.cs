using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using static siaw_funciones.Validar_Vta;

namespace siaw_funciones
{
    public class Cobranzas
    {
        Configuracion configuracion = new Configuracion();
        Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        Ventas ventas = new Ventas();
        Funciones funciones = new Funciones();
        //private readonly siaw_funciones.IDepositosCliente depositos_cliente;
        //private readonly siaw_funciones.IVentas ventas;
        TipoCambio tipocambio = new TipoCambio();
        ProntoPago prontopago = new ProntoPago();
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


        public async Task<bool> Registrar_Descuento_Por_Deposito_de_Cbza(DBContext _context, string codcobranza, string codcliente, string codcliente_real, string nit, int codproforma, string cod_empresa, string usuarioreg)
        {
            DateTime Depositos_Desde_Fecha = new DateTime(2015, 5, 13);

            // obtener las distribuciones de la cbza, y validar si las distribuciones o paggo estan dentro las fechas permitadas respecto del vencimiento
            List<consultCocobranza> dt_credito_depositos_pendientes = await Depositos_Cobranzas_Credito_Cliente_Sin_Aplicar(_context, "codcbza", codcobranza, codcliente_real, nit, codcliente_real, false, "APLICAR_DESCTO", codproforma, "", cod_empresa, false, Depositos_Desde_Fecha, false);


            foreach (var reg in dt_credito_depositos_pendientes)
            {
                reg.tipo_pago = "es_cbza_credito";
                if (reg.tipo == 0)
                {
                    //la moneda de pago igual a dela cobrzna
                    reg.monpago = reg.moncbza;
                }
                else
                {
                    reg.monpago = await Moneda_De_Pago_de_una_Cobranza2(_context, reg.codcobranza, reg.codremision);
                }
            }

            //esta funcion totaliza las distribuciones de una cobranza en un solo monto
            List<dtdepositos_pendientes> dtpendientes = await Totalizar_Cobranzas_Depositos_Pendientes(_context, dt_credito_depositos_pendientes);


            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //1ra ETAPA: registrar en cocobranza_deposito con codproforma=0, y con el monto de descuento limite
            Datos_Cbza_Deposito objdatos_dep = new Datos_Cbza_Deposito();
            int coddesextra = await configuracion.emp_coddesextra_x_deposito(_context,cod_empresa);
            double PORCEN_DESCTO = (double)await ventas.DescuentoExtra_Porcentaje(_context, coddesextra);
            double monto_descto = 0;

            try
            {
                foreach (var reg in dtpendientes)
                {
                    objdatos_dep.idcbza = reg.idcbza;
                    objdatos_dep.nroidcbza = reg.nroidcbza;
                    objdatos_dep.codcliente = reg.cliente;
                    objdatos_dep.cod_cbza = reg.codcobranza ?? 0;
                    objdatos_dep.cod_cbza_contado = 0;
                    objdatos_dep.cod_anticipo = 0;
                    objdatos_dep.codproforma = 0;
                    objdatos_dep.monto_dist = (double)reg.monto_dis;
                    monto_descto = Math.Round(objdatos_dep.monto_dist * 0.01 * PORCEN_DESCTO, 2);
                    objdatos_dep.monto_descto = monto_descto;
                    objdatos_dep.monto_rest = 0;

                }
            }
            catch (Exception)
            {

                throw;
            }

            return true;
        }


        public async Task<List<dtdepositos_pendientes>> Totalizar_Cobranzas_Depositos_Pendientes ( DBContext _context ,List<consultCocobranza> tblpendientes)
        {
            DateTime ahora = DateTime.Now;
            List<dtdepositos_pendientes> DtDepositos_Pendientes = new List<dtdepositos_pendientes> ();
            foreach (var reg in tblpendientes)
            {
                if (!await Cobranza_Ya_Esta(DtDepositos_Pendientes, reg.codcobranza))
                {
                    dtdepositos_pendientes newReg = new dtdepositos_pendientes();
                    newReg.cliente = reg.cliente;
                    newReg.codcobranza = reg.codcobranza;
                    newReg.idcbza = reg.idcbza;
                    newReg.nroidcbza = reg.nroidcbza;

                    newReg.iddeposito = reg.iddeposito;
                    newReg.numeroiddeposito = reg.numeroiddeposito;

                    newReg.fecha_cbza = reg.fecha_cbza;
                    newReg.tipo_pago = reg.tipo_pago;
                    newReg.tipo = reg.tipo;

                    newReg.monto_cbza = reg.monto_cbza;
                    //consultar si de tblpendientes el campo monpago es igual a la cobr<naza y si no fuera convertir a la moneda de la cobranza
                    if (reg.moncbza == reg.monpago)
                    {
                        // no convertir
                        newReg.monto_dis = reg.monto_dis;
                    }
                    else
                    {
                        
                        newReg.monto_dis = await tipocambio._conversion(_context, reg.moncbza, reg.monpago, ahora, reg.monto_dis);
                    }
                    newReg.moncbza = reg.moncbza;
                    DtDepositos_Pendientes.Add(newReg);
                }
                else
                {
                    foreach (var reg2 in DtDepositos_Pendientes)
                    {
                        if (reg2.codcobranza == reg.codcobranza)
                        {
                            //consultar si de tblpendientes el campo monpago es igual a la cobr<naza y si no fuera convertir a la moneda de la cobranza
                            if (reg.moncbza == reg.monpago)
                            {
                                //no convertir
                                reg2.monto_dis += reg.monto_dis;
                            }
                            else
                            {
                                reg2.monto_dis += await tipocambio._conversion(_context, reg.moncbza, reg.monpago, ahora, reg.monto_dis);
                            }
                        }
                    }
                }
            }
            return DtDepositos_Pendientes;
        }

        public async Task<bool> Cobranza_Ya_Esta(List<dtdepositos_pendientes> DtDepositos_Pendientes, int codcobranza)
        {
            var res = DtDepositos_Pendientes.Where(i => i.codcobranza == codcobranza).FirstOrDefault();
            if (res == null)
            {
                return false;
            }
            return true;
        }

        public async Task<string> Moneda_De_Pago_de_una_Cobranza2(DBContext _context, int codcobranza, int codremision)
        {
            string resultado = "";
            //buscar el codigo de moneda de la nota de remsiion primero de las ventas a credito en coplancuotas
            var dt_remi_cuotas = await _context.copagos
                .Join(_context.coplancuotas,
                      p => p.codcuota,
                      c => c.codigo,
                      (p, c) => new { p, c })
                .Where(pc => pc.p.codcobranza == codcobranza &&
                             _context.veremision.Any(v => v.codigo == codremision && v.codigo == pc.c.coddocumento))
                .Select(pc => new
                {
                    pc.p.codigo,
                    pc.p.codcobranza,
                    pc.p.codcuota,
                    monto_pago = pc.p.monto,
                    pc.p.fecha,
                    pc.p.tdc,
                    pc.p.codremision,
                    cod_plancuota = pc.c.codigo,
                    codremision_cuota = pc.c.coddocumento,
                    codcliente = pc.c.cliente,
                    pc.c.nrocuota,
                    monto_cuota = pc.c.monto,
                    pc.c.montopagado,
                    pc.c.vencimiento,
                    pc.c.moneda
                }).ToListAsync();
            if (dt_remi_cuotas.Count() > 0)
            {
                foreach (var reg in dt_remi_cuotas)
                {
                    resultado = reg.moneda;
                }
            }
            dt_remi_cuotas.Clear();
            //buscar el codigo de moneda de la nota de remsiion segundo de las ventas a contado en segun el pago hecho y codigo remision
            if (resultado == "")
            {
                var dt_remi_contado = await _context.copagos
                    .Join(_context.veremision,
                          p => p.codremision,
                          r => r.codigo,
                          (p, r) => new { p, r })
                    .Where(pr => pr.p.codcobranza == codcobranza &&
                                 pr.r.codigo == codremision)
                    .Select(pr => new
                    {
                        pr.p.codigo,
                        pr.p.codcobranza,
                        pr.p.codcuota,
                        monto_pago = pr.p.monto,
                        pr.p.fecha,
                        pr.p.tdc,
                        pr.p.codremision,
                        cod_plancuota = pr.r.codigo,
                        codremision_cuota = pr.p.codremision,
                        codcliente = pr.r.codcliente,
                        nrocuota = 0,
                        monto_cuota = pr.r.total,
                        montopagado = pr.r.total,
                        vencimiento = pr.r.fecha,
                        moneda = pr.r.codmoneda
                    }).ToListAsync();
                if (dt_remi_contado.Count() > 0)
                {
                    foreach (var reg in dt_remi_contado)
                    {
                        resultado = reg.moneda;
                    }
                }
                dt_remi_contado.Clear();
            }
            return resultado;
        }

        public async Task<List<consultCocobranza>> Depositos_Cobranzas_Credito_Cliente_Sin_Aplicar(DBContext _context, string BusquedaPor, string CodCbzas, string codcliente, string nit, string codcliente_real, bool buscar_por_nit, string para_que_es, int codproforma, string opcion, string codempresa, bool incluir_aplicados, DateTime nuevos_depositos_desde, bool para_proforma)
        {
            int coddesextra_deposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            List<consultCocobranza>? resultado = new List<consultCocobranza>();
            try
            {
                resultado = await Consulta_Deposito_Cobranzas_Credito_Sin_Aplicar(_context, BusquedaPor, codcliente, nit, codcliente_real, buscar_por_nit, para_que_es, CodCbzas, incluir_aplicados, nuevos_depositos_desde);

                List<consultCocobranza> dt_aux = new List<consultCocobranza>();

                //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                //%% A CONTINUACION SE FILTRAN LAS  CBZAS-DEPOSITOS PARA DEJAR SOLO LAS QUE SON APLICABLES
                //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //1° Revisar si las cbzas-depositos que quedaron con saldos pendientes tienen todavia saldo disponible para aplicar
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                double ttl_saldo = 0;
                double ttl_dist = 0;
                double ttl_ajuste = 0;
                double ttl_recargo_aplicado = 0;
                string cadena_negativos = "";
                double saldo_recargo = 0;


                foreach (var reg in resultado)
                {
                    if (reg.tipo == 0)
                    {
                        ttl_saldo = 0;
                        ttl_dist = 0;
                        ttl_ajuste = 0;
                        // 1ro. Obtener el monto que se aplico en descuentos por deposito
                        ttl_dist = await depositos_cliente.Total_Descuentos_Por_Deposito_Aplicado_De_Cobranza_En_Proforma(_context, reg.codcobranza, codproforma, reg.moncbza);
                        //2do. Obtener el monto que se aplico en ajustes
                        ttl_ajuste += await depositos_cliente.Total_Ajustes_Por_Deposito_Aplicado_De_Cobranza(_context, reg.codcobranza, codproforma, reg.moncbza);

                        ttl_saldo = (double)reg.monto_dis - (ttl_dist + ttl_ajuste);
                        ttl_saldo = Math.Round(ttl_saldo, 2);

                        if (ttl_saldo > 0)
                        {
                            reg.monto_dis = (decimal)ttl_saldo;
                            dt_aux.Add(reg);
                        }
                        if (ttl_saldo < 0)
                        {
                            //verificar si ya se aplico el recargo en otras proformas, y aplicar recargo solo si hay saldo
                            ttl_saldo = Math.Abs(ttl_saldo);
                            ttl_recargo_aplicado = await depositos_cliente.Total_Recargos_Por_Deposito_Aplicado_De_Cobranza_En_Proforma(_context, reg.codcobranza, codproforma, reg.moncbza);
                            saldo_recargo = ttl_saldo - ttl_recargo_aplicado;
                            /*
                            if (saldo_recargo > 0)
                            {
                            //mero a cero quiere decir que se aplico mas descto
                            }
                            */
                        }
                    }
                    else
                    {
                        dt_aux.Add(reg);
                    }
                }

                resultado.Clear();
                resultado = new List<consultCocobranza>();
                resultado = dt_aux;
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //2° verificar si la nota pagada ES contra entrega, si es contra entrega, la fecha de vecimiento a aconsiderar
                //    debe ser la fecha que se entrego la mercaderia, es decir la fecha que se registro como entrega en: vedespacho.fdespacho
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                /*
                For i = 0 To resultado.Rows.Count - 1
                    reg = resultado.Rows(i)
                    If Not reg("tipo") = 0 Then

                    End If
                Next

                */
                var pp = 1;
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //3° verificar los pagos a cuotas que vencieron domingo, segun instruido por JRA en fecha 17-07-2015 via telefono, 
                //   si la fecha de vencimiento cae en domingo u otro dia no habil, la fecha de vencimiento se recorre un dia
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                dt_aux = await Analizar_Cobranzas_Pago_Cuotas(_context, resultado);
                resultado.Clear();
                resultado = new List<consultCocobranza>();
                resultado = dt_aux;
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                dt_aux = new List<consultCocobranza>();
                foreach (var reg in resultado)
                {
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //1° si la cbza no es reversion de anticipo volver a verificar manualmente que
                    //realmente no lo sea
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //verifica si es reversion de anticipo

                    if (reg.tipo == 1)
                    {
                        if (await CobranzaMontoAnticipo(_context, reg.codcobranza) > 0)
                        {
                            reg.valido = "No";
                            goto verifica_si_se_uso;
                        }
                    }
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //1° si la cbza SI es reversion de anticipo volver a verificar manualmente que
                    //realmente no lo sea
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //verifica si es reversion de anticipo
                    if (reg.tipo == 2)
                    {
                        if (await CobranzaMontoAnticipo(_context, reg.codcobranza) > 0)
                        {
                            reg.valido = "No";
                        }
                    }
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //2° aqui se verifica si la cbza alguna vez ya fue usada para aplicar descuento por deposito, esta revision
                //se realiza en vedesextraprof y/o vedesextraremi, en caso de que el codcobranza ya haya sido
                //utilizada estara vinculado a alguna proforma-nrremision
                //si tipo no es CERO, es decir es: 1 es decir estar como cobranza NUEVA que no se aplico descto
                //por deposito pero ala vez se verifica que ya esta en alguna proforma asignado en vedesextraprof, entonces se excluye
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                verifica_si_se_uso:
                    if (reg.tipo != 0)
                    {
                        if (await depositos_cliente.Cobranza_Se_Aplico_Para_Descuento_Por_Deposito_2(_context, reg.codcobranza, codproforma) == false)
                        {
                            reg.valido = "Si";
                        }
                        else
                        {
                            reg.valido = "No";
                        }
                    }
                    else
                    {
                        //si es tipo=0 directo se marca como valido porque los 
                        //tipo son cero son los depositos que quedaron con saldo de
                        //descuento por aplicar
                        reg.valido = "Si";
                    }
                continuar_aqui:
                    //Desde 08 / 12 / 2023 verificar que los descuentos por deposito no esten en la tabla cocobranza_deposito_excluidos
                    //ya que en esta tabla de inserto todas aquellas descuentos por deposito mal generadas de ventas casual-referenciales en fecha 08/12/2023
                    //solo si es para añadir descuento en proforma
                    if (para_proforma)
                    {
                        if (reg.valido == "Si")
                        {
                            if (await depositos_cliente.Deposito_Esta_Excluido_por_Venta_Casual_Referencial(_context, reg.codcobranza))
                            {
                                reg.valido = "No";
                            }
                        }
                    }
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //3° Aqui se filtran las cbzas y se carga la tabla solo con las validas o usables
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    if (reg.valido == "Si")
                    {
                        dt_aux.Add(reg);
                    }
                }
                resultado.Clear();
                resultado = dt_aux;


            }
            catch (Exception)
            {
                resultado = null;
            }

            return resultado;
        }

        public async Task<double> CobranzaMontoAnticipo(DBContext _context, int codcobranza)
        {
            var resultado = await _context.cocobranza_anticipo.Where(i => i.codcobranza == codcobranza).SumAsync(i => i.monto);
            return (double)(resultado ?? 0);
        }



        public async Task<List<consultCocobranza>> Analizar_Cobranzas_Pago_Cuotas(DBContext _context, List<consultCocobranza> resultado)
        {
            List<consultCocobranza> dt_aux = new List<consultCocobranza> ();
            int dias_prorroga = 0;
            int dias = 0;
            DateTime mi_fecha = DateTime.Now;
            try
            {
                foreach (var reg in resultado)
                {
                    dias_prorroga = 0;
                    if (reg.tipo != 0)
                    {
                        reg.fecha_cbza = reg.fdeposito;
                        ///////////////////////////////*************DESDE 30-11-2017**********************************///////////////////////////////////////
                        //por instruccion de JRA la fecha de vencimiento debe ser la fecha de vencimiento para el cliente NO para pertec
                        //que en el caso de las notas que son con descuento pronto pago se disminuye normalmente 2 o 4 dias

                        DateTime fecha_control = new DateTime(2017, 11, 30);
                        if (reg.fecha_remi >= fecha_control)
                        {
                            dias = await ventas.Disminuir_Fecha_Vence(_context, reg.codremision, reg.cliente);
                            mi_fecha = reg.vencimiento.AddDays(dias);
                            reg.vencimiento = mi_fecha;
                            reg.obs1 = "Fecha Vencimiento Cliente: " + dias + " dias menos, todas las ventanas a partir del 30-11-2017";
                        }
                        else
                        {
                            dias = 0;
                            mi_fecha = reg.vencimiento.AddDays(dias);
                            reg.vencimiento = mi_fecha;
                            reg.obs1 = "Fecha de Vencimiento Pertec: " + dias + " dias menos!!!";
                        }
                        ///////////////////////////////**************************************************************///////////////////////////////////////

                        if (reg.contra_entrega)
                        {
                            //++++++++++++++++SI   ES CONTRA ENTREGA+++++++++++++++
                            if (reg.fecha_cbza > reg.vencimiento)
                            {
                                int codProf = await ventas.codproforma_de_remision(_context, reg.codremision);
                                if (await ventas.Pedido_Esta_Despachado(_context, codProf))
                                {
                                    DateTime fdespacho = await ventas.Obtener_Fecha_Despachado_Pedido(_context, codProf);
                                    reg.vencimiento = fdespacho;
                                    reg.obs = "Contra Entrega Vence la fecha de entrega: " + fdespacho.ToShortDateString();

                                    if (reg.fecha_cbza <= reg.vencimiento)
                                    {
                                        reg.valido = "Si";
                                        dt_aux.Add(reg);
                                    }
                                    else if (reg.deposito_en_mora_habilita_descto)
                                    {
                                        reg.valido = "Si";
                                        reg.obs = "Pago Fuera de Tiempo!!!, pero con autorizacion para descuento.";
                                        dt_aux.Add(reg);
                                    }
                                    else
                                    {
                                        reg.valido = "No";
                                    }
                                }
                            }
                            else
                            {
                                reg.valido = "Si";
                                dt_aux.Add(reg);
                            }
                        }
                        else
                        {
                            //++++++++++++++++SI NO ES CONTRA ENTREGA+++++++++++++++
                            DateTime mifecha_vence = reg.vencimiento;
                            //si es domingo añadir 1 dia
                            if (reg.vencimiento.DayOfWeek == DayOfWeek.Sunday || await prontopago.DianoHabil(_context, reg.codalmacen, reg.vencimiento))
                            {
                                mifecha_vence = mifecha_vence.AddDays(1);
                                reg.vencimiento = mifecha_vence;
                                dias_prorroga += 1;
                            }
                            //añadir dias prorroga hasta que sea una fecha habil
                            while (await prontopago.DianoHabil(_context, reg.codalmacen, mifecha_vence))
                            {
                                mifecha_vence = mifecha_vence.AddDays(1);
                                reg.vencimiento = mifecha_vence;
                                dias_prorroga += 1;
                            }
                            if (reg.fecha_cbza <= mifecha_vence)
                            {
                                if (dias_prorroga > 0)
                                {
                                    reg.obs = "Se amplio: " + dias_prorroga + " (dias) a la fecha original de vencimiento(cliente) porque esta vencia en una fecha no habil!!!";
                                    reg.valido = "Si";
                                    dt_aux.Add(reg);
                                }
                            }
                            else if (reg.fecha_cbza > mifecha_vence && reg.deposito_en_mora_habilita_descto == true)
                            {
                                //si la fecha de pago(deposito) es posterior al vencimiento, es decir no se pago a tiempo
                                //peor tiene autorizado para descuento por deposito aun cuando fue pagada con restraso, entonces
                                //habilita el pago para descto por deposito
                                reg.obs = "Pago Fuera de Tiempo!!!, pero con autorizacion para descuento.";
                                reg.valido = "Si";
                                dt_aux.Add(reg);
                            }
                            else
                            {
                                reg.valido = "No";

                            }
                        }
                    }
                    else
                    {
                        dt_aux.Add(reg);
                    }
                }
                return dt_aux;
            }
            catch (Exception e)
            {
                throw;
            }
            
        }

        public async Task<List<consultCocobranza>> Consulta_Deposito_Cobranzas_Credito_Sin_Aplicar (DBContext _context, string busqueda_por, string codcliente, string nit, string codcliente_real, bool buscar_por_nit, string Para_Que_Es, string codcbzas, bool incluir_aplicados, DateTime buscar_depositos_desde)
        {
            if (!buscar_por_nit)
            {
                nit = "";
            }
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //consulta busca los despositos(cbzas) que ya se usaron para aplicar descuento por deposito 
            //pero tienen: saldorest por aplicar de descuento
            //y estan en la tabla: cocobranza_deposito  (estos no son reversiones de anticipos)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //tipo=0 SON SALDOS PENDIENTES DE DEPOSITOS YA APLICADOS

            // PARTE 1/6

            // Paso 1: Unir cocobranza_deposito y cocobranza

            
            var query1 = await _context.cocobranza_deposito
                .Join(
                _context.cocobranza,
                p1 => p1.codcobranza,
                p2 => p2.codigo,
                (p1, p2) => new { p1, p2 }
            )
                .Join(
                _context.fndeposito_cliente,
                join => new { id = join.p2.iddeposito, numeroid = join.p2.numeroiddeposito ?? -1 },
                p3 => new { id = p3.id, numeroid = p3.numeroid },
                (join, p3) => new { join.p1, join.p2, p3 }
            )
               .Where(result => result.p2.reciboanulado == false &&
                                 !_context.cocobranza_anticipo.Any(anticipo => anticipo.codcobranza == result.p1.codcobranza))
                .Select(result => new consultCocobranza
                {
                    nro = "1",
                    tipo = 0,
                    aplicado = result.p1.aplicado ?? false,
                    codalmacen = result.p2.codalmacen ?? 0,
                    codcobranza = result.p1.codcobranza,

                    idcbza = result.p2.id,
                    nroidcbza = result.p2.numeroid,
                    fecha_cbza = result.p2.fecha,
                    fdeposito = result.p2.fecha,
                    cliente = result.p2.cliente,

                    nit = result.p3.nit,
                    nomcliente_nit = result.p3.nomcliente_nit,
                    monto_cbza = result.p1.montodist,
                    monto_dis = result.p1.montodescto,
                    moncbza = result.p2.moneda,

                    reciboanulado = result.p2.reciboanulado,
                    iddeposito = result.p2.iddeposito,
                    numeroiddeposito = result.p2.numeroiddeposito ?? 0,
                    deposito_cliente = result.p2.deposito_cliente ?? false,
                    contra_entrega = false,

                    codremision = 0,
                    idrem = "",
                    nroidrem = 0,
                    fecha_remi = result.p2.fecha,
                    nrocuota = 0,

                    vencimiento = result.p2.fecha,
                    vencimiento_cliente = result.p2.fecha,
                    monto = 0,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = true,

                    codcliente = result.p3.codcliente,
                    codigo = result.p2.codigo
                }).ToListAsync(); 
            
            if (!incluir_aplicados)
            {
                // si no incluye los ya aplicados, entonces debe especificar: aplicado='0'
                query1 = query1.Where(i => i.aplicado == false).ToList();
            }

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    // SI ES PARA PROFORMA(APLICAR DESCTO DEPOSITO)
                    query1 = query1.Where(i => i.codcliente == codcliente_real).ToList();
                }
                else
                {
                    // SI ES PARA EL REPORTE (REP ESTADO DE SALDOS DEPOSITOS)
                    query1 = query1.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query1 = query1.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }

            // lo mismo al anterior pero son de reveresiones (estos SI SON reversiones de anticipos)
            // PARTE 2/6
            var query2 = await _context.cocobranza_deposito
                .Join(_context.cocobranza,
                    p1 => p1.codcobranza,
                    p2 => p2.codigo,
                    (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza_anticipo,
                    p => p.p1.codcobranza,
                    p4 => p4.codcobranza,
                    (p, p4) => new { p.p1, p.p2, p4 })
                .Join(_context.coanticipo,
                    pp => pp.p4.codanticipo,
                    p5 => p5.codigo,
                    (pp, p5) => new { pp.p1, pp.p2, pp.p4, p5 })
                .Join(_context.fndeposito_cliente,
                    ppp => new { id = ppp.p5.iddeposito, numeroid= ppp.p5.numeroiddeposito ?? -1 },
                    p3 => new { p3.id, p3.numeroid },
                    (ppp, p3) => new { ppp.p1, ppp.p2, ppp.p4, ppp.p5, p3 })
                .Where(joined =>
                    joined.p2.reciboanulado == false)
                .Select(joined => new consultCocobranza
                {
                    nro = "2",
                    tipo = 0,
                    aplicado = joined.p1.aplicado ?? false,
                    codalmacen = joined.p2.codalmacen ?? 0,
                    codcobranza = joined.p1.codcobranza,
                    idcbza = joined.p2.id,
                    nroidcbza = joined.p2.numeroid,
                    fecha_cbza = joined.p2.fecha,
                    fdeposito = joined.p2.fecha,
                    cliente = joined.p2.cliente,
                    nit = joined.p3.nit,
                    nomcliente_nit = joined.p3.nomcliente_nit,
                    monto_cbza = joined.p1.montodist,
                    monto_dis = joined.p1.montodescto,
                    moncbza = joined.p2.moneda,
                    reciboanulado = joined.p2.reciboanulado,
                    iddeposito = joined.p5.iddeposito,
                    numeroiddeposito = joined.p5.numeroiddeposito ?? 0,
                    deposito_cliente = joined.p5.deposito_cliente ?? false,
                    contra_entrega = false,
                    codremision = 0,
                    idrem = "",
                    nroidrem = 0,
                    fecha_remi = joined.p2.fecha,
                    nrocuota = 0,
                    vencimiento = joined.p2.fecha,
                    vencimiento_cliente = joined.p2.fecha,
                    monto = 0,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = true,

                    codcliente = joined.p3.codcliente,
                    codigo = joined.p2.codigo
                }).ToListAsync();

            if (!incluir_aplicados)
            {
                //si no incluye los ya aplicados, entonces debe especificar: aplicado='0'
                query2 = query2.Where(i => i.aplicado == false).ToList();
            }

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    //SI ES PARA PROFORMA(APLICAR DESCTO DEPOSITO)
                    query2 = query2.Where(i => i.codcliente == codcliente_real).ToList();
                }
                else
                {
                    //SI ES PARA EL REPORTE (REP ESTADO DE SALDOS DEPOSITOS)
                    query2 = query2.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query2 = query2.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //consulta busca las cbza(depositos no asignados)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //busca las cbza(depositos no asignados)
            //tipo=1 SON CBZAS
            // parte 3/6

            var query3 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.coplancuotas, p => p.p2.codcuota, p3 => p3.codigo, (p, p3) => new { p.p1, p.p2, p3 })
                .Join(_context.veremision, p => p.p3.coddocumento, p4 => p4.codigo, (p, p4) => new { p.p1, p.p2, p.p3, p4 })
                .Join(_context.fndeposito_cliente,
                      p => new { Id = p.p1.iddeposito, NumeroId = p.p1.numeroiddeposito ?? -1 },
                      p5 => new { Id = p5.id, NumeroId = p5.numeroid },
                      (p, p5) => new { p.p1, p.p2, p.p3, p.p4, p5 })
                .Join(_context.vecliente, p => p.p1.cliente, p6 => p6.codigo, (p, p6) => new { p.p1, p.p2, p.p3, p.p4, p.p5, p6 })
                .Where(joined => joined.p1.reciboanulado == false
                    && joined.p4.anulada == false
                    && joined.p5.verificado == true      ///SE AÑADIO EL VERIFICADO EN FECHA 11-12-2019
                    && joined.p1.deposito_cliente == true  //si o si debe estar marcada como deposito y ademas tener el id-nroid del deposito
                    && joined.p1.iddeposito != ""
                    && joined.p1.numeroiddeposito != 0
                    && !_context.cocobranza_anticipo.Select(ca => ca.codcobranza).Distinct().Contains(joined.p1.codigo) //(añadido en fecha: 04-09-2021)no debe incluir si son reveresiones de anticipo, porque en la consulta de mas abajo se incluyen estas
                    && joined.p4.fecha >= new DateTime(2015, 5, 13)  //la politica dice que deben ser ventas emitidas a ventas posteriores al lanzamiento de la politica de fecha:
                    && !_context.cocobranza_deposito.Select(cd => cd.codcobranza).Distinct().Contains(joined.p1.codigo))  //las cbzas(depositos) no debe incluir aquellas que tienen saldo pendiente de aplicacion
                .Select(joined => new consultCocobranza
                {
                    nro = "3",
                    tipo = 1,
                    aplicado = false,
                    codalmacen = joined.p1.codalmacen ?? 0,
                    codcobranza = joined.p1.codigo,
                    idcbza = joined.p1.id,
                    nroidcbza = joined.p1.numeroid,
                    fecha_cbza = joined.p1.fecha,
                    fdeposito = joined.p5.fecha ?? new DateTime(1900, 1, 1),
                    cliente = joined.p1.cliente,
                    nit = joined.p5.nit,
                    nomcliente_nit = joined.p5.nomcliente_nit,
                    monto_cbza = joined.p1.monto ?? 0,
                    monto_dis = joined.p2.monto,
                    moncbza = joined.p1.moneda,
                    reciboanulado = joined.p1.reciboanulado,
                    iddeposito = joined.p1.iddeposito,
                    numeroiddeposito = joined.p1.numeroiddeposito ?? 0,
                    deposito_cliente = joined.p1.deposito_cliente ?? false,
                    contra_entrega = joined.p4.contra_entrega ?? false,

                    codremision = joined.p4.codigo,
                    idrem = joined.p4.id,
                    nroidrem = joined.p4.numeroid,
                    fecha_remi = joined.p4.fecha,
                    nrocuota = joined.p3.nrocuota,
                    vencimiento = joined.p3.vencimiento,
                    vencimiento_cliente = joined.p3.vencimiento,
                    monto = joined.p3.monto,
                    montopagado = joined.p3.montopagado ?? 0,
                    deposito_en_mora_habilita_descto = joined.p5.deposito_en_mora_habilita_descto ?? false,

                    codcliente = joined.p5.codcliente,
                    codigo = joined.p1.codigo
                }).ToListAsync();
            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    //SI ES PARA PROFORMA(APLICAR DESCTO DEPOSITO)
                    query3 = query3.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query3 = query3.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    //SI ES PARA EL REPORTE (REP ESTADO DE SALDOS DEPOSITOS)
                    query3 = query3.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query3 = query3.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }
            ////tipo=1 SON CBZAS CONTADO TABLA: COPAGOS.CODREMISION
            // Parte 4/6
            var query4 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.veremision, p => p.p2.codremision, p4 => p4.codigo, (p, p4) => new { p.p1, p.p2, p4 })
                .Join(_context.fndeposito_cliente,
                      p => new { Id = p.p1.iddeposito, NumeroId = p.p1.numeroiddeposito ?? -1 },
                      p5 => new { Id = p5.id, NumeroId = p5.numeroid },
                      (p, p5) => new { p.p1, p.p2, p.p4, p5 })
                .Join(_context.vecliente, p => p.p1.cliente, p6 => p6.codigo, (p, p6) => new { p.p1, p.p2, p.p4, p.p5, p6 })
                .Where(joined => joined.p1.reciboanulado == false
                    && joined.p4.anulada == false
                    && joined.p5.verificado == true  ///SE AÑADIO EL VERIFICADO EN FECHA 11-12-2019
                    && joined.p1.deposito_cliente == true   //si o si debe estar marcada como deposito y ademas tener el id-nroid del deposito
                    && joined.p1.iddeposito != ""
                    && joined.p1.numeroiddeposito != 0
                    && !_context.cocobranza_anticipo.Select(ca => ca.codcobranza).Distinct().Contains(joined.p1.codigo)  //(añadido en fecha: 04-09-2021)no debe incluir si son reveresiones de anticipo, porque en la consulta de mas abajo se incluyen estas
                    && joined.p4.fecha >= new DateTime(2015, 5, 13)    //la politica dice que deben ser ventas emitidas a ventas posteriores al lanzamiento de la politica de fecha:
                    && !_context.cocobranza_deposito.Select(cd => cd.codcobranza).Distinct().Contains(joined.p1.codigo))    //las cbzas(depositos) no debe incluir aquellas que tienen saldo pendiente de aplicacion
                .Select(joined => new consultCocobranza
                {
                    nro = "4",
                    tipo = 1,
                    aplicado = false,
                    codalmacen = joined.p1.codalmacen ?? 0,
                    codcobranza = joined.p1.codigo,
                    idcbza = joined.p1.id,
                    nroidcbza = joined.p1.numeroid,
                    fecha_cbza = joined.p1.fecha,
                    fdeposito = joined.p5.fecha ?? new DateTime(1900, 1, 1),
                    cliente = joined.p1.cliente,
                    nit = joined.p5.nit,
                    nomcliente_nit = joined.p5.nomcliente_nit,
                    monto_cbza = joined.p1.monto ?? 0,
                    monto_dis = joined.p2.monto,
                    moncbza = joined.p1.moneda,
                    reciboanulado = joined.p1.reciboanulado,
                    iddeposito = joined.p1.iddeposito,
                    numeroiddeposito = joined.p1.numeroiddeposito ?? 0,
                    deposito_cliente = joined.p1.deposito_cliente ?? false,
                    contra_entrega = joined.p4.contra_entrega ?? false,
                    codremision = joined.p4.codigo,
                    idrem = joined.p4.id,
                    nroidrem = joined.p4.numeroid,
                    fecha_remi = joined.p4.fecha,
                    nrocuota = 1,
                    vencimiento = joined.p4.fecha,
                    vencimiento_cliente = joined.p4.fecha,
                    monto = joined.p4.total,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = joined.p5.deposito_en_mora_habilita_descto ?? false,

                    codcliente = joined.p5.codcliente,
                    codigo = joined.p1.codigo
                }).ToListAsync();

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    //SI ES PARA PROFORMA(APLICAR DESCTO DEPOSITO)
                    query4 = query4.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query4 = query4.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    //SI ES PARA EL REPORTE (REP ESTADO DE SALDOS DEPOSITOS)
                    query4 = query4.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query4 = query4.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }


            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //consulta busca las cbza que deposito_cliente=0 pero que son reversion de anticipo y su anticipo si deposito de cliente
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //tipo=2 SON REVERSIONES DE ANTICIPO DE TIPO DEPOSITO
            // Parte 5/6

            var query5 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.coplancuotas, p => p.p2.codcuota, p3 => p3.codigo, (p, p3) => new { p.p1, p.p2, p3 })
                .Join(_context.veremision, p => p.p3.coddocumento, p4 => p4.codigo, (p, p4) => new { p.p1, p.p2, p.p3, p4 })
                .Join(_context.cocobranza_anticipo, p => p.p1.codigo, p6 => p6.codcobranza, (p, p6) => new { p.p1, p.p2, p.p3, p.p4, p6 })
                .Join(_context.coanticipo, p => p.p6.codanticipo, p5 => p5.codigo, (p, p5) => new { p.p1, p.p2, p.p3, p.p4, p.p6, p5 })
                .Join(_context.fndeposito_cliente,
                p => new { Id = p.p5.iddeposito, NumeroId = p.p5.numeroiddeposito ?? -1 } ,
                p7 => new { Id = p7.id, NumeroId = p7.numeroid }, (p,p7)=> new { p.p1, p.p2, p.p3, p.p4, p.p5, p.p6, p7 })
                .Join(_context.vecliente, p => p.p1.cliente, p8 => p8.codigo, (p, p8) => new { p.p1, p.p2, p.p3, p.p4, p.p5, p.p6, p.p7, p8 }) 
                .Where(p => p.p1.reciboanulado == false &&
                            p.p4.anulada == false &&
                            p.p5.anulado == false &&
                            p.p7.verificado == true &&   ///SE AÑADIO EL VERIFICADO EN FECHA 11-12-2019
                            p.p5.deposito_cliente == true &&   //si o si debe estar marcada como deposito y ademas tener el id-nroid del deposito
                            p.p5.iddeposito != "" &&
                            p.p5.numeroiddeposito != 0 &&
                            p.p4.fecha >= new DateTime(2015, 5, 13) &&   //la politica dice que deben ser ventas emitidas a ventas posteriores al lanzamiento de la politica de fecha:
                            !_context.cocobranza_deposito.Select(c => c.codcobranza).Contains(p.p1.codigo))   //las cbzas(depositos) no debe incluir aquellas que tienen saldo pendiente de aplicacion
                .OrderBy(p => p.p1.id)
                .Select(p => new consultCocobranza
                { 
                    nro = "5",
                    tipo = 2,
                    aplicado = false,
                    codalmacen = p.p1.codalmacen ?? 0,
                    codcobranza = p.p1.codigo,

                    idcbza = p.p1.id,
                    nroidcbza = p.p1.numeroid,
                    fecha_cbza = p.p1.fecha,
                    fdeposito = p.p7.fecha ?? new DateTime(1900, 1, 1),
                    cliente = p.p1.cliente,

                    nit = p.p7.nit,
                    nomcliente_nit = p.p7.nomcliente_nit,
                    monto_cbza = p.p1.monto ?? 0,
                    monto_dis = p.p2.monto,
                    moncbza = p.p1.moneda,
                    
                    reciboanulado = p.p1.reciboanulado,
                    iddeposito = p.p5.iddeposito,
                    numeroiddeposito = p.p5.numeroiddeposito ?? 0,
                    deposito_cliente = p.p5.deposito_cliente ?? false,
                    contra_entrega = p.p4.contra_entrega ?? false,
                    
                    codremision = p.p4.codigo,
                    idrem = p.p4.id,
                    nroidrem = p.p4.numeroid,
                    fecha_remi = p.p4.fecha,
                    nrocuota = p.p3.nrocuota,
                    
                    vencimiento = p.p3.vencimiento,
                    vencimiento_cliente = p.p3.vencimiento,
                    monto = p.p3.monto,
                    montopagado = p.p3.montopagado ?? 0,
                    deposito_en_mora_habilita_descto = p.p7.deposito_en_mora_habilita_descto ?? false,

                    codcliente = p.p7.codcliente,
                    codigo = p.p1.codigo

                })
                .ToListAsync();

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    //SI ES PARA PROFORMA(APLICAR DESCTO DEPOSITO)
                    query5 = query5.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query5 = query5.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    //SI ES PARA EL REPORTE (REP ESTADO DE SALDOS DEPOSITOS)
                    query5 = query5.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query5 = query5.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }

            //return query5;

            // Parte 6/6


            var query6 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.veremision, p => p.p2.codremision, p4 => p4.codigo, (p, p4) => new { p.p1, p.p2, p4 })
                .Join(_context.cocobranza_anticipo, p => p.p1.codigo, p6 => p6.codcobranza, (p, p6) => new { p.p1, p.p2, p.p4, p6 })
                .Join(_context.coanticipo, p => p.p6.codanticipo, p5 => p5.codigo, (p, p5) => new { p.p1, p.p2, p.p4, p.p6, p5 })
                .Join(_context.fndeposito_cliente,
                      p => new { Id = p.p5.iddeposito, NumeroId = p.p5.numeroiddeposito ?? -1 },
                      p7 => new { Id = p7.id, NumeroId = p7.numeroid },
                      (p, p7) => new { p.p1, p.p2, p.p4, p.p5, p.p6, p7 })
                .Join(_context.vecliente, p => p.p1.cliente, p8 => p8.codigo, (p, p8) => new { p.p1, p.p2, p.p4, p.p5, p6 = p.p6, p7 = p.p7, p8 })
                .Where(p => p.p1.reciboanulado == false &&
                            p.p4.anulada == false &&
                            p.p5.anulado == false &&
                            p.p7.verificado == true &&   ///SE AÑADIO EL VERIFICADO EN FECHA 11-12-2019
                            p.p5.deposito_cliente == true &&   //si o si debe estar marcada como deposito y ademas tener el id-nroid del deposito
                            p.p5.iddeposito != "" &&
                            p.p5.numeroiddeposito != 0 &&
                            p.p4.fecha >= new DateTime(2015, 5, 13) &&   //la politica dice que deben ser ventas emitidas a ventas posteriores al lanzamiento de la politica de fecha:
                            !_context.cocobranza_deposito.Select(c => c.codcobranza).Distinct().Contains(p.p1.codigo))   //las cbzas(depositos) no debe incluir aquellas que tienen saldo pendiente de aplicacion
                .OrderBy(p => p.p2.codcobranza)
                .Select(p => new consultCocobranza
                {
                    nro = "6",
                    tipo = 2,
                    aplicado = false,
                    codalmacen = p.p1.codalmacen ?? 0,
                    codcobranza = p.p1.codigo,

                    idcbza = p.p1.id,
                    nroidcbza = p.p1.numeroid,
                    fecha_cbza = p.p1.fecha,
                    fdeposito = p.p7.fecha ?? new DateTime(1900, 1, 1),
                    cliente = p.p1.cliente,

                    nit = p.p7.nit,
                    nomcliente_nit = p.p7.nomcliente_nit,
                    monto_cbza = p.p1.monto ?? 0,
                    monto_dis = p.p2.monto,
                    moncbza = p.p1.moneda,
                    
                    reciboanulado = p.p1.reciboanulado,
                    iddeposito = p.p5.iddeposito,
                    numeroiddeposito = p.p5.numeroiddeposito ?? 0,
                    deposito_cliente = p.p5.deposito_cliente ?? false,
                    contra_entrega = p.p4.contra_entrega ?? false,
                    
                    codremision = p.p4.codigo,
                    idrem = p.p4.id,
                    nroidrem = p.p4.numeroid,
                    fecha_remi = p.p4.fecha,
                    nrocuota = 1,
                    
                    vencimiento = p.p4.fecha,
                    vencimiento_cliente = p.p4.fecha,
                    monto = p.p4.total,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = p.p7.deposito_en_mora_habilita_descto ?? false,

                    codcliente = p.p7.codcliente,
                    codigo = p.p1.codigo
                })
                .ToListAsync();




            //return query6;
            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    //SI ES PARA PROFORMA(APLICAR DESCTO DEPOSITO)
                    query6 = query6.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query6 = query6.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    //SI ES PARA EL REPORTE (REP ESTADO DE SALDOS DEPOSITOS)
                    query6 = query6.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query6 = query6.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }
            
            var resultadosUnion = query1
                .Union(query2)
                .Union(query3)
                .Union(query4)
                .Union(query5)
                .Union(query6)
                .OrderBy(x => x.tipo)
                .ThenBy(x => x.fecha_cbza)
                .ToList();

            return resultadosUnion;
            
        }
        public async Task<string[]> Id_Nroid_Cobranza(DBContext _context, int codcobranza)
        {
            //List<string> resultado = new List<string>();
            string[] resultado = new string[2];
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.cocobranza
                .Where(v => v.codigo == codcobranza)
                .Select(v => new { v.id, v.numeroid })
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado[0] = result.id.ToString();
                    resultado[1] = result.numeroid.ToString();
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
        public async Task<string[]> Id_Nroid_Cobranza_Contado(DBContext _context, int codcobranza)
        {
            //List<string> resultado = new List<string>();
            string[] resultado = new string[2];
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.cocobranza_contado
                .Where(v => v.codigo == codcobranza)
                .Select(v => new { v.id, v.numeroid })
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado[0] = result.id.ToString();
                    resultado[1] = result.numeroid.ToString();
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
        public async Task<string[]> Id_Nroid_Anticipo(DBContext _context, int codcobranza)
        {
            //List<string> resultado = new List<string>();
            string[] resultado = new string[2];
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.coanticipo
                .Where(v => v.codigo == codcobranza)
                .Select(v => new { v.id, v.numeroid })
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado[0] = result.id.ToString();
                    resultado[1] = result.numeroid.ToString();
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
        public async Task<bool> Existe_Cobranza(DBContext _context, string id, string nroid)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{

                int count = _context.cocobranza
                .Where(c => c.id == id && c.numeroid == int.Parse(nroid))
                .Count();

                if (count > 0)
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }
        public async Task<bool> Existe_Cobranza_Contado(DBContext _context, string id, string nroid)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{

                int count = _context.cocobranza_contado
                .Where(c => c.id == id && c.numeroid == int.Parse(nroid))
                .Count();

                if (count > 0)
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }
        public async Task<DateTime> Fecha_De_Cobranza(DBContext _context, string id, string nroid)
        {
            DateTime resultado;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{

                var fecha = _context.cocobranza
                   .Where(c => c.id == id && c.numeroid == int.Parse(nroid))
                   .OrderByDescending(c => c.fecha)
                   .Select(c => c.fecha)
                   .FirstOrDefault();

                if (fecha != null)
                {
                    resultado = fecha;
                }
                else
                {
                    resultado = DateTime.Today;
                }
                //}
            }
            catch (Exception ex)
            {
                resultado = DateTime.Today;
            }
            return resultado;
        }
        public async Task<DateTime> Fecha_De_Cobranza_Contado(DBContext _context, string id, string nroid)
        {
            DateTime resultado;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{

                var fecha = _context.cocobranza_contado
                   .Where(c => c.id == id && c.numeroid == int.Parse(nroid))
                   .OrderByDescending(c => c.fecha)
                   .Select(c => c.fecha)
                   .FirstOrDefault();

                if (fecha != null)
                {
                    resultado = fecha;
                }
                else
                {
                    resultado = DateTime.Today;
                }
                //}
            }
            catch (Exception ex)
            {
                resultado = DateTime.Today;
            }
            return resultado;
        }
        public async Task<double> CobranzasSinDistribuir(DBContext _context, string codcliente)
        {
            var resultado = await _context.cocobranza
                .Where(v => v.cliente == codcliente
                    && v.autorizada == true)
                .SumAsync(i => i.montorest) ?? 0;
            return (double)resultado;
        }


        public async Task<int> CodAnticipo(DBContext _context, string id_anticipo, int numeroid_anticipo)
        {
            var resultado = await _context.coanticipo.Where(i => i.id == id_anticipo && i.numeroid == numeroid_anticipo).Select(i => i.codigo).FirstOrDefaultAsync();
            return resultado;
        }


        public async Task<string> mostrar_mensajes_depositos_aplicar(DBContext _context, string codempresa, List<consultCocobranza> dt_depositos_pendientes, List<tabladescuentos> tabladescuentos)
        {
            string cadena = "";
            int coddesextra = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            double porcendesc = (double)await ventas.DescuentoExtra_Porcentaje(_context, coddesextra);
            double monto_descto = 0;
            double total_descto = 0;
            double total_descto_aplicado = 0;
            double total_descto_saldo_pendiente_aplicado = 0;
            List<int> lista_depositos = new List<int>();
            List<int> lista_depositos_contado = new List<int>();
            List<int> lista_anticipos_contado = new List<int>();
            string total_descto_aplicado_moneda = "";
            string monto_descto_moneda = "";

            // totalizar el total de descuentos que se aplicaron
            foreach (var reg in tabladescuentos)
            {
                // aqui preguntamos si es el descto por deposito. si lo es obtenemos el codcobranza si es mayor a cero, si es igual a cero
                // obtenemos el codcobranza_contado porque entonces se trata de una cbza contado, de lo contrario es una anticipo
                if (coddesextra == reg.coddesextra)
                {
                    total_descto_aplicado += (double)reg.montodoc;
                    total_descto_aplicado_moneda += reg.codmoneda;
                    if (reg.codcobranza != 0)
                    {
                        lista_depositos.Add(reg.codcobranza ?? 0); 
                    }else if(reg.codcobranza_contado != 0)
                    {
                        lista_depositos_contado.Add(reg.codcobranza_contado ?? 0);
                    }else if(reg.codanticipo != 0)
                    {
                        lista_anticipos_contado.Add(reg.codanticipo ?? 0);
                    }
                }
            }

            if (dt_depositos_pendientes.Count() > 0)
            {
                cadena = funciones.Rellenar("DESCUENTO PENDIENTE POR DEPOSITO DE CLIENTE", 79, " ", false).Replace("\r\n", Environment.NewLine) + Environment.NewLine +
                 "-----------------------------------------------------------------------------------" + Environment.NewLine +
                 funciones.Rellenar("COD     DOC            FECHA       DOC            MONTO     MONTO     %     MONTO", 79, " ", false).Replace("\r\n", Environment.NewLine) + Environment.NewLine +
                 funciones.Rellenar("CLIENT  CBZA           DEPOSITO    DEPOSITO       DEPOSITO  DIST      DESC  DESC ", 79, " ", false).Replace("\r\n", Environment.NewLine) + Environment.NewLine +
                 "-----------------------------------------------------------------------------------" + Environment.NewLine + Environment.NewLine;

                foreach (var reg in dt_depositos_pendientes)
                {
                    if (lista_depositos.Contains(reg.codcobranza) || lista_depositos_contado.Contains(reg.codcobranza) || lista_anticipos_contado.Contains(reg.codcobranza))
                    {
                        monto_descto = 0;
                        if (reg.tipo == 0)
                        {
                            // si es tipo=0 es es saldo de descuento de deposito pendiente
                            // no se debe sacar el procentaje, directo usar el monto
                            monto_descto = (double)reg.monto_dis;
                            monto_descto_moneda = reg.moncbza;
                            monto_descto = Math.Round(monto_descto, 2);
                            // aqui se totaliza cuando son los descuentos por saldos de descuentos pendientes 
                            total_descto_saldo_pendiente_aplicado += (double)reg.monto_dis;
                        }
                        else
                        {
                            monto_descto = porcendesc * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            monto_descto_moneda = reg.moncbza;
                        }
                        total_descto += monto_descto;
                        cadena += funciones.Rellenar(reg.cliente, 6, " ", false);
                        cadena += "  " + funciones.Rellenar(reg.idcbza + " " + reg.nroidcbza, 15, " ", false);
                        cadena += " " + funciones.Rellenar(reg.fecha_cbza.ToString(), 10, " ", false);
                        cadena += "  " + funciones.Rellenar(reg.iddeposito + " " + reg.numeroiddeposito, 13, " ", false);
                        cadena += "  " + funciones.Rellenar(reg.monto_cbza.ToString(), 8, " ", false);
                        cadena += "  " + funciones.Rellenar(reg.monto_dis.ToString(), 8, " ", false);
                        if (reg.tipo == 0)
                        {
                            cadena += "  " + funciones.Rellenar("100", 4, " ", false);
                        }
                        else
                        {
                            cadena += "  " + funciones.Rellenar(porcendesc.ToString(), 4, " ", false);
                        }
                        // cadena &= "  " & sia_funciones.Funciones.Instancia.rellenar(monto_descto, 5, " ", False) & Chr(13)
                        cadena += "  " + funciones.Rellenar(monto_descto.ToString() + " " + monto_descto_moneda.ToString(), 5, " ", false) + "\n";
                    }
                }
                cadena += "-----------------------------------------------------------------------------------\n";
                total_descto = Math.Round(total_descto, 2);
                total_descto_aplicado = Math.Round(total_descto_aplicado, 2);
                total_descto_saldo_pendiente_aplicado = Math.Round(total_descto_saldo_pendiente_aplicado, 2);
                cadena += "Total Descuento Por Depositos Aplicable: " + total_descto.ToString() + " " + monto_descto_moneda + "\n";
                cadena += "Total Descuento Por Depositos Aplicado: " + total_descto_aplicado.ToString() + " " + total_descto_aplicado_moneda + "\n";
                cadena += "-----------------------------------------------------------------------------------\n";
                cadena += "El Descuento aplicado incluye Reintegro Depositos Pendientes Por: " + total_descto_saldo_pendiente_aplicado.ToString() + " " + monto_descto_moneda + "\n";

                if (total_descto_aplicado != total_descto)
                {
                    if (total_descto_aplicado - total_descto > 0.03)
                    {
                        cadena += "Nota.-El monto restante del deposito queda pendiente para la siguiente proforma. \n";
                    }
                }
            }

            return cadena;
        }
    }
}
