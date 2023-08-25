using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SIAW.Data;
using SIAW.Models;
using SIAW.Models_Extra;
using System.Collections.Generic;
using System.Text;

namespace SIAW.Controllers.ventas
{
    public class ClienteCasual
    {
        public async Task<string> validar_crear_cliente(string userConnectionString, string codcliente, string nit, string tipo_doc_id)
        {
            if(! await EsClienteSinNombre(userConnectionString, codcliente))
            {
                return "El codigo ingresado no correspponde a codigo de cliente Sin Nombre!!!";
            }
            string val_nit_correct = await Validar_NIT_Correcto(userConnectionString, nit, tipo_doc_id);
            if(val_nit_correct != "Ok")
            {
                return val_nit_correct;
            }
            string val_nit_cliente_misma_agencia = await verificar_nit_Cliente_Misma_Agencia(userConnectionString, codcliente, nit);
            if (val_nit_cliente_misma_agencia != "Ok")
            {
                return val_nit_cliente_misma_agencia;
            }
            return "Ok";
        }
        public async Task<bool> EsClienteSinNombre(string userConnectionString, string codcliente)
        {
            try
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codcliente_sn = await _context.vecliente_sinnombre
                        .Where(cliente => cliente.codcliente == codcliente)
                        .CountAsync();
                    if (codcliente_sn > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            
        }
        public async Task<string> Validar_NIT_Correcto(string userConnectionString, string nit, string tipo_doc_id)
        {
            /*
            1 CI - CEDULA DE IDENTIDAD
            2 CEX - CEDULA DE IDENTIDAD DE EXTRANJERO
            3 PAS -PASAPORTE
            4 OD - OTRO DOCUMENTO DE IDENTIDAD
            5 NIT - NÚMERO DE IDENTIFICACIÓN TRIBUTARIA
            */

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                int max_largo = await Longitud_Max_NIT_Facturacion(userConnectionString);
                int min_largo = await Longitud_Min_NIT_Facturacion(userConnectionString);
                int largo = nit.Trim().Length;

                if (tipo_doc_id == "1" || tipo_doc_id == "5")
                {
                    if (!int.TryParse(nit, out int number))
                    {
                        return "Debe ingresar un NIT/CI numerico.";
                    }
                    if (largo > max_largo || largo < min_largo)
                    {
                        return "El NIT/CI  debe ser un valor numerico de " + min_largo + " digitos como minimo y " + max_largo + " digitos como máximo, verifique por favor.";
                    }
                }
                return "Ok";
            }
        }
        public async Task<int> Longitud_Max_NIT_Facturacion(string userConnectionString)
        {
            int resultado = 13;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adparametros
                    .Select(parametro => new
                    {
                        longitud_maxima_nit_facturacion = parametro.longitud_maxima_nit_facturacion
                    })
                    .FirstOrDefaultAsync();
                if (result!=null)
                {
                    resultado = (int)result.longitud_maxima_nit_facturacion;
                }
                return resultado;
            }
        }
        public async Task<int> Longitud_Min_NIT_Facturacion(string userConnectionString)
        {
            int resultado = 6;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adparametros
                    .Select(parametro => new
                    {
                        longitud_minima_nit_facturacion = parametro.longitud_minima_nit_facturacion
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (int)result.longitud_minima_nit_facturacion;
                }
                return resultado;
            }
        }

        public async Task<string> verificar_nit_Cliente_Misma_Agencia(string userConnectionString, string codcliente, string nit)
        {
            List<veclientesiguales> codigos_iguales = await CodigosIguales(userConnectionString, codcliente);
            int cuantos = 0;
            string msg = "El N.I.T. " + nit + "  pertenece a otro cliente \n";
            foreach (var cod in codigos_iguales)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultados = await _context.vecliente
                        .Where(v => v.nit == nit && v.codigo.All(char.IsDigit) && v.codigo != cod.codcliente_b)
                        .Select(v => new
                        {
                            v.codigo,
                            v.razonsocial,
                            v.nombre_comercial
                        })
                        .FirstOrDefaultAsync();
                    if(resultados != null)
                    {
                        cuantos++;
                        msg = msg + resultados.codigo + ": " + resultados.razonsocial + " " + resultados.nombre_comercial + "\n";
                    }
                }
            }
            if (cuantos > 0)
            {
                return msg;
            }
            return "Ok";
        }


        public async Task<List<veclientesiguales>> CodigosIguales(string userConnectionString, string codcliente)
        {
            string codigoPrincipal = await CodigoPrincipal(userConnectionString, codcliente);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.veclientesiguales
                    .Where(v => v.codcliente_a == codigoPrincipal && v.codcliente_b.All(char.IsDigit))
                    .Select(v => new veclientesiguales
                    {
                        codcliente_a = v.codcliente_a,
                        codcliente_b = v.codcliente_b
                    })
                    .ToListAsync();
                return result;
            }
        }

        public async Task<string> CodigoPrincipal(string userConnectionString, string codcliente)
        {
            string resultado = codcliente;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.veclientesiguales
                    .Where(v => v.codcliente_b == codcliente)
                    .Select(parametro => new
                    {
                        codcliente_a = parametro.codcliente_a
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result.codcliente_a;
                }
                return resultado;
            }
        }

        // crear cliente casual
        public async Task<bool> Crear_Cliente_Casual (string userConnectionString, int codalmacen, clienteCasual cliCasual)
        {
            //string codpto_vta = await getCodArea(userConnectionString, codalmacen);

            int v = int.Parse(await Ultimo_Codigo_Numerico(userConnectionString)) + 1;
            string cod_cliente = v.ToString();

            float limite_descto_deposito = await Porcentaje_Limite_Descuento_Deposito(userConnectionString, 0);

            // obtener datos de cliente
            vecliente vecliente = await getDataClienteCasual(userConnectionString, cliCasual.codSN, cod_cliente, cliCasual.nomcliente_casual, cliCasual.nit_cliente_casual, cliCasual.email_cliente_casual, cliCasual.usuarioreg, cliCasual.celular_cliente_casual, limite_descto_deposito);
            // obtener datos de tienda
            vetienda vetienda = await getDataClienteCasualTienda(userConnectionString, cod_cliente, codalmacen, cliCasual.nomcliente_casual, cliCasual.email_cliente_casual, cliCasual.celular_cliente_casual, vecliente.fechareg, vecliente.horareg, cliCasual.usuarioreg);
            //valida que datos no esten vacios
            if (vecliente  == null)
            {
                return false;
            }

            // crea cliente
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                _context.vecliente.Add(vecliente);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
            // crea tienda
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                _context.vetienda.Add(vetienda);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }

            if (!await AsignarCuentasCliente(userConnectionString, cod_cliente))
            {
                return false;
            }


            return true;
        }



        public async Task<bool> AsignarGrupoComercial_Cliente_Nuevo(string userConnectionString, string codcliente, string codcliente_casa_matriz, int codvendedor)
        {
            int almacen = await get_almacen_de_vendedor(userConnectionString, codvendedor);
            bool creacion = await ClientesIguales_Insertar(userConnectionString, codcliente_casa_matriz, codcliente, almacen);
            if (!creacion)
            {
                return false;
            }
            return true;
        }



        public async Task<bool> ClientesIguales_Insertar(string userConnectionString, string codcliente_a, string codcliente_b, int almacen)
        {
            veclientesiguales veclientesiguales = new veclientesiguales();
            veclientesiguales.codcliente_a = codcliente_a;
            veclientesiguales.codcliente_b = codcliente_b;
            veclientesiguales.codalmacen = almacen;

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                _context.veclientesiguales.Add(veclientesiguales);
                try
                {
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        public async Task<int> get_almacen_de_vendedor(string userConnectionString, int codvendedor)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var query = await _context.vevendedor
                        .Where(v => v.codigo == codvendedor)
                            .Select(v => new
                            {
                                almacen = v.almacen
                            })
                        .FirstOrDefaultAsync();
                if (query == null)
                {
                    return 0;
                }
                return query.almacen;
            }
        }

        public async Task<bool> AsignarCuentasCliente(string userConnectionString, string codclienteAct)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var codcliente = await _context.vecliente.OrderBy(v => v.codigo).Select(v => v.codigo).FirstOrDefaultAsync();

                var vecliente_conta = await _context.vecliente_conta
                    .Where(vc => vc.codcliente == codcliente)
                    .Select(vc => new vecliente_conta
                    {
                        codcliente = codclienteAct,
                        codunidad = vc.codunidad,
                        cta_vtacontado = vc.cta_vtacontado,
                        cta_vtacontado_aux = vc.cta_vtacontado_aux,
                        cta_vtacontado_cc = vc.cta_vtacontado_cc,
                        cta_vtacredito = vc.cta_vtacredito,
                        cta_vtacredito_aux = vc.cta_vtacredito_aux,
                        cta_vtacredito_cc = vc.cta_vtacredito_cc,
                        cta_porcobrar = vc.cta_porcobrar,
                        cta_porcobrar_aux = vc.cta_porcobrar_aux,
                        cta_porcobrar_cc = vc.cta_porcobrar_cc,
                        cta_iva = vc.cta_iva,
                        cta_iva_aux = vc.cta_iva_aux,
                        cta_iva_cc = vc.cta_iva_cc,
                        codalmacen = vc.codalmacen,
                        cta_ivadebito = vc.cta_ivadebito,
                        cta_ivadebito_aux = vc.cta_ivadebito_aux,
                        cta_ivadebito_cc = vc.cta_ivadebito_cc,
                        cta_anticipo = vc.cta_anticipo,
                        cta_anticipo_aux = vc.cta_anticipo_aux,
                        cta_anticipo_cc = vc.cta_anticipo_cc,
                        cta_anticipo_contado = vc.cta_anticipo_contado,
                        cta_anticipo_contado_aux = vc.cta_anticipo_contado_aux,
                        cta_anticipo_contado_cc = vc.cta_anticipo_contado_cc,
                        cta_diftdc = vc.cta_diftdc,
                        cta_diftdc_aux = vc.cta_diftdc_aux,
                        cta_diftdc_cc = vc.cta_diftdc_cc
                    })
                    .FirstOrDefaultAsync();

                if (vecliente_conta != null)
                {
                    _context.vecliente_conta.Add(vecliente_conta);
                    try
                    {
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    
                }
                return false;
            }

        }

        public async Task<float> Porcentaje_Limite_Descuento_Deposito(string userConnectionString, decimal subtotal_prof)
        {
            float resultado = 0;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var query = await _context.verango_descuento_deposito
                        .Where(descuento => subtotal_prof >= descuento.desde && subtotal_prof <= descuento.hasta)
                        .FirstOrDefaultAsync();
                if (query == null)
                {
                    return 0;
                }
                if (query.porcentaje_limite != null)
                {
                    resultado = (float)query.porcentaje_limite;
                }
                return resultado;
            }
        }


        public async Task<vetienda> getDataClienteCasualTienda(string userConnectionString, string cod_cliente, int codalmacen,
            string nomcliente_casual, string email_cliente_casual, string celular_cliente_casual, DateTime fechareg, string horareg, string usuarioreg)
        {
            vetienda vetienda = new vetienda();
            vetienda.codcliente = cod_cliente;

            vetienda.direccion = await getdireccionalmacen(userConnectionString, codalmacen);
            vetienda.telefono = "";
            vetienda.nomb_telf1 = "";
            vetienda.fax = "";
            vetienda.celular = celular_cliente_casual;
            vetienda.nomb_cel1 = nomcliente_casual;
            vetienda.email = email_cliente_casual;
            vetienda.codptoventa = await getCodArea(userConnectionString, codalmacen);
            vetienda.obs = "";

            vetienda.fechareg = fechareg;
            vetienda.horareg = horareg;
            vetienda.usuarioreg = usuarioreg;

            //crear la direcion del cliente con la direccion de la tienda donde se esta creando al cliente (por sugerencia de JRA)
            vetienda.aclaracion_direccion = "CASUAL";
            vetienda.latitud = await getlatitudalmacen(userConnectionString, codalmacen);
            vetienda.longitud = await getlongitudalmacen(userConnectionString, codalmacen);

            vetienda.telefono_2 = "";
            vetienda.nomb_telf2 = "";
            vetienda.celular_2 = "";
            vetienda.nomb_cel2 = "";
            vetienda.celular_3 = "";
            vetienda.nomb_cel3 = "";
            vetienda.celular_whatsapp = celular_cliente_casual;
            vetienda.nomb_whatsapp = nomcliente_casual;

            return vetienda;

        }


        public async Task<string> getdireccionalmacen(string userConnectionString, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inalmacen
                    .Where(v => v.codigo == codalmacen)
                    .Select(parametro => new
                    {
                        direccion = parametro.direccion
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return "";
                }
                return result.direccion;
            }
        }

        public async Task<string> getlatitudalmacen(string userConnectionString, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inalmacen
                    .Where(v => v.codigo == codalmacen)
                    .Select(parametro => new
                    {
                        latitud = parametro.latitud
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return "0";
                }
                return result.latitud;
            }
        }

        public async Task<string> getlongitudalmacen(string userConnectionString, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inalmacen
                    .Where(v => v.codigo == codalmacen)
                    .Select(parametro => new
                    {
                        longitud = parametro.longitud
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return "0";
                }
                return result.longitud;
            }
        }
        // este codigo en el sia antiguo era select p2.codigo as codarea,p2.descripcion,p1. * from inalmacen p1, adarea p2 where p1.codigo='311' and p1.codarea=p2.codigo 
        // solo utilizado para tener el codigo de area de la agencia, se simplifico a select codarea from inalmacen where codigo='311'
        public async Task<int> getCodArea(string userConnectionString, int codalmacen)
        {
            int codpto_vta = 0;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inalmacen
                    .Where(v => v.codigo == codalmacen)
                    .Select(parametro => new
                    {
                        codarea = parametro.codarea
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    codpto_vta = result.codarea;
                }
                return codpto_vta;
            }
        }


        //devuelve nuevo codigo para cliente
        public async Task<string> Ultimo_Codigo_Numerico(string userConnectionString)
        {
            int inicial = await getemp_numeracion_clientes_desde(userConnectionString);
            int final = await getemp_numeracion_clientes_hasta(userConnectionString);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.vecliente
                    .Where(c => c.codigo.CompareTo(inicial) >= 0 && c.codigo.CompareTo(final) <= 0
                                && c.codigo.All(char.IsDigit)
                                && !c.codigo.Contains("E"))
                    .OrderBy(c => c.codigo)
                    .Select(c => c.codigo)
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    result = "0";
                }
                return result;
            }
        }

        public async Task<int> getemp_numeracion_clientes_desde(string userConnectionString)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adparametros
                    .Select(parametro => parametro.numeracion_clientes_desde)
                    .FirstOrDefaultAsync();
                
                return (int)result;
            }
        }
        public async Task<int> getemp_numeracion_clientes_hasta(string userConnectionString)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.adparametros
                    .Select(parametro => parametro.numeracion_clientes_hasta)
                    .FirstOrDefaultAsync();

                return (int)result;
            }
        }
        public async Task<vecliente> getDataClienteCasual(string userConnectionString, string codSN, string cod_cliente, string nomcliente_casual, string nit_cliente_casual, string email_cliente_casual, string usuarioreg,
            string celular_cliente_casual, float limite_descto_deposito)
        {
            string fechaDef = "1900-01-01";
            string fechaAc = getFechaActual();
            string horaAc = getHoraActual();

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var dt_SN = await _context.vecliente
                    .Where(c => c.codigo == codSN && _context.vecliente_sinnombre.Any(v => v.codcliente == c.codigo))
                    .FirstOrDefaultAsync();

                if(dt_SN == null)
                {
                    return null;
                }
                dt_SN.codigo = cod_cliente;
                dt_SN.razonsocial = nomcliente_casual;
                dt_SN.nit = nit_cliente_casual;
                dt_SN.credito = 0;

                dt_SN.fvenccred = DateTime.Parse(fechaDef);  //enviar fecha

                dt_SN.creditodisp = 0;

                dt_SN.fapertura = DateTime.Parse(fechaAc); // enviar fecha actual

                dt_SN.email = email_cliente_casual;



                dt_SN.horareg = horaAc;  //enviar string la hora

                dt_SN.fechareg = DateTime.Parse(fechaAc); // enviar fecha actual

                dt_SN.usuarioreg = usuarioreg;
                dt_SN.clasificacion = "Z";
                dt_SN.controla_empaque_cerrado = true;
                dt_SN.controla_precios = true;
                dt_SN.permite_items_repetidos = false;
                dt_SN.controla_empaque_minimo = true;
                dt_SN.controla_monto_minimo = true;
                dt_SN.es_cliente_final = false;
                dt_SN.nombre_contrato = "";
                dt_SN.nombre_fact = nomcliente_casual;
                dt_SN.nit_fact = nit_cliente_casual;
                dt_SN.nombre_comercial = nomcliente_casual;
                dt_SN.es_empresa = false;
                dt_SN.nropoder = "0";
                dt_SN.ciudad_titular = "--";
                dt_SN.direccion_titular = "-";
                dt_SN.celular_titular = celular_cliente_casual;
                dt_SN.latitud_titular = "0";
                dt_SN.longitud_titular = "0";
                dt_SN.obs_titular = "--";
                dt_SN.cliente_pertec = false;
                dt_SN.porcentaje_limite_descto_deposito = (decimal?)limite_descto_deposito;
                dt_SN.casual = true;

                return dt_SN;
            }
        }


        public string getFechaActual()
        {
            DateTime fechaActualLocal = DateTime.Now;
            string fechaFormateada = fechaActualLocal.ToString("yyyy-MM-dd");
            return fechaFormateada;
        }

        public string getHoraActual()
        {
            DateTime horaActual = DateTime.Now;
            int hora = horaActual.Hour; // Obtiene la hora actual en formato de 24 horas
            int minutos = horaActual.Minute; // Obtiene los minutos actuales
            string horaAct = hora.ToString() + ":" + minutos.ToString();
            return horaAct;
        }
    }
}
