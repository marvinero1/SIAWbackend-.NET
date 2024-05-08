using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_funciones;
using NuGet.Configuration;
using System.Collections;
using System.Reflection;
using static siaw_funciones.Validar_Vta;
using System.Globalization;
using MessagePack;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
//using static siaw_funciones.Validar_Vta;

namespace siaw_funciones
{
    public class Validar_Vta
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
        string Sql = "";
        public enum Acciones_Validar
        {

            Ninguna = 0,

            Pedir_Servicio = 1,

            Confirmar_SiNo = 2,

            Solo_Ok = 3,
        }
        public class ResultadoValidacion
        {
            public bool resultado { get; set; }
            public string observacion { get; set; } = "";
            public string obsdetalle { get; set; } = "";
            public string datoA { get; set; } = "";
            public string datoB { get; set; } = "";
            public Acciones_Validar accion { get; set; }
        }

        public void EliminarControlValidado(string codControl, DataTable dtValidar)
        {
            // Esta función se utiliza para poner el campo "valido" como "NO" para cualquier control
            // que ya ha sido previamente validado y por alguna modificación en la proforma,
            // su validación como "SI" se ve alterada y es necesario volver a validar.

            foreach (DataRow row in dtValidar.Rows)
            {
                if (row["codcontrol"].ToString() == codControl)
                {
                    row["valido"] = "NO";
                }
            }
        }
        private ResultadoValidacion InicializarResultado(ResultadoValidacion objres)
        {
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            return objres;
        }

        public class JsonResultadoValidacion
        {
            public string codservicio { get; set; } = "";
            public string valido { get; set; } = "";
            public string observacion { get; set; } = "";
            public string obsdetalle { get; set; } = "";
            public string descservicio { get; set; } = "";
            public string dataA { get; set; } = "";
            public string dataB { get; set; } = "";
            public string clave_servicio { get; set; } = "";
            public string accion { get; set; } = "";
        }
        public class Dtnocumplen
        {
            public string codigo { get; set; }
            public string descripcion { get; set; }
            public decimal cantidad { get; set; }
            public decimal cantidad_pf_anterior { get; set; }
            public decimal cantidad_pf_total { get; set; }
            public decimal porcen_venta { get; set; }
            public int coddescuento { get; set; }
            public int codtarifa { get; set; }
            public decimal saldo { get; set; }
            public decimal porcen_maximo { get; set; }
            public decimal porcen_mercaderia { get; set; }
            public decimal cantidad_permitida_seg_porcen { get; set; }
            public int empaque_precio { get; set; }
            public string obs { get; set; }
        }
        public class Dtnegativos
        {
            public string kit { get; set; }
            public int nro_partes { get; set; }
            public string coditem_cjto { get; set; }
            public string coditem_suelto { get; set; }
            public string codigo { get; set; }
            public string descitem { get; set; }
            public decimal cantidad { get; set; }
            public decimal cantidad_conjunto { get; set; }
            public decimal cantidad_suelta { get; set; }
            public decimal saldo_descontando_reservas { get; set; }
            public decimal saldo_sin_descontar_reservas { get; set; }
            public decimal cantidad_reservada_para_cjtos { get; set; }
            public string obs { get; set; }
        }
        public class Dt_desglosado
        {
            public string kit { get; set; }
            public int nro_partes { get; set; }
            public string coditem_cjto { get; set; }
            public string coditem_suelto { get; set; }
            public string codigo { get; set; }
            public double cantidad { get; set; }
            public double cantidad_conjunto { get; set; }
            public double cantidad_suelta { get; set; }
        }

        public class Controles
        {
            public int Codigo { get; set; }
            public int Orden { get; set; }
            public string CodControl { get; set; }
            public bool? Grabar { get; set; }
            public string GrabarAprobar { get; set; }
            public bool? HabilitadoPf { get; set; }
            public bool? HabilitadoNr { get; set; }
            public bool? HabilitadoFc { get; set; }
            public string Descripcion { get; set; }
            public string? CodServicio { get; set; }
            // ... otras propiedades ...
            public string NroItems { get; set; }
            public string Descuentos { get; set; }
            public string Recargos { get; set; }
            public string Nit { get; set; }
            public string Subtotal { get; set; }
            public double Total { get; set; }
            public string Preparacion { get; set; }
            // ... otras propiedades ...
            public string DescGrabar { get; set; }
            public string DescGrabarAprobar { get; set; }
            public string Valido { get; set; }
            public string Observacion { get; set; }
            public string ObsDetalle { get; set; }
            public string DescServicio { get; set; }
            public string DatoA { get; set; }
            public string DatoB { get; set; }
            public string ClaveServicio { get; set; }
            public string Accion { get; set; }
            // Nuevo atributo de la clase Dtnegativos
            public List<Dtnegativos> Dtnegativos { get; set; }
            public List<Dtnocumplen> Dtnocumplen { get; set; }
        }
        public class intarifaMonMinMay
        {
            public int codtarifa { get; set; }
            public decimal montomin { get; set; }
            public string moneda { get; set; }
        }
        private Nombres nombres = new Nombres();
        private Cliente cliente = new Cliente();
        private Configuracion configuracion = new Configuracion();
        private Ventas ventas = new Ventas();
        private Items items = new Items();
        private Cobranzas cobranzas = new Cobranzas();
        private Creditos creditos = new Creditos();
        private Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        private TipoCambio tipocambio = new TipoCambio();
        private Almacen almacen = new Almacen();
        private Funciones funciones = new Funciones();
        private Restricciones restricciones = new Restricciones();
        private Empresa empresa = new Empresa();
        private Saldos saldos = new Saldos();
        private HardCoded hardcoded = new HardCoded();
        private Seguridad seguridad = new Seguridad();
        //Task<ActionResult<itemDataMatriz>>
        //Task<Controles>
        public async Task<List<Controles>> DocumentoValido(string userConnectionString, string cadena_control, string tipodoc, string opcion_validar, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos,string codempresa, string usuario)
        {
            List<Controles> resultados = new List<Controles>();
            try
            {
                
                List<Controles> controles_final = new List<Controles>();
                string validando = "";
                if (cadena_control == "vacio") { cadena_control = ""; }

                //var _context = DbContextFactory.Create(userConnectionString);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vetipo_control_vtas.AsQueryable();
                    if (tipodoc == "proforma")
                    {
                        if (opcion_validar == "grabar")
                        {
                            query = query.Where(vc => vc.habilitado_pf == true && vc.grabar == true);
                        }
                        else if (opcion_validar == "grabar_aprobar")
                        {
                            query = query.Where(vc => vc.habilitado_pf == true && vc.grabar_aprobar == "1");
                        }
                        else if (opcion_validar == "validar")
                        {
                            query = query.Where(vc => vc.habilitado_pf == true);
                        }
                    }
                    else if (tipodoc == "remision")
                    {
                        query = query.Where(vc => vc.habilitado_nr == true);
                    }
                    else if (tipodoc == "factura")
                    {
                        query = query.Where(vc => vc.habilitado_fc == true);
                    }
                    else
                    {
                        query = query.Where(vc => vc.habilitado_pf == true);
                    }

                    if (cadena_control.Length > 0)
                    {
                        var controlesEspecificos = cadena_control.Split(',').ToList();
                        query = query.Where(vc => controlesEspecificos.Contains(vc.codcontrol));
                    }

                    var controles = query.OrderBy(vc => vc.orden)
                                     .Select(vc => new Controles
                                     {
                                         Codigo = vc.codigo,
                                         Orden = (int)vc.orden,
                                         CodControl = vc.codcontrol,
                                         Grabar = vc.grabar,
                                         GrabarAprobar = vc.grabar_aprobar,
                                         HabilitadoPf = vc.habilitado_pf,
                                         HabilitadoNr = vc.habilitado_nr,
                                         HabilitadoFc = vc.habilitado_fc,
                                         Descripcion = vc.descripcion,
                                         CodServicio = vc.codservicio,
                                         // ... otras propiedades ...
                                         NroItems = DVTA.nroitems.ToString(),
                                         Descuentos = DVTA.totdesctos_extras.ToString(),
                                         Recargos = DVTA.totrecargos.ToString(),
                                         Nit = DVTA.nitfactura,
                                         Subtotal = DVTA.subtotaldoc.ToString(),
                                         Total = DVTA.totaldoc,
                                         Preparacion = DVTA.preparacion,
                                         // ... otras propiedades ...
                                         DescGrabar = "",
                                         DescGrabarAprobar = "",
                                         Valido = "",
                                         Observacion = "",
                                         ObsDetalle = "",
                                         DescServicio = "",
                                         DatoA = "",
                                         DatoB = "",
                                         ClaveServicio = "",
                                         Accion = "",
                                         Dtnegativos = new List<Dtnegativos>(),
                                         Dtnocumplen = new List<Dtnocumplen>()

                                     })
                                     .ToList();
                    controles_final = controles;
                    foreach (var control in controles_final)
                    {
                        control.CodServicio = control.CodServicio?.Trim();
                        validando = control.CodControl + "-" + control.Descripcion;
                        control.Valido = "SI";
                        control.Observacion = "Sin Observacion";
                        control.ObsDetalle = "";
                        if (control.CodServicio?.Trim().Length > 0)
                        {
                            control.DescServicio = await nombres.NombreConOpcionDeCodigoAsync(userConnectionString, "adservicio", control.CodServicio, "", "");
                        }
                        control.DatoA = "";
                        control.DatoB = "";
                        control.ClaveServicio = "";
                        control.Accion = Acciones_Validar.Ninguna.ToString();
                        //Dtnocumplen dtnocumplen = new Dtnocumplen();
                        List<Dtnocumplen> dtnocumplen = new List<Dtnocumplen>();
                        List<Dtnegativos> dtnegativos = new List<Dtnegativos>();
                        control.Dtnocumplen = dtnocumplen;
                        control.Dtnegativos = dtnegativos;
                        _ = await Control_Valido(_context, control, DVTA, tabladetalle, tabladescuentos, dt_etiqueta, dt_anticipo_pf, tablarecargos, dtnocumplen, dtnegativos, codempresa, usuario);
                        //Control_Valido(userConnectionString, control, DVTA, tabladetalle, tabladescuentos, dt_etiqueta, dt_anticipo_pf, tablarecargos, dtnocumplen, dtnegativos, codempresa);
                    }
                    resultados = controles_final;
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return resultados;
        }
        public async Task<bool> Control_Valido(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos, List<Dtnocumplen> dtnocumplen, List<Dtnegativos> dtnegativos,string codempresa, string usuario)
            //public void Control_Valido(string userConnectionString, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos, Dtnocumplen dtnocumplenm, Dtnegativos dtnegativos, string codempresa)
        {
            string _codcontrol = regcontrol.CodControl;
            string _desccontrol = regcontrol.Descripcion;
            switch (_codcontrol)
            {
                case "00001":
                    //**ES CLIENTE COMPETENCIA (NIT)
                    _ = await Control_Valido_C00001Async(_context, regcontrol, DVTA);
                    break;
                case "00002":
                    //**VENTA CON VB PRESIDENCIA
                    _ = await Control_Valido_C00002Async(_context, regcontrol, DVTA);
                    break;
                case "00003":
                    //**CLIENTE COMPETENCIA PERMITIR DESCSTOS DE LINEA
                    _ = await Control_Valido_C00003Async(_context, regcontrol, DVTA, tabladetalle);
                    break;
                case "00004":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTO PROVEEDOR
                    _ = await Control_Valido_C00004Async(_context, regcontrol, DVTA, tabladetalle);
                    break;
                case "00005":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTO VOLUMEN
                    _ = await Control_Valido_C00005Async(_context, regcontrol, DVTA, tabladetalle);
                    break;
                case "00006":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTOS PROMOCION
                    _ = await Control_Valido_C00006Async(_context, regcontrol, DVTA, tabladescuentos,codempresa);
                    break;
                case "00007":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTOS EXTRAS
                    _ = await Control_Valido_C00007Async(_context, regcontrol, DVTA, tabladescuentos);
                    break;
                case "00008":
                    //VERIFICAR MONTO IGUAL A CERO DE PROFORMA
                    _ =  Control_Valido_C00008(_context, regcontrol, DVTA);
                    break;
                case "00009":
                    //VERIFICAR DESCUENTO DE NIVEL SEGÚN SOLICITUD
                    _ = await Control_Valido_C00009Async(_context, regcontrol, DVTA);
                    break;
                case "00010":
                    //VERIFICAR SI VTA ES CON DESCUENTO DE NIVEL SEGÚN SOLICITUD Y COD CLIENTE SN
                    _ = await Control_Valido_C00010Async(_context, regcontrol, DVTA);
                    break;
                case "00011":
                    //si el cliente de la proforma es un CASUAL  o un SN debe pedir clave
                    _ = await Control_Valido_C00011Async(_context, regcontrol, DVTA);
                    break;
                case "00012":
                    //12	VERIFICAR TIPO DE PREPARACION DE CLIENTE FINAL
                    _ = await Control_Valido_C00012Async(_context, regcontrol, DVTA);
                    break;
                case "00013":
                    // VERIFICA VENTA CON CODIGO DE CLIENTE SN Y CLIENTE REAL QUE NO ES SN y CASUAL
                    _ = await Control_Valido_C00013Async(_context, regcontrol, DVTA);
                    break;
                case "00014":
                    //VERIFICAR SI LOS DESCUENTOS EXTRAS ESTAN VIGENTES
                    _ = await Control_Valido_C00014Async(_context, regcontrol,tabladescuentos);
                    break;
                case "00015":
                    //VERIFICAR FECHA VIGENCIA DESCTOS. EXTRAS VERSUS FECHA INICIAL PROFORMA
                    _ = await Control_Valido_C00015Async(_context, regcontrol, DVTA, tabladescuentos);
                    break;
                case "00016":
                    //VERIFICAR SI HAY PROMOCIONES PENDIENTES DE APLICAR
                    _ = await Control_Valido_C00016Async(_context, regcontrol, DVTA, tabladescuentos, tabladetalle, codempresa);
                    break;
                case "00017":
                    //VALIDAR DESCUENTO POR DEPOSITO (CBZAS VALIDAS)
                    // Control_Valido_C00017(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00017Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00018":
                    //VALIDAR DESCUENTO EXTRA REQUIERE CREDITO VALIDO
                    //  Control_Valido_C00018(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00018Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00019":
                    //VALIDAR DESCUENTO EXTRA SEGÚN TIPO DE VENTA (CONTADO CE, CONTADO CA, CREDITO)
                    //  Control_Valido_C00019(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00019Async(_context, regcontrol, DVTA, tabladescuentos,dt_anticipo_pf, codempresa);
                    break;
                case "00020":
                    //VALIDAR MONTO MINIMO SEGÚN LISTA DE PRECIOS
                    // Control_Valido_C00020(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00020Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00021":
                    //VALIDAR MONTO MINIMO PARA ENTREGAR EL PEDIDO AL CLIENTE
                    //  Control_Valido_C00021(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00021Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00022":
                    //VALIDAR MONTO MINIMO PARA APLICAR DESCUENTOS ESPECIALES
                    // Control_Valido_C00022(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00022Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00023":
                    //VALIDAR CUMPLIMIENTO DE EMPAQUE MINIMO SEGUN LISTA DE PRECIOS
                    // Control_Valido_C00023(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00023Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00024":
                    //VALIDAR CREDITO DISPONIBLE
                    // Control_Valido_C00024(regcontrol, DVTA);
                    _ = await Control_Valido_C00024Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00025":
                    //VALIDAR ETIQUETA DE LA PROFORMA
                    //  Control_Valido_C00025(regcontrol, DVTA, dt_etiqueta);
                    _ = await Control_Valido_C00025Async(_context, regcontrol, DVTA, dt_etiqueta, codempresa);
                    break;
                case "00026":
                    //VALIDAR TIPO DE PAGO PERMITIDO AL CLIENTE
                    //  Control_Valido_C00026(regcontrol, DVTA, dt_etiqueta);
                    _ = await Control_Valido_C00026Async(_context, regcontrol, DVTA, dt_etiqueta, codempresa);
                    break;
                case "00027":
                    //VERIFICAR SI EL CLIENTE TIENE NOTAS DE REMISION PENDIENTES DE PAGO
                    // Control_Valido_C00027(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00027Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00028":
                    //VERIFICAR QUE EL CLIENTE Y CLIENTE REAL SEA ATENDIDO POR EL VENDEDOR
                    // Control_Valido_C00028(regcontrol, DVTA);
                    _ = await Control_Valido_C00028Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00029":
                    //VERIFICAR QUE EL PERIODO ESTE ABIERTO
                    // Control_Valido_C00029(regcontrol, DVTA);
                    _ = await Control_Valido_C00029Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00030":
                    //VERIFICAR NIT VALIDO
                    //  Control_Valido_C00030(regcontrol, DVTA);
                    _ = await Control_Valido_C00030Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00031":
                    //VERIFICAR VENTA A CREDITO NIT NO PUEDE SER CERO
                    // Control_Valido_C00031(regcontrol, DVTA);
                    _ = await Control_Valido_C00031Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00032":
                    //VERIFICAR MONTO LIMITE FACTURA SIN NOMBRE
                    // Control_Valido_C00032(regcontrol, DVTA);
                    _ = await Control_Valido_C00032Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00033":
                    //VERIFICAR DESCUENTOS EXTRAS APLICADOS
                    //  Control_Valido_C00033(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00033Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00034":
                    //VALIDAR EL TIPO DE TRANSPORTE ELEGIDO PARA ENTREGA DE MERCADERIA
                    // Control_Valido_C00034(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00034Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00035":
                    //VERIFICAR NOMBRE DE TRANSPORTE VALIDO
                    //  Control_Valido_C00035(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00035Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00036":
                    //VALIDAR QUIEN DEBE CANCELAR EL FLETE(CLIENTE O PERTEC)
                    //  Control_Valido_C00036(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00036Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00037":
                    //VALIDAR OTRO MEDIO DE TRANSPORTE
                    //  Control_Valido_C00037(regcontrol, DVTA);
                    _ = await Control_Valido_C00037Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00038":
                    //VERIFICAR ANTICIPO ASIGNADO EN VENTA AL CONTADO
                    // Control_Valido_C00038(regcontrol, DVTA, dt_anticipo_pf);
                    _ = await Control_Valido_C00038Async(_context, regcontrol, DVTA, dt_anticipo_pf, codempresa);
                    break;
                case "00039":
                    //VALIDAR VENTA CONTADO CONTRA ENTREGA CLIENTE DEL INTERIOR
                    // Control_Valido_C00039(regcontrol, DVTA);
                    _ = await Control_Valido_C00039Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00040":
                    //VALIDAR SI ES VENTA CONTADO CONTRA ENTREGA NO TENGA ANTICIPO
                    //  Control_Valido_C00040(regcontrol, DVTA);
                    _ = await Control_Valido_C00040Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00041":
                    //VALIDAR DIRECCION DE ENTREGA (CLIENTE CON VARIOS PTOS. DE VENTA)
                    //  Control_Valido_C00041(regcontrol, DVTA);
                    _ = await Control_Valido_C00041Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00042":
                    //VERIFICAR DESCUENTOS EXTRAS REPETIDOS
                    //  control_valido_c00042(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00042Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00043":
                    //VALIDAR GEO-REFERENCIA DE ENTREGA VALIDA
                    // control_valido_c00043(regcontrol, DVTA);
                    _ = await Control_Valido_C00043Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00044":
                    //VERIFICAR CLIENTE TIENE CUENTAS POR PAGAR EN MORA
                    // control_valido_c00044(regcontrol, DVTA);
                    _ = await Control_Valido_C00044Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00045":
                    //VERIFICAR QUE EL DESCUENTO DE PROMOCION NO SOBRE PASE EL DISPONIBLE
                    // control_valido_c00045(regcontrol, DVTA);
                    _ = await Control_Valido_C00045Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00046":
                    //VALIDAR PRECIO DE VENTA PERMITIDO AL CLIENTE
                    // control_valido_c00046(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00046Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00047":
                    //VALIDAR QUE LA CANTIDAD DE VENTA DEL ITEM AL CLIENTE NO SEA MAYOR AL PERMITIDO EN LOS DIAS LIMITE
                    // control_valido_c00047(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00047Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00048":
                    //VALIDAR VENTA A CLIENTE SIN CUENTA EN OFICINA
                    // control_valido_c00048(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00048Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00049":
                    //VALIDAR VENTA A CLIENTE CON CUENTA EN OFICINA
                    // control_valido_c00049(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00049Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00050":
                    //VALIDAR PEDIDO URGENTE
                    // Control_Valido_C00050(regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00050Async(_context, regcontrol, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00051":
                    //VALIDAR MONTO MINIMO REQUERIDO PARA LOS DESCUENTOS EXTRAS
                    //  Control_Valido_C00051(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00051Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00052":
                    //VALIDAR PESO MINIMO REQUERIDO PARA APLICAR DESCUENTOS EXTRAS
                    // Control_Valido_C00052(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00052Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00053":
                    //VALIDAR MONTO PARA BANCARIZACION
                    // Control_Valido_C00053(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00053Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00054":
                    //VALIDAR VENTA A CLIENTE QUE SE VENDE SOLO CONTRA ENTREGA
                    //  Control_Valido_C00054(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00054Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00055":
                    //VALIDAR QUE NO SE VENDA A CREDITO A CLIENTE SIN NOMBRE
                    //  Control_Valido_C00055(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00055Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00056":
                    //VALIDAR DIRECCION DE LA EQIQUETA CORRECTA
                    //  Control_Valido_C00056(regcontrol, DVTA, dt_etiqueta);
                    _ = await Control_Valido_C00056Async(_context, regcontrol, DVTA, dt_etiqueta, codempresa);
                    break;
                case "00057":
                    //VALIDAR QUE NO SE OTORGUE DESCTO PRONTO PAGO NORMAL A VENTA CONTRA ENTREGA Y VICEVERSA
                    //  Control_Valido_C00057(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00057Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00058":
                    //VALIDAR LIMITE MAXIMO DE VENTA DEFINIDO EN PORCENTAJE
                    // Control_Valido_C00058(regcontrol, DVTA, tabladetalle, dtnocumplen, dgvmaximos_vta);
                    _ = await Control_Valido_C00058Async(_context, regcontrol, DVTA, tabladetalle, dtnocumplen, codempresa, usuario);
                    break;
                case "00059":
                    //PERMITIR DESCUENTOS DE NIVEL ANTERIORES
                    // Control_Valido_C00059(regcontrol, DVTA);
                    _ = await Control_Valido_C00059Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00060":
                    //VALIDAR SALDOS NEGATIVOS
                    //  Control_Valido_C00060(regcontrol, DVTA, tabladetalle, dtnegativos, dgvnegativos);
                    _ = await Control_Valido_C00060Async(_context, regcontrol, DVTA, tabladetalle, dtnegativos, codempresa, usuario);
                    break;
                case "00061":
                    //VALIDAR NRO. DE PEDIDOS URGENTES DEL CLIENTE POR DIA
                    //  Control_Valido_C00061(true, regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00061Async(_context, regcontrol,true, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00062":
                    //VALIDAR NRO. DE PEDIDOS URGENTES DEL CLIENTE POR SEMANA
                    //  Control_Valido_C00061(false, regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00061Async(_context, regcontrol, false, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00063":
                    //VALIDAR MONTO MINIMO PEDIDO URGENTE A PROVINCIA
                    // Control_Valido_C00063(false, regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00063Async(_context, regcontrol, false, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00064":
                    // Control_Valido_C00064(regcontrol, DVTA, dt_anticipo_pf);
                    break;
                case "00065":
                    //Control_Valido_C00065(regcontrol, DVTA, tabladetalle);
                    break;
                case "00066":
                    //VALIDAR ITEMS REPETIDOS
                    // Control_Valido_C00066(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00066Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00067":
                    //VALIDAR EMPAQUES CERRADOS SEGUN LISTA DE PRECIOS
                    //Control_Valido_C00067(regcontrol, DVTA, tabladetalle);
                    
                    break;
                case "00068":
                    // Control_Valido_C00068(regcontrol, tabladetalle);
                    break;
                case "00069":
                    //Control_Valido_C00069(regcontrol, tabladetalle);
                    break;
                case "00070":
                    // Control_Valido_C00070(regcontrol, tabladetalle, tabladescuentos);
                    break;
                case "00071":
                    // Control_Valido_C00071(regcontrol, DVTA, tabladetalle);
                    break;
                case "00072":
                    //Control_Valido_C00071(regcontrol, DVTA, tabladetalle);
                    break;
                case "00073":
                    //  Control_Valido_C00073(regcontrol, DVTA, tabladetalle);
                    break;
                case "00074":
                    //  Control_Valido_C00074(regcontrol, DVTA);
                    break;
                case "00075":
                    //  Control_Valido_C00075(regcontrol, DVTA, tabladetalle);
                    break;
                case "00076":
                    // Control_Valido_C00076(regcontrol, DVTA);
                    break;
                case "00077":
                    // Control_Valido_C00077(regcontrol, DVTA);
                    break;
                case "00078":
                    //  Control_Valido_C00078(regcontrol, DVTA);
                    break;
                case "00079":
                    // Control_Valido_C00079(regcontrol, DVTA);
                    break;
                case "00080":
                    //  Control_Valido_C00080(regcontrol, DVTA);
                    break;
                case "00081":
                    //  Control_Valido_C00081(regcontrol, DVTA, tabladetalle);
                    break;
                case "00082":
                    // Control_Valido_C00082(regcontrol, DVTA, tabladetalle);
                    break;
                case "00083":
                    // Control_Valido_C00083(regcontrol, DVTA, tabladetalle);
                    break;
                case "00084":
                    //control_valido_c00084(regcontrol, DVTA, tabladetalle);
                    break;
                case "00085":
                    // control_valido_c00085(regcontrol, DVTA, tabladetalle);
                    break;
                case "00086":
                    //control_valido_c00086(regcontrol, tabladetalle);
                    break;
                case "00087":
                    // Control_Valido_c00087(regcontrol, tabladetalle);
                    break;
                case "00088":
                    // Control_Valido_C00088(regcontrol, DVTA, tabladetalle);
                    break;
                case "00089":
                    //Control_Valido_C00089(regcontrol, DVTA, tabladetalle);
                    break;
                case "00090":
                    //Control_Valido_C00090(regcontrol, DVTA, tabladetalle);
                    break;
                case "00091":
                    // Control_Valido_C00091(regcontrol, DVTA, tabladetalle);
                    break;
                case "00092":
                    // Control_Valido_C00092(regcontrol, DVTA, tabladetalle);
                    break;
                case "00093":
                    //Control_Valido_C00093(regcontrol, DVTA, tabladetalle);
                    break;
                case "00094":
                    //Control_Valido_C00094(regcontrol, DVTA, tabladescuentos, tabladetalle);
                    break;
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00001Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            // //PERMITIR GRABAR PARA APROBAR PROFORMA CLIENTE COMPETENCIA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
           objres = await Validar_NIT_Es_Cliente_CompetenciaAsync(_context, DVTA);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion= objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00002Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            // '//VENTA CON VB PRESIDENCIA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Tipo_ClienteAsync(_context, DVTA.codcliente_real);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00003Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //CLIENTE COMPETENCIA PERMITIR DESCSTOS DE LINEA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Competencia_Permite_Desctos_De_LineaAsync(_context, DVTA, tabladetalle);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
               // regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00004Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //CLIENTE COMPETENCIA PERMITIR DESCTO PROVEEDOR
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Competencia_Permite_Desctos_ProveedorAsync(_context, DVTA, tabladetalle);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00005Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //CLIENTE COMPETENCIA PERMITIR DESCTO VOLUMEN
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Competencia_Permite_Desctos_VolumnenAsync(_context, DVTA, tabladetalle);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
               // regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00006Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //**CLIENTE COMPETENCIA PERMITIR DESCTOS PROMOCION
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Competencia_Permite_Desctos_PromocionAsync(_context, DVTA, tabladescuentos,codempresa);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00007Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            //**CLIENTE COMPETENCIA PERMITIR DESCTOS EXTRAS
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Competencia_Permite_Desctos_ExtraAsync(_context, DVTA, tabladescuentos);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private bool Control_Valido_C00008(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //**VERIFICAR MONTO IGUAL A CERO DE PROFORMA
            if ((Math.Round(DVTA.totaldoc, 2) < 0) || (Math.Round(DVTA.totaldoc, 2) == 0))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = "El monto total final de la proforma es igual a cero, lo cual no esta permitido, verificar esta situacion!!!";
                regcontrol.ObsDetalle = "";
                regcontrol.DatoA = "";
                regcontrol.DatoB = "";
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = Acciones_Validar.Ninguna.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00009Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //**VENTA CON DESCUENTOS DE NIVEL SEGUN SOLICITUD
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Descuentos_De_Nivel_Segun_SolicitudAsync(_context, DVTA);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00010Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //**107 - PERMITIR VENTA A CLIENTE SIN NOMBRE SIN ENLAZAR A CLIENTE REAL
            //verificar que si se utiliza el codigo SIN NOMBRE codcliente.text en el codcliente real, no puede estar vacio o ser el 
            //mismo codcliente, debe ser un codigo de cliente real
            //implementado en fecha: 11-10-2016 solicitado por JRA
            bool resultado = true;
            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) == true || await cliente.EsClienteCasual(_context, DVTA.codcliente) == true)
            {
                if (DVTA.codcliente == DVTA.codcliente_real || await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real) || await cliente.EsClienteCasual(_context, DVTA.codcliente_real))
                {
                    resultado= false;
                    regcontrol.Valido = "NO";
                    //regcontrol.DescServicio = "";
                    regcontrol.Observacion = "Toda venta al codigo de cliente SIN NOMBRE o CASUAL requiere que el codigo de cliente real-referencia sea un codigo de cliente diferente de un codigo de Cliente Sin Nombre/Casual. Ingrese el permiso especial!!!";
                    if (DVTA.estado_doc_vta.ToUpper() =="NUEVO")
                    {
                        regcontrol.DatoA = DVTA.codcliente +"-"+DVTA.nitfactura;
                    }
                    else
                    {
                        regcontrol.DatoA = DVTA.id + "-"+DVTA.numeroid+" "+DVTA.codcliente;
                    }
                    regcontrol.DatoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                    regcontrol.ClaveServicio = "";
                    regcontrol.Accion = Acciones_Validar.Pedir_Servicio.ToString();
                }
                
            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00011Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //99 - PROFORMA SIN NOMBRE CON DESCUENTOS DE NIVEL OTRO CLIENTE
            //si el cliente de la proforma es un CASUAL  o un SN debe pedir clave
            bool resultado = true;
            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real) )
            {
                resultado = false;
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = "Las Ventas al cliente Sin Nombre o Casual con enlace a cliente Cliente Real-Referencia, requieren permiso especial!!!";
                regcontrol.ObsDetalle= "";
                regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente + "-" + DVTA.codcliente_real;
                regcontrol.DatoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = Acciones_Validar.Pedir_Servicio.ToString();
            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00012Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //12 VERIFICAR TIPO DE PREPARACION DE CLIENTE FINAL
            bool resultado = true;
            regcontrol.Valido = "SI";
            regcontrol.Observacion = "";
            regcontrol.ObsDetalle = "";
            regcontrol.DatoA = "";
            regcontrol.DatoB = "";
            regcontrol.ClaveServicio = "";
            regcontrol.Accion = Acciones_Validar.Ninguna.ToString();

            if (!await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real) || !await cliente.EsClienteCasual(_context, DVTA.codcliente_real))
            {
                if (await cliente.EsClienteFinal(_context, DVTA.codcliente_real))
                {
                    if(DVTA.preparacion !="FINAL" || DVTA.preparacion != "URGENTE")
                    {
                        resultado = false;
                        regcontrol.Valido = "NO";
                        //regcontrol.DescServicio = "";
                        regcontrol.Observacion = "El cliente: " + DVTA.codcliente_real + " es un cliente final por tanto el tipo de preparacion de un cliente final puede ser: FINAL o URGENTE. Corrija esta situacion!!!";
                        regcontrol.ObsDetalle = "";
                        regcontrol.DatoA = "";
                        regcontrol.DatoB = "";
                        regcontrol.ClaveServicio = "";
                        regcontrol.Accion = Acciones_Validar.Ninguna.ToString();
                    }

                }
                
            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00013Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //DESCTOS NIVEL DE CLIENTE COMPETENCIA EN PROFORMA SIN NOMBRE
            bool resultado = true;
            regcontrol.Valido = "SI";
            regcontrol.Observacion = "";
            regcontrol.ObsDetalle = "";
            regcontrol.DatoA = "";
            regcontrol.DatoB = "";
            regcontrol.ClaveServicio = "";
            regcontrol.Accion = Acciones_Validar.Ninguna.ToString();

            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real))
            {
                if (await cliente.EsClienteCompetencia(_context, await cliente.NIT(_context, DVTA.codcliente_real)))
                {
                    resultado = false;
                    regcontrol.Valido = "NO";
                        //regcontrol.DescServicio = "";
                    regcontrol.Observacion = "El cliente real-referencia: " + DVTA.codcliente_real + " esta clasificado como competencia por lo cual para realizar este cliente compras a otro nombre debe ingresar permiso especial!!!";
                    regcontrol.ObsDetalle = "";
                    if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                    {
                        regcontrol.DatoA = DVTA.codcliente + "-" + DVTA.codcliente_real + " " + DVTA.nitfactura;
                    }
                    else
                    {
                        regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente + "-" + DVTA.codcliente_real;
                    }
                    regcontrol.DatoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                    regcontrol.ClaveServicio = "";
                    regcontrol.Accion = Acciones_Validar.Pedir_Servicio.ToString();
                }

            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00014Async(DBContext _context, Controles regcontrol, List<vedesextraDatos> tabladescuentos)
        {
            //VERIFICAR SI LOS DESCUENTOS EXTRAS ESTAN VIGENTES
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_Descuentos_Extras_Habilitados(_context, tabladescuentos);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00015Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            //VERIFICAR FECHA VIGENCIA DESCTOS. EXTRAS VERSUS FECHA INICIAL PROFORMA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Vigencia_DesctosExtras_Segun_FechasAsync(_context, DVTA, tabladescuentos);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00016Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VERIFICAR SI HAY PROMOCIONES PENDIENTES DE APLICAR
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Promociones_Por_Aplicar(_context, DVTA, tabladescuentos,tabladetalle, false, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00017Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //##VALIDAR DESCUENTO POR DEPOSITO
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Descuento_Por_Deposito(_context, DVTA, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00018Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //##VALIDAR DESCUENTO EXTRA REQUIERE CREDITO VALIDO
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Descuento_Extra_Requiere_Credito_Valido(_context, DVTA.codcliente_real, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00019Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<vedetalleanticipoProforma> tablaanticipos, string codempresa)
        {
            //VALIDAR DESCUENTO EXTRA SEGÚN TIPO DE VENTA (CONTADO CE, CONTADO CA, CREDITO)
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Descuentos_Extra_Para_TipoVenta(_context, DVTA, tabladescuentos, tablaanticipos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00020Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VALIDAR MONTO MINIMO SEGÚN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Monto_Minimos_Segun_Lista_Precio(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                {
                    regcontrol.DatoA = DVTA.codcliente + "-" + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                }
                regcontrol.DatoB = "SubTotal: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00021Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VALIDAR MONTO MINIMO PARA ENTREGAR AL CLIENTE
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Monto_Minimo_Para_Entrega_Pedido(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = "";
                regcontrol.DatoB = "";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00022Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VALIDAR MONTO MINIMO PARA APLICAR DESCUENTOS ESPECIALES
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales(_context, DVTA.codcliente_real, tabladetalle, DVTA.codmoneda, DVTA.fechadoc.Date, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                {
                    regcontrol.DatoA = DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid ;
                }
                regcontrol.DatoB = "SubTotal: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00023Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //##VALIDAR CUMPLIMIENTO DE EMPAQUE MINIMO SEGUN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Empaque_Minimo_Segun_Lista_Precios(_context,tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00024Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA,  string codempresa, string usuario)
        {
            //VALIDAR CREDITO DISPONIBLE
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Credito_Disponible_Para_Vta(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00025Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            //VALIDAR ETIQUETA DE LA PROFORMA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Etiqueta_Proforma(_context, dt_etiqueta, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00026Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            //VALIDAR CUMPLIMIENTO DE EMPAQUE MINIMO SEGUN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Etiqueta_Proforma(_context, dt_etiqueta, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00027Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //VERIFICAR SI EL CLIENTE TIENE NOTAS DE REMISION (reversiones pp) PENDIENTES DE PAGO
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Tiene_Reversiones_PP_Pendientes_de_Pago(_context, tabladescuentos,DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00028Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            //VERIFICAR QUE EL CLIENTE Y CLIENTE REAL SEA ATENDIDO POR EL VENDEDOR
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Es_Atendido_Por_El_Vendedor(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00029Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Periodo_Abierto_Para_Venta(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00030Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_NIT_Valido(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00031Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Credito_NIT_Dfte_Cero(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00032Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Monto_Limite_Factura_Sin_Nombre(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00033Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_Descuentos_Extras_Aplicados_Validos(_context, DVTA, tabladetalle, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00034Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_Tipo_Transporte_Valido_Para_Entregar_Pedido(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00035Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_Nombre_Transporte_Valido_Para_Entregar_Pedido(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00036Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Quien_Cancelara_Flete(_context, DVTA.fletepor, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00037Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Otro_Medio_de_Transporte(_context, DVTA.transporte, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    regcontrol.DatoA = DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid;
                }
                regcontrol.DatoB = "Total: " + DVTA.totaldoc + "(" + DVTA.codmoneda + ")";
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00038Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleanticipoProforma> tablaanticipos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Contado_NOCE_Tenga_Anticipo(_context, DVTA, tablaanticipos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00039Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Contado_Contra_Entrega_Cliente_Del_Interior(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                if (objres.accion.ToString() != Acciones_Validar.Pedir_Servicio.ToString())
                {
                    //si la validacion devolvio NO PEDIR SERVICIO, que se quite el codservicio y el desc servicio
                    regcontrol.CodServicio = "0";
                    regcontrol.DescServicio = "";
                }
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00040Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Contado_Contra_Entrega_Con_Anticipo(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00041Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Direccion_Entrega(_context, DVTA.codcliente_real, DVTA.direccion, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00042Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_Si_Hay_Descuentos_Extras_Repetidos(_context, tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00043Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Georeferencia_Entrega_Valida(_context, DVTA.latitud, DVTA.longitud, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00044Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Cliente_Tiene_Cuentas_Por_Pagar_En_Mora(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00045Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //objres = await Validar_Cliente_Tiene_Cuentas_Por_Pagar_En_Mora(_context, DVTA, codempresa);
            //if (objres.resultado == false)
            //{
            //    regcontrol.Valido = "NO";
            //    regcontrol.DescServicio = "";
            //    regcontrol.Observacion = objres.observacion;
            //    regcontrol.ObsDetalle = objres.obsdetalle;
            //    regcontrol.DatoA = objres.datoA;
            //    regcontrol.DatoB = objres.datoB;
            //    regcontrol.ClaveServicio = objres.accion.ToString();
            //    regcontrol.Accion = objres.accion.ToString();
            //}
            return true;
        }
        private async Task<bool> Control_Valido_C00046Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Precio_de_Venta_Permitido_Cliente(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00047Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_Monto_Maximo_De_Venta_Cliente(_context,DVTA.codcliente_real,DVTA.codalmacen ,DVTA.fechadoc , tabladetalle);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00048Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Cliente_SinCuenta_En_Oficina(_context, DVTA , tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00049Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Cliente_ConCuenta_En_Oficina(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00050Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Pedido_Urgente_Cliente(_context, DVTA, tabladetalle, tablarecargos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00051Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Montos_Minimos_Para_Desctos_Extras(_context, tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00052Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Peso_Minimo_Requerido_Para_Alicar_Descuentos_Extras(_context, tabladetalle, tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00053Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Monto_Venta_Para_Bancarizacion(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00054Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Cliente_Solo_Contra_Entrega(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00055Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Verificar_No_Se_Venda_A_Credito_A_Cliente_SinNombre(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00056Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Direccion_Etiqueta_Correcta(_context, DVTA,dt_etiqueta, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00057Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Descuento_Extra_Aplicado_Segun_Tipo_Vta(_context,tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00058Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            //VALIDAR PORCENTAJE MAXIMO DE VENTA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //objres = await Validar_Saldos_Negativos_Doc(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario);
            (objres, dtnocumplen) = await Validar_Limite_Maximo_de_Venta(_context, tabladetalle, DVTA, dtnocumplen, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
                regcontrol.Dtnocumplen = dtnocumplen;
            }
            regcontrol.Dtnocumplen = dtnocumplen;
            return true;
        }
        private async Task<bool> Control_Valido_C00059Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Venta_Con_Desctos_Nivel_anterior(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00060Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<Dtnegativos> dtnegativos, string codempresa, string usuario)
        {
            //VALIDAR SALDOS NEGATIVOS
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //objres = await Validar_Saldos_Negativos_Doc(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario);
            (objres, dtnegativos) = await Validar_Saldos_Negativos_Doc(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
                regcontrol.Dtnegativos = dtnegativos;
            }
            regcontrol.Dtnegativos = dtnegativos;
            return true;
        }
        private async Task<bool> Control_Valido_C00061Async(DBContext _context, Controles regcontrol, bool pedido_por_dia, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Nro_Pedido_Urgentes_Cliente(_context, pedido_por_dia, DVTA, tabladetalle, tablarecargos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00063Async(DBContext _context, Controles regcontrol, bool pedido_por_dia, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = false;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Monto_Minimo_Pedido_Urgente_Provincia(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Minimo_Pedido_Urgente_Provincia(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            string cadena = "";
            if (DVTA.preparacion == "URGENTE")
            {//validar monto minimo 
                double monto_min = await configuracion.monto_minimo_venta_urgente_provincia(_context, codempresa);
                double monto_min_aux = await configuracion.monto_minimo_venta_urgente_provincia(_context, codempresa);
                string moneda_monto_min = await configuracion.moneda_monto_minimo_venta_urgente_provincia(_context, codempresa);

                if (DVTA.codmoneda == moneda_monto_min)
                {
                    if (monto_min > DVTA.subtotaldoc)
                    {
                        cadena += "\n El monto minimo para pedidos urgentes a provincia es de: " + monto_min + "(" + DVTA.codmoneda + ")" + " y la proforma actual no cumple con este requisito.";
                    }
                }
                else
                {
                    monto_min_aux = (double)await tipocambio._conversion(_context, DVTA.codmoneda, moneda_monto_min, await funciones.FechaDelServidor(_context), (decimal)monto_min);
                    if (monto_min_aux > DVTA.subtotaldoc)
                    {
                        cadena += "\n El monto minimo de pedidos urgentes a provincia es de: " + monto_min_aux + "(" + DVTA.codmoneda + ")" + " y la proforma actual no cumple con este requisito.";
                        objres.resultado = false;
                    }
                }
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones del pedido urgente: ";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Subtotal: " + DVTA.subtotaldoc.ToString() + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Nro_Pedido_Urgentes_Cliente(DBContext _context, bool verificar_pordia, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            string cadena = "";
            if (DVTA.preparacion == "URGENTE" || DVTA.preparacion == "URGENTE PROVINCIAS")
            {
                int nro_urg_dia = await cliente.DiaVentasUrgentes(_context, DVTA.codcliente_real, DVTA.fechadoc.Date);
                int nro_urg_dia_max = await empresa.maxurgentes_por_dia(_context, codempresa);

                int nro_urg_semana = await cliente.SemanaVentasUrgentes(_context, DVTA.codcliente_real, DVTA.fechadoc.Date);
                int nro_urg_semana_max = await empresa.maxurgentes(_context, codempresa);

                if (verificar_pordia)
                {//validar nro de pedidos urgentes por dia
                    if(nro_urg_dia >= nro_urg_dia_max)
                    {
                        cadena = "Un cliente solo puede tener " + nro_urg_dia_max + " pedidos urgentes al dia, y el cliente: " + DVTA.codcliente_real + " tiene actualmente: " + nro_urg_dia;
                    }
                }
                else
                {
                    if (nro_urg_semana >= nro_urg_semana_max)
                    {
                        cadena = "Un cliente solo puede tener " + nro_urg_semana_max + " pedidos urgentes desde inicio de semana(lunes), y el cliente: " + DVTA.codcliente_real + " actualmente tiene: " + nro_urg_semana;
                        objres.resultado = false;
                    }
                }
            }

            if (cadena.Trim().Length > 0 )
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones del pedido urgente: ";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Subtotal: " + DVTA.subtotaldoc.ToString() + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Con_Desctos_Nivel_anterior(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.niveles_descuento == "ANTERIOR" )
            {
                objres.resultado = false;
                objres.observacion = "Las ventas con descuentos de nivel ANTERIOR requieren permiso especial!!!";
                objres.obsdetalle = "";
                if(DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                    objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                }
                else
                {
                    objres.datoA = DVTA.id;
                    objres.datoB = DVTA.numeroid;
                }
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuento_Extra_Aplicado_Segun_Tipo_Vta(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {

            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            string cadena = "";
            string tipo_vta_descto = "";


            foreach (var descuentos in tabladescuentos)
            {
                tipo_vta_descto = await ventas.Descuento_Extra_Tipo_Venta(_context, descuentos.coddesextra);
                if (DVTA.tipo_vta == "CREDITO")
                {
                    if (tipo_vta_descto == "CREDITO" || tipo_vta_descto == "SIN RESTRICCION")
                    {
                        //OK
                    }
                    else
                    {
                        cadena += "\n " + descuentos.coddesextra + "-" + descuentos.descrip;
                    }
                }
                else
                {
                    if (tipo_vta_descto == "CONTADO" || tipo_vta_descto == "SIN RESTRICCION")
                    {
                        //OK
                    }
                    else
                    {
                        cadena += "\n " + descuentos.coddesextra + "-" + descuentos.descrip;
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos no pueden aplicarse en ventas al: " + DVTA.tipo_vta;
                objres.obsdetalle = "\n" + cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Direccion_Etiqueta_Correcta(DBContext _context, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (!await cliente.EsClienteSinNombre(_context, DVTA.codcliente))
            {
                if (dt_etiqueta.Count > 0)
                {
                    foreach (var etiqueta in dt_etiqueta)
                    {
                        if (await cliente.direccion_es_valida(_context, DVTA.codcliente, etiqueta.representante))
                        {
                            objres.resultado = true;
                            objres.observacion = "";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else
                        {
                            if (!await cliente.direccion_con_pto_vta_es_valida(_context, DVTA.codcliente, etiqueta.representante))
                            {
                                objres.resultado = false;
                                objres.observacion = "La direccion de la etiqueta, no corresponde a una direccion valida para el cliente: " + DVTA.codcliente;
                                objres.obsdetalle = "";
                                objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                                objres.datoB = DVTA.codcliente_real;
                                objres.accion = Acciones_Validar.Pedir_Servicio;
                            }

                        }
                    }
                }
                else
                {
                    objres.resultado = false;
                    objres.observacion = "No se encontro la etiqueta para la proforma!!!";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            else
            {
                if (!(DVTA.codcliente == DVTA.codcliente_real))
                {
                    if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.Es_Cliente_Casual(_context, DVTA.codcliente) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real))
                    {
                        if(dt_etiqueta.Count > 0)
                        {
                            foreach (var etiqueta in dt_etiqueta)
                            {
                                if (await cliente.direccion_es_valida(_context, DVTA.codcliente_real, etiqueta.representante))
                                {
                                    objres.resultado = true;
                                    objres.observacion = "";
                                    objres.obsdetalle = "";
                                    objres.datoA = "";
                                    objres.datoB = "";
                                    objres.accion = Acciones_Validar.Ninguna;
                                }
                                else
                                {
                                    if (!await cliente.direccion_con_pto_vta_es_valida(_context, DVTA.codcliente_real, etiqueta.representante))
                                    {
                                        objres.resultado = false;
                                        objres.observacion = "La direccion de la etiqueta, no corresponde a una direccion valida para el cliente: " + DVTA.codcliente_real;
                                        objres.obsdetalle = "";
                                        objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                                        objres.datoB = DVTA.codcliente_real;
                                        objres.accion = Acciones_Validar.Pedir_Servicio;
                                    }

                                }

                            }
                        }
                        else
                        {
                            objres.resultado = false;
                            objres.observacion = "No se encontro la etiqueta para la proforma!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        
                    }
                      
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_No_Se_Venda_A_Credito_A_Cliente_SinNombre(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.tipo_vta == "CREDITO")
            {
                if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente))
                {
                    objres.resultado = false;
                    objres.observacion = "No puede hacer una venta a credito a un cliente Sin Nombre y/o Casual.";
                    objres.obsdetalle = "";
                    objres.datoA = DVTA.codcliente + "-" + DVTA.nitfactura;
                    objres.datoB = "Total: " + DVTA.totaldoc.ToString() + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Cliente_Solo_Contra_Entrega(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            bool venta_es_ce = true;
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            venta_es_ce = DVTA.contra_entrega == "SI";

            if (await restricciones.Validar_Cliente_Contraentrega(_context, venta_es_ce, DVTA.codcliente_real))
            {
                objres.resultado = false;
                objres.observacion = "El cliente: " + DVTA.codcliente_real + " esta Registrado como 'Solo Venta Contra Entrega'. Por favor marque este documento como una venta contra-entrega antes de proseguir.";
                objres.obsdetalle = "";
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString() + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Venta_Para_Bancarizacion(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            double MAX_RND = 0;
            double monto_convertido = 0;
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            MAX_RND = await configuracion.emp_monto_rnd100011(_context, codempresa);
            monto_convertido = (double)await tipocambio._conversion(_context,await empresa.monedabase(_context, codempresa),DVTA.codmoneda, DVTA.fechadoc, (decimal)DVTA.totaldoc);

            if (monto_convertido >= MAX_RND)
            {
                objres.resultado = false;
                objres.observacion = "Como esta venta es mayor a " + MAX_RND + " (MN) tendra que ser pagada por medio de una entidad bancaria obligatoriamente, Necesita autorizacion especial.";
                objres.obsdetalle = "";
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString() + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Peso_Minimo_Requerido_Para_Alicar_Descuentos_Extras(DBContext _context, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            string cadena = "";
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real))
            {   //calcular Peso
                double peso_doc = 0;
                foreach (var detalle in tabladetalle)
                {
                    peso_doc += detalle.cantidad * await items.itempeso(_context, detalle.coditem);
                }
                foreach (var descuentos in tabladescuentos)
                {
                    if (peso_doc < await ventas.PesoMinimoDescuentoExtra(_context, descuentos.coddesextra))
                    {
                        if (cadena.Trim().Length == 0)
                        {
                           cadena = "El descuento " + descuentos.descrip + " no cumple el peso minimo para su aplicacion.";
                        }
                        else
                        {
                            cadena += "\n El descuento " + descuentos.descrip + " no cumple el peso minimo para su aplicacion.";
                        }
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los descuentos extras aplicados no cumplen la cantidad minima en KG para su aplicacion!!!";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString() + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Montos_Minimos_Para_Desctos_Extras(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            string cadena = "";
            double _total = 0;
            double _montoMIN = 0;
            double _dif = 0;
            //verificar el monto si cumple con el requerido
            //antes se obitene datos del complemento mayorista-Dimediado si tiene
            int _codproforma = 0;
            double _subtotal_pfcomplemento = 0;
            string _moneda_total_pfcomplemento = "";
            bool hay_enlace = false;

            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real))
            {
                try
                {
                    if (DVTA.idpf_complemento.Trim().Length > 0 && DVTA.nroidpf_complemento.Trim().Length > 0 && DVTA.tipo_complemento == "complemento_para_descto_monto_min_desextra")
                    {
                        _codproforma = await ventas.codproforma(_context, DVTA.nroidpf_complemento,Convert.ToInt32(DVTA.nroidpf_complemento));
                        if (Convert.ToInt32(DVTA.nroidpf_complemento) > 0)
                        {
                            _moneda_total_pfcomplemento = await ventas.MonedaPF(_context, _codproforma);
                            _subtotal_pfcomplemento = (double)await ventas.SubTotal_Proforma(_context, _codproforma);
                            _subtotal_pfcomplemento = (double)await tipocambio._conversion(_context, DVTA.codmoneda, _moneda_total_pfcomplemento, DVTA.fechadoc.Date, (decimal)_subtotal_pfcomplemento);
                            hay_enlace = true;
                        }
                    }
                }
                catch
                {
                    _moneda_total_pfcomplemento = "";
                    _subtotal_pfcomplemento = 0;
                }
                if (DVTA.tipo_vta == "CONTADO")
                {
                    //contado
                    foreach (var descuentos in tabladescuentos)
                    {
                        _total = DVTA.subtotaldoc;
                        //implementado en fecha: 07-07-2021
                        //se añade el subtotal del complemento
                        _total += _subtotal_pfcomplemento;
                        //obtener el montomin requerido por el descto extra
                        _montoMIN = (double)await ventas.MontoMinimoContadoDescuentoExtra(_context, descuentos.coddesextra, DVTA.codmoneda, DVTA.fechadoc.Date);
                        _dif = _montoMIN - _total;
                        if (_montoMIN == 0)
                        {
                            objres.resultado = true;
                        }
                        else
                        {
                            if (_total < _montoMIN)
                            {
                                if (_subtotal_pfcomplemento > 0)
                                {
                                    cadena +=  "\n Esta proforma tiene complemento";
                                }
                                cadena +=  "\n El descuento: " + descuentos.coddesextra + "-" + descuentos.descrip + " no cumple el monto minimo para su aplicacion, el monto mínimo es: " + _montoMIN.ToString() + "(" + DVTA.codmoneda + ") y el subtotal de esta proforma: " + DVTA.subtotaldoc.ToString() + " mas su complemento de: " + _subtotal_pfcomplemento.ToString() + " es de:" + _total + "(" + DVTA.codmoneda + ")";
                            }
                        }
                    }

                }
                else
                {
                    //credito
                    foreach (var descuentos in tabladescuentos)
                    {
                        _total = DVTA.subtotaldoc;
                        //implementado en fecha: 07-07-2021
                        //se añade el subtotal del complemento
                        _total += _subtotal_pfcomplemento;
                        //obtener el montomin requerido por el descto extra
                        _montoMIN = (double)await ventas.MontoMinimoCreditoDescuentoExtra(_context, descuentos.coddesextra, DVTA.codmoneda, DVTA.fechadoc.Date);
                        _dif = _montoMIN - _total;
                        if (_montoMIN == 0)
                        {
                            objres.resultado = true;
                        }
                        else
                        {
                            if (_total < _montoMIN)
                            {
                                if (_subtotal_pfcomplemento > 0)
                                {
                                    cadena += "\n Esta proforma tiene complemento para cumplir monto minimo de descto extra";
                                }
                                cadena += "\n El descuento: " + descuentos.coddesextra + "-" + descuentos.descrip + " no cumple el monto minimo para su aplicacion, el monto mínimo es: " + _montoMIN.ToString() + "(" + DVTA.codmoneda + ") y el subtotal de esta proforma: " + DVTA.subtotaldoc.ToString() + " mas su complemento de: " + _subtotal_pfcomplemento.ToString() + " es de:" + _total + "(" + DVTA.codmoneda + ")";
                            }
                        }
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras aplicados no cumplen el monto minimo requerido:";
                objres.obsdetalle = cadena;
                if(DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                }
                objres.datoB = "Subtotal: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Pedido_Urgente_Cliente(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            string cadena = "";
            string cadena_msg_empaques = "";
            string cadena_no_cumple_empaque = "";
            int nro_items;
            int nro_items_no_cumple_empaque = 0;
            int nro_items_max;

            if (DVTA.preparacion == "URGENTE" || DVTA.preparacion == "URGENTE PROVINCIAS")
            {
                nro_items = tabladetalle.Count;
                nro_items_max =await empresa.maxitemurgentes(_context, codempresa);
                //verificar el nro de items del pedido
                if (nro_items > nro_items_max)
                {
                    cadena = $"\nPara un pedido URGENTE el numero maximo de items es {nro_items_max}, y el pedido actual tiene: {nro_items} items.";
                    objres.resultado = false;
                }
            }

            if (DVTA.preparacion == "URGENTE PROVINCIAS")
            {//validar el recargo para pedidos urgentes provincias
                int codrecargo_pedido_urg_provincia = await configuracion.emp_codrecargo_pedido_urgente_provincia(_context,codempresa);
                List<int> lst_rec_prov = tablarecargos.AsEnumerable().Where(r => r.codrecargo == codrecargo_pedido_urg_provincia).Select(r => r.codrecargo).ToList();

                if (lst_rec_prov.Count == 0)
                {
                    cadena += $"\nTodo pedido urgente con envio a provincias, debe incluir el recargo de envio: {codrecargo_pedido_urg_provincia} por favor añada el recargo.";
                }
            }
            //verificar si cumple los empaque
            //aclaracion de marlen en fecha: 30-09-2021 (en pedidos urgentes todo debe controlar, por mas que sea final)
            //Validar empaques Cerrados
            if (DVTA.preparacion == "URGENTE" || DVTA.preparacion == "URGENTE PROVINCIAS")
            {
                int nro_items_urgentes_debe_cumplir_empaque_cerrado = await empresa.nro_items_urgentes_empaque_cerrado(_context, codempresa);

                foreach (var row in tabladetalle)
                {
                    if (await ventas.Tarifa_EmpaqueCerrado(_context, row.codtarifa))
                    {
                        if (!await ventas.CumpleEmpaqueCerrado(_context, row.coditem, row.codtarifa, row.coddescuento, (decimal)row.cantidad, DVTA.codcliente_real))
                        {
                            cadena_no_cumple_empaque += $"\n{row.coditem} {funciones.Rellenar(row.descripcion, 20, " ", false)} {funciones.Rellenar(row.medida, 14, " ", false)}  {funciones.Rellenar(row.cantidad.ToString(), 12, " ")}";
                            nro_items_no_cumple_empaque++;
                        }
                    }
                }

                if (!(nro_items_no_cumple_empaque < nro_items_urgentes_debe_cumplir_empaque_cerrado))
                {
                    cadena_msg_empaques = $"En pedidos urgentes, {nro_items_urgentes_debe_cumplir_empaque_cerrado} deben cumplir empaque cerrado, y en este pedido existen: {nro_items_no_cumple_empaque} items que no cumplen con empaque cerrado:";
                }
                else
                {
                    cadena_msg_empaques = "";
                }
            }

            if (cadena.Trim().Length > 0 || cadena_msg_empaques.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones al pedido urgente: ";
                objres.obsdetalle = cadena + "\n" + cadena_msg_empaques + "\n" + cadena_no_cumple_empaque;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Venta_Cliente_ConCuenta_En_Oficina(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            string cadena = "";

            if (DVTA.vta_cliente_en_oficina == false)
            {
                return objres;
            }
            ///////////////////////////////////////////////
            //SI ES VENTA A CLIENTE CON CODIGO
            //'///////////////////////////////////////////////

            if (!await cliente.EsClienteSinNombre(_context, DVTA.codcliente) )
            {//validar venta con codigo
                int codempaque_vta_oficina = await configuracion.empaque_venta_cliente_en_oficina(_context, codempresa);
                int nro_empaques_minimo = await configuracion.Nro_Empaques_Minimo_Vta_Oficina(_context, codempresa);
                int maximo_items_conteo = await configuracion.Nro_Items_Maximo_Conteo_Vta_Oficina(_context, codempresa);
                int nro_empaques_cerrados = 0;
                int nro_items_conteo = 0;
                List<dynamic> tbl_control = new List<dynamic>();

                foreach (var reg in tabladetalle)
                {
                    string coditem = reg.coditem.ToString();
                    double cantidad_pedido = Convert.ToDouble(reg.cantidad);
                    double cantidad_empaque_correcto = await ventas.Empaque(_context,codempaque_vta_oficina, coditem);
                    nro_empaques_cerrados = 0;
                    if (cantidad_empaque_correcto != 0)
                    {
                        // Realizar la división solo si el valor del empaque correcto es diferente de cero
                        nro_empaques_cerrados = (int)(cantidad_pedido / cantidad_empaque_correcto);
                    }   
                    double sobrante_de_empaque = cantidad_pedido % cantidad_empaque_correcto;
                    int es_conteo = sobrante_de_empaque > 0 ? 1 : 0;
                    nro_items_conteo += es_conteo;

                    tbl_control.Add(new { coditem, cantidad_pedido, cantidad_empaque_correcto, nro_empaques_cerrados, sobrante_de_empaque, es_conteo });
                }

                nro_empaques_cerrados = tbl_control.Sum(reg => reg.nro_empaques_cerrados);

                if (nro_empaques_cerrados < nro_empaques_minimo)
                {
                    cadena += $"\nLa venta en oficina a clientes(con codigo de cliente) debe contener al menos: {nro_empaques_minimo} empaques {await nombres.nombreempaque(_context, codempaque_vta_oficina)} y el pedido solo tiene solo: {nro_empaques_cerrados} items con empaque cerrado.";
                }

                if (nro_items_conteo > maximo_items_conteo)
                {
                    cadena += $"\nEl numero maximo de items de conteo para venta en oficina a cliente(con codigo de cliente) es: {maximo_items_conteo} y la proforma actualmente tiene: {nro_items_conteo} items de conteo, verifique esta situacion!!!";
                }

                if (cadena.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Se tienen las siguientes observaciones a la venta en oficina a cliente con cuenta:";
                    objres.obsdetalle = cadena;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }

            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Cliente_SinCuenta_En_Oficina(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.vta_cliente_en_oficina == false)
            {
                return objres;
            }
            ///////////////////////////////////////////////
            //SI ES VENTA A CON CODIGO SIN NOMBRE
            //'///////////////////////////////////////////////
            
            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente))
            {//validar venta minima cliente sin nombre
                double monto_min_sinnombre = await configuracion.monto_minimo_venta_cliente_en_oficina(_context, codempresa);
                string codmoneda_monto_mins_sn = await configuracion.moneda_monto_minimo_venta_cliente_en_oficina(_context, codempresa);
                double monto_min_requerido = monto_min_sinnombre;
                
                if(DVTA.codmoneda != codmoneda_monto_mins_sn)
                {
                    monto_min_requerido = (double)await tipocambio._conversion(_context, DVTA.codmoneda, codmoneda_monto_mins_sn, await funciones.FechaDelServidor(_context), (decimal)monto_min_sinnombre);
                    monto_min_requerido = Math.Round(monto_min_requerido, 2);
                }
                if (monto_min_requerido > DVTA.subtotaldoc)
                {
                    objres.resultado = false;
                    objres.observacion = "El monto minimo para venta a cliente(Sin Nombre o Casual) en oficina es de: " + monto_min_requerido.ToString() + " (" + DVTA.codmoneda + ") y el subtotal de la proforma actual es: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ") y es menor a este minimo, verifique esta situacion!!!";
                    objres.obsdetalle = "";
                    objres.datoA = DVTA.nitfactura;                    
                    objres.datoB = "SubTotal: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.resultado = true;
                    objres.observacion = "";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Monto_Maximo_De_Venta_Cliente(DBContext _context, string codcliente_real, string codalmacen,DateTime fecha, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            string cadena_items = "";
            string cadena_solo_items = "";
            double max = 0;
            int diascontrol = 0;
            double cantidad_ttl_vedida = 0;

            //esta funcion verifica si el cliente - item - alacen tiene configurado un maximo de ventas en $ cada cierto numero de dias
            if (await cliente.ClienteControlaMaximo(_context, codcliente_real))
            {
                foreach (var detalle in tabladetalle)
                {
                    max =await items.MaximoDeVenta(_context, detalle.coditem, codalmacen, detalle.codtarifa);
                    if (max > 0) //si controlar
                    {
                        diascontrol = await items.MaximoDeVenta_PeriodoDeControl(_context, detalle.coditem, codalmacen, detalle.codtarifa);
                        if(diascontrol > 0)
                        {
                            //obtiene la cantidad vendida en los ultimos X dias mas lo que se quiere vender a ahora
                            cantidad_ttl_vedida = 0;
                            cantidad_ttl_vedida = await cliente.CantidadVendida(_context, detalle.coditem,codcliente_real, fecha.Date.AddDays(-diascontrol), fecha) + detalle.cantidad;
                            if(cantidad_ttl_vedida <= max)
                            {
                                detalle.cumple = true;
                            }
                            else
                            {
                                detalle.cumple = false;
                                if(cadena_items.Trim().Length > 0)
                                {
                                    cadena_solo_items = detalle.coditem;
                                    cadena_items = "Item:" + detalle.coditem + "cantidad Vendida: " + cantidad_ttl_vedida.ToString() + " MaxVta Permitida: " + max.ToString();
                                }
                                else
                                {
                                    cadena_items += "\nItem:" + detalle.coditem + " cantidad Vendida: " + cantidad_ttl_vedida.ToString() + " MaxVta Permitida: " + max.ToString();
                                    cadena_solo_items += ", " + detalle.coditem;
                                }
                            }
                        }
                        else
                        {
                            detalle.cumple = true;
                        }
                        
                    }
                    else
                    {
                        detalle.cumple = true;
                    }

                }

                if (cadena_items.Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items, ya fueron comprados por el cliente: " + codcliente_real + " en los ultimos "+ diascontrol + " dias.";
                    objres.obsdetalle = cadena_items;
                    objres.datoA = codcliente_real;
                    objres.datoB = cadena_solo_items;
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.resultado = true;
                    objres.observacion = "";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Precio_de_Venta_Permitido_Cliente(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            List<string> precios_doc = new List<string>();
            List<string> precios_cliente = new List<string>();
            string cadena_precios_cliente = "";

            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            // Llenar precios del documento de venta
            precios_doc.Clear();
            foreach (var detalle in tabladetalle)
            {
                if (!precios_doc.Contains(detalle.codtarifa.ToString()))
                {
                    precios_doc.Add(Convert.ToString(detalle.codtarifa));
                }
            }

            // Obtener lista de precios permitidos al cliente
            var preciosClienteQuery = await _context.veclienteprecio
                                   .Where(precio => precio.codcliente == DVTA.codcliente_real)
                                   .OrderBy(precio => precio.codtarifa)
                                   .Select(precio => precio.codtarifa)
                                   .ToListAsync();

            foreach (var codtarifa in preciosClienteQuery)
            {
                precios_cliente.Add(Convert.ToString(codtarifa));
                if (cadena_precios_cliente.Trim().Length == 0)
                {
                    cadena_precios_cliente += Convert.ToString(codtarifa);
                }
                else
                {
                    cadena_precios_cliente += ", " + Convert.ToString(codtarifa);
                }
            }

            // Verificar precios no permitidos al cliente
            string cadena_precios_no_permitidos = "";
            int nro_no_permitidos = 0;
            for (int j = 0; j < precios_doc.Count; j++)
            {
                if (!precios_cliente.Contains(precios_doc[j]))
                {
                    if (cadena_precios_no_permitidos.Trim().Length == 0)
                    {
                        cadena_precios_no_permitidos = precios_doc[j].ToString();
                        nro_no_permitidos += 1;
                    }
                    else
                    {
                        cadena_precios_no_permitidos += ", " + precios_doc[j].ToString();
                        nro_no_permitidos += 1;
                    }
                }
            }

            // Si hay uno o más precios no permitidos
            if (nro_no_permitidos > 0)
            {
                objres.resultado = false;
                objres.observacion = "El cliente no tiene habilitado el/los precio(s): " + cadena_precios_no_permitidos + " los precios habilitados para el cliente son: " + cadena_precios_cliente;
                objres.obsdetalle = "";
                objres.datoA = DVTA.codcliente_real;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente;
                }
                objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Es_Atendido_Por_El_Vendedor(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = ""; 
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await configuracion.emp_clientevendedor(_context, codempresa))
            {
                if (!await ventas.Cliente_de_vendedor(_context, DVTA.codcliente, int.Parse(DVTA.codvendedor)))
                {
                    //verificar si es cliente casual (cliente al que se realiza la proforma) es o no casual, solo para clientes casuales se aceptara que no sea su vendedor el vendedor del documento de la proforma
                    if (await cliente.EsClienteCasual(_context, DVTA.codcliente) == false)
                    {
                        objres.resultado = false;
                        objres.observacion = "El cliente:" + DVTA.codcliente_real + " No Es Casual y no pertenece al a cartera del vendedor " + DVTA.codvendedor + ", la venta debe ser realizada con el vendedor al cual esta asignado el cliente.";
                        objres.obsdetalle = "Verifique esta situacion";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                    else
                    {
                        objres.resultado = false;
                        objres.observacion = "El cliente:" + DVTA.codcliente_real + " Si Es Casual y no pertenece al a cartera del vendedor " + DVTA.codvendedor + ", esta seguro de continuar con la operacion?";
                        objres.obsdetalle = "Verifique esta situacion";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                }
            }
            return objres;
        }

        public async Task<(ResultadoValidacion resultadoValidacion, List<Dtnocumplen> dtnocumplen)> Validar_Limite_Maximo_de_Venta(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_items2 = "";

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Pedir_Servicio;

            if (DVTA.estado_doc_vta == "NUEVO")
            {
                if (DVTA.id.Substring(0, 2) == "FC")
                {
                    DVTA.id = DVTA.idpf_solurgente;
                    DVTA.numeroid = DVTA.noridpf_solurgente;
                }
                else
                {
                    DVTA.id = "";
                    DVTA.numeroid = "0";
                }
            }

            decimal PorcenMaxVtaAg = await hardcoded.MaximoPorcentajeDeVentaPorMercaderia(_context, Convert.ToInt32(DVTA.codalmacen));
            // modificado 03-11-2021 antes de hacer la validacion de porcentaje del detalle que sume las cantidades de los items de
            // compras hechas 30 dias antes
            int diascontrol = await configuracion.Dias_Proforma_Vta_Item_Cliente(_context, codempresa);
            string valida_nr_pf = await configuracion.Valida_Maxvta_NR_PF(_context, codempresa);
            // añadir al la dt copia para validar el max vta con las cantidades sumadas
            List<itemDataMatrizMaxVta> dt_detalle_item = new List<itemDataMatrizMaxVta>();
            dt_detalle_item.Clear();

            // Utilizar ConvertAll para convertir los elementos de tabladetalle a itemDataMatrizMaxVta
            dt_detalle_item = tabladetalle.ConvertAll(item =>
                new itemDataMatrizMaxVta
                {
                    coditem = item.coditem,
                    descripcion = item.descripcion,
                    medida = item.medida,
                    udm = item.udm,
                    porceniva = item.porceniva,
                    niveldesc = item.niveldesc,
                    porcendesc = item.porcendesc,
                    porcen_mercaderia = item.porcen_mercaderia,
                    cantidad_pedida = item.cantidad_pedida,
                    cantidad = item.cantidad,
                    codtarifa = item.codtarifa,
                    coddescuento = item.coddescuento,
                    precioneto = item.precioneto,
                    preciodesc = item.preciodesc,
                    preciolista = item.preciolista,
                    total = item.total,
                    cumple = item.cumple,
                    nroitem = item.nroitem,
                    cantidad_pf_anterior = 0,
                    cantidad_pf_total = 0
                }
            );
            dt_detalle_item.Clear();
            foreach (var detalle in tabladetalle)
            {
                // obtiene los items y la cantidad en proformas en los ultimos X dias mas lo que se quiere vender a ahora
                decimal cantidad_ttl_vendida_pf = 0;
                decimal cantidad_ttl_vendida_pf_actual = 0;
                decimal cantidad_ttl_vendida_pf_total = 0;
                cantidad_ttl_vendida_pf_actual = (decimal)detalle.cantidad;
                DateTime fecha_serv = await funciones.FechaDelServidor(_context);
                if (diascontrol > 0 && detalle.cantidad > 0)
                {
                    if (valida_nr_pf == "PF")
                    {
                        cantidad_ttl_vendida_pf = await cliente.CantidadVendida_PF(_context, Convert.ToString(detalle.coditem), DVTA.codcliente_real, fecha_serv.Date.AddDays(-diascontrol), fecha_serv.Date);
                    }
                    else if (valida_nr_pf == "NR")
                    {
                        cantidad_ttl_vendida_pf = await cliente.CantidadVendida_NR(_context, Convert.ToString(detalle.coditem), DVTA.codcliente_real, fecha_serv.Date.AddDays(-diascontrol), fecha_serv.Date);
                    }
                    else
                    {
                        cantidad_ttl_vendida_pf = await cliente.CantidadVendida_PF(_context, Convert.ToString(detalle.coditem), DVTA.codcliente_real, fecha_serv.Date.AddDays(-diascontrol), fecha_serv.Date);
                    }

                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                    // aqui verificar si la cantidad sumada con las pf q hubieran y la pf actual sobrepasan el porcentaje o no
                    // VALIDAR PORCENTAJE MAXIMO DE VENTA
                    if (cantidad_ttl_vendida_pf_total > (decimal)detalle.cantidad)
                    {
                        // poner la cantidad del item obtenido de la suma de la cantidad actual de la pf y de las pf q hubiera
                        dt_detalle_item.Add(new itemDataMatrizMaxVta
                        {
                            coditem = detalle.coditem,
                            descripcion = detalle.descripcion,
                            medida = detalle.medida,
                            udm = detalle.udm,
                            porceniva = detalle.porceniva,
                            niveldesc = detalle.niveldesc,
                            porcendesc = detalle.porcendesc,
                            porcen_mercaderia = detalle.porcen_mercaderia,
                            cantidad_pedida = detalle.cantidad_pedida,
                            cantidad = detalle.cantidad,
                            codtarifa = detalle.codtarifa,
                            coddescuento = detalle.coddescuento,
                            precioneto = detalle.precioneto,
                            preciodesc = detalle.preciodesc,
                            preciolista = detalle.preciolista,
                            total = detalle.total,
                            cumple = detalle.cumple,
                            nroitem = detalle.nroitem,
                            // Añadir las propiedades adicionales de itemDataMatrizMaxVta que no existen en itemDataMatriz
                            cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                            cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                        });
                    }
                    else
                    {
                        dt_detalle_item.Add(new itemDataMatrizMaxVta
                        {
                            coditem = detalle.coditem,
                            descripcion = detalle.descripcion,
                            medida = detalle.medida,
                            udm = detalle.udm,
                            porceniva = detalle.porceniva,
                            niveldesc = detalle.niveldesc,
                            porcendesc = detalle.porcendesc,
                            porcen_mercaderia = detalle.porcen_mercaderia,
                            cantidad_pedida = detalle.cantidad_pedida,
                            cantidad = detalle.cantidad,
                            codtarifa = detalle.codtarifa,
                            coddescuento = detalle.coddescuento,
                            precioneto = detalle.precioneto,
                            preciodesc = detalle.preciodesc,
                            preciolista = detalle.preciolista,
                            total = detalle.total,
                            cumple = detalle.cumple,
                            nroitem = detalle.nroitem,
                            // Añadir las propiedades adicionales de itemDataMatrizMaxVta que no existen en itemDataMatriz
                            cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                            cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                        });
                    }
                }
                else
                {
                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                    dt_detalle_item.Add(new itemDataMatrizMaxVta
                    {
                        coditem = detalle.coditem,
                        descripcion = detalle.descripcion,
                        medida = detalle.medida,
                        udm = detalle.udm,
                        porceniva = detalle.porceniva,
                        niveldesc = detalle.niveldesc,
                        porcendesc = detalle.porcendesc,
                        porcen_mercaderia = detalle.porcen_mercaderia,
                        cantidad_pedida = detalle.cantidad_pedida,
                        cantidad = detalle.cantidad,
                        codtarifa = detalle.codtarifa,
                        coddescuento = detalle.coddescuento,
                        precioneto = detalle.precioneto,
                        preciodesc = detalle.preciodesc,
                        preciolista = detalle.preciolista,
                        total = detalle.total,
                        cumple = detalle.cumple,
                        nroitem = detalle.nroitem,
                        // Añadir las propiedades adicionales de itemDataMatrizMaxVta que no existen en itemDataMatriz
                        cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                        cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                    });
                }
            }
            dtnocumplen.Clear();
            dtnocumplen = await restricciones.ValidarMaximoPorcentajeDeMercaderiaVenta(_context, DVTA.codcliente_real, false, dt_detalle_item, Convert.ToInt32(DVTA.codalmacen), PorcenMaxVtaAg, DVTA.id, Convert.ToInt32(DVTA.numeroid), codempresa, usuario);
            dt_detalle_item.Clear();

            // poner a todos del detalle que cumplen
            foreach (var detalle in tabladetalle)
            {
                detalle.porcen_mercaderia = 0;
            }

            // ahora indentificar los que no cumplen de la tabla detalle
            double _porcen = 0;
            foreach (var nocumplen in dtnocumplen)
            {

                if (Math.Round(Convert.ToDouble(nocumplen.saldo), 2) == 0)
                {
                    // si el el saldo es cero evitar la divicion entre cero
                    _porcen = 0;
                }
                else
                {
                    _porcen = (Convert.ToDouble(nocumplen.cantidad) * 100) / Convert.ToDouble(nocumplen.saldo);
                    _porcen = Math.Round(_porcen, 2);
                }

                if (nocumplen.obs.ToString() == "No hay Saldo!!!")
                {
                    // si no hay saldo
                    goto sgte;
                }
                else if (!nocumplen.obs.ToString().Equals("Cumple"))
                {
                    // formar cadena de los que no cumplen
                    if (cadena_items.Trim().Length == 0)
                    {
                        cadena_items = nocumplen.codigo.ToString();
                    }
                    else
                    {
                        cadena_items += " | " + nocumplen.codigo.ToString();
                    }
                }

                foreach (var detalle in tabladetalle)
                {
                    if (detalle.coditem.ToString() == nocumplen.codigo.ToString())
                    {
                        // SI NO CUMPLE ES DECIR PORCENTAJE DE VENTA MAYOR AL PERMITIDO
                        detalle.porcen_mercaderia = (double)_porcen;
                        break;
                    }
                }

            sgte:;
            }

            if (cadena_items.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Hay algunos items que sobrepasan el limite de venta!!!";
                objres.obsdetalle = cadena_items;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura + "-" + DVTA.codcliente_real;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                }
                objres.datoB = cadena_items;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return (objres, dtnocumplen);
        }


        public async Task<(ResultadoValidacion resultadoValidacion, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, List<Dtnegativos> dtnegativos, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_items2 = "";

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (tabladetalle.Count > 0)
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                dtnegativos.Clear();
                dtnegativos = await saldos.ValidarNegativosDocVenta(_context, tabladetalle, Convert.ToInt32(DVTA.codalmacen), DVTA.idpf_solurgente, Convert.ToInt32(DVTA.noridpf_solurgente), msgs, negs, codempresa, usuario);

                foreach (var negativo in dtnegativos)
                {
                    if (negativo.obs.ToString() == "Genera Negativo")
                    {
                        if ((int)negativo.cantidad_conjunto > 0)
                        {
                            negs.Add(negativo.coditem_cjto.ToString());
                        }
                        if ((int)negativo.cantidad_suelta > 0)
                        {
                            negs.Add(negativo.coditem_suelto.ToString());
                        }
                    }
                }
                if (negs.Count == 0)
                {
                    objres.resultado = true;
                    objres.observacion = "Ningun item del documento genera negativos";
                    objres.accion = Acciones_Validar.Solo_Ok;

                    foreach (var detalle in tabladetalle)
                    {
                        detalle.cumple = true;
                    }
                }
                else
                {
                    foreach (var detalle in tabladetalle)
                    {
                        if (negs.Contains(detalle.coditem.ToString()))
                        {
                            // No cumple genera negativo
                            detalle.cumple = false;
                            // Genera la cadena de items en una fila
                            if (cadena_items.Trim().Length == 0)
                            {
                                cadena_items = detalle.coditem.ToString();
                            }
                            else
                            {
                                cadena_items += " - " + detalle.coditem.ToString();
                            }
                            // Genera la lista item en filas
                            if (cadena_items2.Trim().Length == 0)
                            {
                                cadena_items2 = detalle.coditem.ToString();
                            }
                            else
                            {
                                cadena_items2 += " - " + detalle.coditem.ToString();
                            }
                        }
                        else
                        {
                            // Si cumple y NO genera negativo
                            detalle.cumple = true;
                        }
                    }
                }
            }

            if (await almacen.Es_Tienda(_context, int.Parse(DVTA.codalmacen)))
            {
                //si es tienda se habilita la posibilidad de facturar con negativos
                //ESTO SERA PARA CASOS EXCEPCIONALES
                if (cadena_items.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items generan saldos negativos!!!";
                    objres.obsdetalle = cadena_items2;
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                    objres.datoB = cadena_items2;
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            else
            {
                if (cadena_items.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items generan saldos negativos!!!";
                    objres.obsdetalle = cadena_items2;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return (objres, dtnegativos);
        }
        private async Task<bool> Control_Valido_C00066Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Items_Repetidos(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_Items_Repetidos(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_repeditos = "";
            string item = "";
            int i, j;
            decimal cantidad;
            decimal cantidad_total;
            ArrayList items_iguales = new ArrayList();
            bool resultado = true;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //Verificar si item tiene en el detalle items repetidos
            //son los que tenian antes de la auditoria
            if (!await empresa.Permitir_Items_Repetidos(_context, codempresa))
            {
                if (!await cliente.Permite_items_repetidos(_context, DVTA.version_codcontrol))
                {
                    //llamar a la funcion que devolvera los items repetidos si es que los hay
                    cadena_repeditos = "";
                    cadena_repeditos = Cadena_Items_Repetidos(tabladetalle);
                    if ((cadena_repeditos.Trim().Length > 0))
                    {
                        resultado = false;
                    }
                }
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron Items Repetidos en el documento lo cual no esta permitido: ";
                objres.obsdetalle = cadena_repeditos;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        private string Cadena_Items_Repetidos(List<itemDataMatriz> tabladetalle)
        {
            bool resultado = true;
            string cadena = "";

            if (tabladetalle.Count > 0)
            {
                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow registro;
                tabla.Columns.Add("coditem", typeof(string));
                tabla.Columns.Add("descripcion", typeof(string));
                tabla.Columns.Add("medida", typeof(string));
                tabla.Columns.Add("veces", typeof(int));

                foreach (var detalle in tabladetalle)
                {
                    // Buscar si ya está
                    bool found = false;
                    foreach (DataRow existingRow in tabla.Rows)
                    {
                        if (existingRow["coditem"].ToString() == detalle.coditem.ToString())
                        {
                            found = true;
                            existingRow["veces"] = (int)existingRow["veces"] + 1;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Si no estaba, añadir
                        registro = tabla.NewRow();
                        registro["coditem"] = detalle.coditem;
                        registro["descripcion"] = detalle.descripcion;
                        registro["medida"] = detalle.medida;
                        registro["veces"] = 1;
                        tabla.Rows.Add(registro);
                    }
                }

                // Borrar de tabla los que tengan menos de dos
                foreach (DataRow row in tabla.Rows.Cast<DataRow>().ToList())
                {
                    if ((int)row["veces"] < 2)
                    {
                        tabla.Rows.Remove(row);
                    }
                }

                if (tabla.Rows.Count > 0)
                {
                    cadena = "";
                    cadena = " ITEM     DESCRIPCION          MEDIDA      VECES\n";
                    cadena += "------------------------------------------------\n";
                    foreach (DataRow row in tabla.Rows)
                    {
                        cadena += " " + row["coditem"] + " " + funciones.Rellenar(row["descripcion"].ToString(), 20, " ", false) + " " + funciones.Rellenar(row["medida"].ToString(), 11, " ", false) + "  " + funciones.Rellenar(row["veces"].ToString(), 3, " ") + "\n";
                    }
                    cadena += "------------------------------------------------\n";
                }
                else
                {
                    cadena = "";
                }
            }
            return cadena;
        }
        private async Task<bool> Control_Valido_C00067Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //##VALIDAR EMPAQUES CERRADOS SEGUN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            objres = await Validar_Empaques_Cerrados_Segun_Lista_Precio(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_Empaques_Cerrados_Segun_Lista_Precio(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string cadena_titulos = "";
            string item = "";
            int i, j;
            decimal cantidad;
            decimal cantidad_total;
            ArrayList items_iguales = new ArrayList();
            bool resultado = true;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            cadena_titulos = "";
            cadena_titulos = " ITEM     DESCRIPCION         MEDIDA             CANTIDAD    " + '\r';
            cadena_titulos += "------------------------------------------------------------------------" + '\r';

            if (await cliente.Controla_empaque_cerrado(_context, DVTA.codcliente_real))
            {
                //Validar empaques Cerrados
                foreach (var detalle in tabladetalle)
                {
                    if (await ventas.Tarifa_EmpaqueCerrado(_context, detalle.codtarifa))
                    {
                        //desde 23-11-2022 implementar que de un item sume la cantidad total de los items repetidos para asi validar si el empaque del item sin descuento cumpla o no el empaque minimo
                        //ya que al realizar la division de las cantidad de un item para cumplir empaque caja cerrada puede haber items q no cumplan el empaque minimo despues de la division'
                        //para esto el item sin descuento empaque cerrada para validar debe sumar su propia cantidad mas la cantidad de caja cerrada y validar el empaque minimo con esa cantidad
                        if (detalle.coddescuento == 0)
                        {
                            //'Dsde 08-12-2022
                            //'1ro verificar si el item que no tiene descuento sin sumar cumple o no el empaquecerrado segun precio
                            if (!await ventas.CumpleEmpaqueCerrado(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, DVTA.codcliente_real))
                            {
                                //2do si no tiene descuento sumar las cantidad de todo el detalle con el mismo item y validar el empaque con ese total
                                cantidad_total = 0;
                                item = detalle.coditem;
                                //sacar items iguales
                                foreach (var detalle2 in tabladetalle)
                                {
                                    if (item == detalle2.coditem)
                                    {
                                        //si es igual sumar la cantidad
                                        cantidad_total = cantidad_total + (decimal)detalle2.cantidad;
                                    }
                                    else
                                    {
                                        cantidad_total = cantidad_total;
                                    }
                                }
                                if (!await ventas.CumpleEmpaqueCerrado(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, cantidad_total, DVTA.codcliente_real))
                                {
                                    resultado = false;
                                    cadena += "\r\n" + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 14, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString(), 12, " ");
                                }
                            }
                        }
                        else
                        {
                            if (!await ventas.CumpleEmpaqueCerrado(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, DVTA.codcliente_real))
                            {
                                resultado = false;
                                cadena += "\r\n" + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 14, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString(), 12, " ");
                            }
                        }
                    }
                }
                cadena += "\r\n" + "------------------------------------------------------------------------" + "\r\n";
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes items no cumplen el empaque cerrado que el precio de venta elegido requiere:";
                objres.obsdetalle = cadena_titulos;
                objres.obsdetalle += "\r\n" + cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Tiene_Cuentas_Por_Pagar_En_Mora(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            string cadena = "";
            string cadena_mora = "";
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            int dias_limite_mora = await configuracion.dias_mora_limite(_context, codempresa);
            //valida solo si es para venta al credito
            if (DVTA.tipo_vta == "CREDITO" || (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.Es_Cliente_Casual(_context, DVTA.codcliente)) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real))
            {
                if (await creditos.ClienteEnMora(_context, DVTA.codcliente_real, codempresa))
                {
                    cadena = "El cliente:" + DVTA.codcliente_real + " tiene cuentas en mora, aun tomando en cuenta sus extensiones, por favor corrija esta situacion antes de grabar para aprobar la proforma(verifique el reporte morosidad por cuotas con extension). El maximo de dias con mora permitido es de: (" + dias_limite_mora + ") dia(s)";
                    cadena_mora = await creditos.Cadena_Notas_De_Remision_En_Mora(_context, DVTA.codcliente_real, codempresa);
                }
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = cadena;
                objres.obsdetalle = cadena_mora;
                objres.datoA = DVTA.estado_doc_vta == "NUEVO" ? DVTA.nitfactura : DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente_real;
                objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Georeferencia_Entrega_Valida(DBContext _context, string latitud, string longitud, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (latitud.Trim().Length == 0 || longitud.Trim().Length == 0)
            {
                cadena += "\n" + (latitud.Trim().Length == 0 ? "La Latitud" : "") + (longitud.Trim().Length == 0 ? "La Longitud" : "") + " de la dirección de entrega es incorrecta!!!";
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Las georeferencias tienen observaciones:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Si_Hay_Descuentos_Extras_Repetidos(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            List<string> listado = new List<string>();
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            string cadena_repetidos = "";

            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //el descuento por depositos si puede estar repetido
            //por eso se excluye y no se controla
            foreach (var descuentos in tabladescuentos)
            {
                if (descuentos.coddesextra != coddesextra_depositos)
                {
                    if (listado.Contains(descuentos.coddesextra.ToString()))
                    {
                        cadena_repetidos += "\n" + descuentos.coddesextra + "-" + descuentos.descrip;
                    }
                    else { listado.Add(descuentos.coddesextra.ToString()); }
                }
            }
            if (cadena_repetidos.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras estan repetidos, lo cual no esta permitido: ";
                objres.obsdetalle = cadena_repetidos;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Direccion_Entrega(DBContext _context, string codcliente_real, string direccion, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.NroDeTiendas(_context, codcliente_real) > 1)
            {
                objres.resultado = false;
                objres.observacion = "Este cliente tiene mas de una tienda. Por favor verifique que ha indicado la direccion correcta, Desea continuar?";
                objres.obsdetalle = "La direccion que eligio es: " + direccion.Trim();
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Contado_Contra_Entrega_Con_Anticipo(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.tipo_vta == "CONTADO" && DVTA.contra_entrega == "SI" && DVTA.pago_con_anticipo == true)
            {

                objres.resultado = false;
                objres.observacion = "Si esta realizando una venta al CONTADO - CONTRA ENTREGA; no debe asignar anticipos, pues existe una contradiccion; la ventas sera contado contra entrega o sera contado con anticipo???";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Contado_Contra_Entrega_Cliente_Del_Interior(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.tipo_vta == "CONTADO")
            {
                if (DVTA.contra_entrega.ToUpper() == "SI")
                {
                    // si es un cliente del interior, no se le puede vender contado contra entrega
                    // implementado desde 29-10-2019
                    if (DVTA.ubicacion == "INTERIOR")
                    {
                        if (string.IsNullOrWhiteSpace(DVTA.estado_contra_entrega))
                        {
                            objres.resultado = false;
                            objres.observacion = "Si está realizando una venta al CONTADO-CONTRA ENTREGA, debe definir el estado de contra entrega!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else if (DVTA.estado_contra_entrega == "POR CANCELAR")
                        {
                            objres.resultado = false;
                            objres.observacion = "Las ventas al CONTADO - CONTRA ENTREGA - POR CANCELAR; a clientes con dirección de entrega al INTERIOR; no están permitidas. Verifique esta situación o ingrese un permiso especial.";
                            objres.obsdetalle = "";
                            objres.datoA = DVTA.estado_doc_vta == "NUEVO" ? DVTA.nitfactura : DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente;
                            objres.datoB = "Total: " + DVTA.totaldoc + "(" + DVTA.codmoneda + ")";
                            objres.accion = Acciones_Validar.Pedir_Servicio;
                        }
                        else
                        {
                            objres.resultado = false;
                            objres.observacion = "¿Está seguro de realizar esta venta al CONTADO - CONTRA ENTREGA - YA CANCELÓ a un cliente del interior?";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Confirmar_SiNo;
                        }
                    }
                    else if (DVTA.ubicacion == "LOCAL")
                    {
                        objres.resultado = false;
                        objres.observacion = "¿Está seguro de realizar esta venta al CONTADO - CONTRA ENTREGA?";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                    else
                    {
                        objres.resultado = false;
                        objres.observacion = "La ubicación de la dirección de entrega del pedido no es válida. Verifique esta situación!!!";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Contado_NOCE_Tenga_Anticipo(DBContext _context, DatosDocVta DVTA, List<vedetalleanticipoProforma> dt_anticipo_pf, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.tipo_vta == "CONTADO")
            {
                if (DVTA.contra_entrega.ToUpper() == "NO")
                {
                    // SI SE HABILITO LA OPCION DE PAGO ANTICIPADO
                    if (DVTA.pago_con_anticipo)
                    {
                        if (dt_anticipo_pf.Count == 0)
                        {
                            objres.resultado = false;
                            objres.observacion = "Toda venta al contado debería tener asignado uno o más anticipos que cubran el monto total de la proforma, y esta proforma no tiene ningún anticipo, por lo tanto no podrá grabar esta proforma!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                    }
                    else
                    {
                        // alertar si es contado y no se asignó ningún anticipo
                        objres.resultado = false;
                        objres.observacion = "Toda venta al contado debería tener asignado uno o más anticipos que cubran el monto total de la proforma, y esta proforma no tiene ningún anticipo, por lo tanto no podrá grabar esta proforma!!!";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Otro_Medio_de_Transporte(DBContext _context, string transporte, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //si el medio de transporte elegido es: OTRO, pedir clave
            if (transporte == "OTRO")
            {
                objres.resultado = false;
                objres.observacion = "El medio de transporte que eligio es: OTROS, lo cual requiere permiso especial para aprobar con este medio de transporte!!!";
                objres.obsdetalle = "";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Quien_Cancelara_Flete(DBContext _context, string fletepor, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (fletepor.Trim().Length == 0)
            {
                objres.resultado = false;
                objres.observacion = "Debe elegir quien cancelara el flete!!!";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Nombre_Transporte_Valido_Para_Entregar_Pedido(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.transporte.Trim().Length > 0 && DVTA.nombre_transporte.Trim().Length == 0)
            {
                objres.resultado = false;
                objres.observacion = "Ha elegido un medio de transporte, pero no ha definido el nombre del transporte, desea continuar de todas formas?";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Tipo_Transporte_Valido_Para_Entregar_Pedido(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //Si no cumple el monto para:ENTREGAR, entonces no puede elegir estos medios de transporte (FLOTA, TRANSPORTADORA,CAMION PERTEC)
            if ((DVTA.transporte == "FLOTA" || DVTA.transporte == "TRANSPORTADORA" || DVTA.transporte == "CAMION PERTEC") && DVTA.tipoentrega == "ENTREGAR")
            {
                ResultadoValidacion objres1 = new ResultadoValidacion();

                objres1.resultado = true;
                objres1.observacion = "";
                objres1.obsdetalle = "";
                objres1.datoA = "";
                objres1.datoB = "";
                objres1.accion = Acciones_Validar.Ninguna;

                objres1 = await Validar_Monto_Minimo_Para_Entrega_Pedido(_context, DVTA, tabladetalle, codempresa);
                if (objres1.resultado == false)
                {
                    objres.resultado = false;
                    objres.observacion = "Ha elegido el medio de transporte: " + DVTA.transporte + " y este pedido no cumple con el monto minimo para entregar, por tanto el medio de transporte elegido no es correcto!!!";
                    objres.obsdetalle = objres1.obsdetalle;
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Descuentos_Extras_Aplicados_Validos(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool resultado = true;
            bool _contra_entrega = DVTA.contra_entrega == "SI" ? true : false;
            int i, j = 0;
            string cadena = "";
            string cadena0 = "";
            string cadena1 = "";
            string cadena2 = "";
            string cadena3 = "";
            string cadena4 = "";

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            // Verifica si es una venta CONTADO contra entrega, no se asigne los decuentos que son de VENTA CREDITO (1,8,24,25)
            foreach (var descuentos in tabladescuentos)
            {
                if (!await restricciones.Validar_Contraentrega_Descuento(_context, _contra_entrega, descuentos.coddesextra))
                {
                    cadena0 += "\nNo se puede aplicar el descuento:" + descuentos.coddesextra + " -" + descuentos.descrip + " a una venta Contra Entrega.";
                    resultado = false;
                }
            }

            if (cadena0.Trim().Length > 0)
            {
                cadena0 += "\n----------------------------------------------";
            }

            // Verificar si el cliente tiene asignado el descuento extra (vecliente_desextra)
            foreach (var descuentos in tabladescuentos)
            {
                // Cliente_Tiene_Descto_Extra_Asignado
                if (!await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, descuentos.coddesextra, DVTA.codcliente_real))
                {
                    if (cadena1.Trim().Length == 0)
                    {
                        cadena1 = "El cliente: " + DVTA.codcliente_real + " no tiene asignado DescExtra: ";
                    }
                    cadena1 += "\n" + descuentos.coddesextra.ToString() + " - " + descuentos.descrip;
                    resultado = false;
                }
            }

            if (cadena1.Trim().Length > 0)
            {
                cadena1 += "\n----------------------------------------------";
            }

            // Verificar si: DVTA.codcliente es dfte de: DVTA.codcliente_real, si dfte entonces validar del otro cliente
            // implementado en fecha: 30-11-2021
            if (!DVTA.codcliente.Equals(DVTA.codcliente_real))
            {
                foreach (var descuentos in tabladescuentos)
                {
                    // Cliente_Tiene_Descto_Extra_Asignado
                    if (!await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, descuentos.coddesextra, DVTA.codcliente))
                    {
                        if (cadena1.Trim().Length == 0)
                        {
                            cadena1 = "El cliente Sin Nombre: " + DVTA.codcliente + " no tiene asignado DescExtra: ";
                        }
                        cadena1 += "\n" + descuentos.coddesextra.ToString() + " - " + descuentos.descrip;
                        resultado = false;
                    }
                }

                if (cadena1.Trim().Length > 0)
                {
                    cadena1 += "\n----------------------------------------------";
                }
            }

            // Verificar el detalle de los items
            List<int> lista_precios = new List<int>();

            foreach (var descuentos in tabladetalle)
            {
                if (!lista_precios.Contains(descuentos.codtarifa))
                {
                    lista_precios.Add(descuentos.codtarifa);
                }
            }

            int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, lista_precios);
            List<int> lista_precios_para_validar = new List<int>();

            lista_precios_para_validar.Clear();
            lista_precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

            foreach (var descuentos in tabladescuentos)
            {
                foreach (int precio in lista_precios_para_validar)
                {
                    // Verificar si el precio - tiene habilitado el DESCTO (vedesextra_tarifa)
                    if (!await ventas.TarifaValidaDescuento(_context, precio, descuentos.coddesextra))
                    {
                        if (cadena2.Trim().Length == 0)
                        {
                            cadena2 = "DesExtra no tiene habilitados precios(vedesextra_tarifa):";
                        }
                        cadena2 += "\nDesExtra: " + descuentos.coddesextra + "  --> Precio: " + precio;
                        resultado = false;
                    }
                }
            }

            if (cadena2.Trim().Length > 0)
            {
                cadena2 += "\n----------------------------------------------";
            }

            // Obtener los descuentos especiales que se están aplicando
            List<int> lista_descesp = new List<int>();

            foreach (var descuentos in tabladetalle)
            {
                if (!lista_descesp.Contains(descuentos.coddescuento))
                {
                    lista_descesp.Add(descuentos.coddescuento);
                }
            }

            // Verificar el detalle de los items
            foreach (var descuentos in tabladescuentos)
            {
                foreach (int descesp in lista_descesp)
                {
                    // Verificar si el DESCTO ESPECIAL - tiene habilitado el DESCTO (vedesextra_descuento)
                    if (!await ventas.DescuentoEspecialValidoDescuento(_context, descesp, descuentos.coddesextra))
                    {
                        if (cadena3.Trim().Length == 0)
                        {
                            cadena3 = "DescExtra no tienen habilitado el DescEspecial(vedesextra_descuento):";
                        }
                        cadena3 += "\nDesExtra: " + descuentos.coddesextra + " --> DesEsp: " + descesp;
                        resultado = false;
                    }
                }
            }

            if (cadena3.Trim().Length > 0)
            {
                cadena3 += "\n----------------------------------------------";
            }

            // Obtener los descuentos especiales que se están aplicando
            List<int> lista_desextra = new List<int>();

            foreach (var descuentos in tabladescuentos)
            {
                if (!lista_desextra.Contains(descuentos.coddesextra))
                {
                    lista_desextra.Add(descuentos.coddesextra);
                }
            }

            foreach (int desextra in lista_desextra)
            {
                foreach (var descuentos in tabladetalle)
                {
                    // Verificar si el item tiene configurado el descuento extra (vedesextra_item)
                    if (!await ventas.DescuentoExtra_ItemValido(_context, descuentos.coditem, desextra))
                    {
                        if (cadena4.Trim().Length == 0)
                        {
                            cadena4 = "Item no tiene habilitado el DesExtra(vedesextra_item):";
                        }
                        cadena4 += "\n- " + descuentos.coditem.ToString() + " DescExtra:" + desextra + "-" + await nombres.nombredesextra(_context, desextra);
                        resultado = false;
                    }
                }
            }

            if (cadena4.Trim().Length > 0)
            {
                cadena4 += "\n----------------------------------------------";
            }

            if (resultado == false)
            {
                cadena = cadena0;
                cadena += "\n\n" + cadena1;
                cadena += "\n\n" + cadena2;
                cadena += "\n\n" + cadena3;
                cadena += "\n\n" + cadena4;
                objres.resultado = false;
                objres.observacion = "Se encontraron las siguientes observaciones en la aplicacion de los descuentos extras:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_NIT_Valido(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string minomcliente = "";

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            minomcliente = DVTA.nombcliente.Trim().Replace(" ", "");
            if (minomcliente == "SINNOMBRE")
            {
                DVTA.nitfactura = "0";
            }
            else
            {
                (bool esValido, string mensaje) = await ventas.Validar_NIT_Correcto(_context, DVTA.nitfactura, DVTA.tipo_doc_id);
                if (!esValido)
                {
                    objres.resultado = false;
                    objres.observacion = "Debe indicar un N.I.T. o Nro de C.I. Valido.";
                    objres.obsdetalle = mensaje;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Credito_NIT_Dfte_Cero(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            DVTA.nitfactura = DVTA.nitfactura.Trim();
            if (DVTA.tipo_vta == "CREDITO")
            {
                //VENTA CREDITO
                if (funciones.EsNumero(DVTA.nitfactura))
                {
                    if (double.Parse(DVTA.nitfactura) == 0)
                    {
                        objres.resultado = false;
                        objres.observacion = "No se puede realizar una venta al credito con N.I.T. o Nro de C.I. igual a cero.";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                else
                {
                    objres.resultado = false;
                    objres.observacion = "Para realizar una venta al credito, debe indicar un N.I.T. o Nro de C.I. Valido.";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }

            }
            else
            {
                (bool esValido, string mensaje) = await ventas.Validar_NIT_Correcto(_context, DVTA.nitfactura, DVTA.tipo_doc_id);
                if (!esValido)
                {
                    objres.resultado = false;
                    objres.observacion = "Debe indicar un N.I.T. o Nro de C.I. Valido.";
                    objres.obsdetalle = mensaje;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Limite_Factura_Sin_Nombre(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            string minomcliente = "";
            string mon_base = await empresa.monedabase(_context, codempresa);
            double monto_conversion = (double)await tipocambio._conversion(_context, mon_base, DVTA.codmoneda, DVTA.fechadoc.Date, (decimal)DVTA.totaldoc);
            string codmoneda_monto_max_vta_snomb = await cliente.Maximo_Vta_Moneda(_context, DVTA.codcliente, codempresa, usuario);
            double montomax_facturas_sinnombre = (double)await configuracion.monto_maximo_facturas_sin_nombre(_context, codempresa);
            string cadena_msg = "";
            string cadena = "";
            cadena = " ";
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //si la venta es mayor al permitido para facturas sin nombre alertar como debe ser
            //consignado el nombre de la factura y el nit

            if (monto_conversion >= montomax_facturas_sinnombre && DVTA.nitfactura == "0" && DVTA.nombcliente == "SIN NOMBRE")
            {
                cadena_msg = "Como esta venta es mayor o igual a: " + montomax_facturas_sinnombre + " (" + codmoneda_monto_max_vta_snomb + ") " + Environment.NewLine;
                cadena_msg += " deben llenarse obligatoriamente los datos del comprador: " + Environment.NewLine;
                cadena_msg += " -Para sociedades o entidades, colocar el nombre comercial " + Environment.NewLine;
                cadena_msg += "  registrado en Fundempresa y luego el NIT." + Environment.NewLine;
                cadena_msg += " -Para empresas unipersonales que usan el nombre del propietario, " + Environment.NewLine;
                cadena_msg += "  o personas naturales se debe colocar el nombre completo  " + Environment.NewLine;
                cadena_msg += "  o por lo menos el primer apellido. Luego el NIT o CI. " + Environment.NewLine;
                cadena_msg += " -Para personas no obligadas a tener NIT o personas extranjeras:  " + Environment.NewLine;
                cadena_msg += "  En el lugar de Señores: colocar la entidad o persona," + Environment.NewLine;
                cadena_msg += "  y en la parte NIT/CI: colocar el numero: 99001 " + Environment.NewLine;
            }

            if (monto_conversion >= montomax_facturas_sinnombre)
            {
                if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente))
                {
                    if (!funciones.EsNumero(DVTA.nitfactura))
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre + " (MN) debe ser facturada a un NIT válido.";
                    }

                    if (DVTA.nombcliente.Replace(" ", "") == "SINNOMBRE")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre + " (MN) debe ser facturada a un nombre válido.";
                    }

                    if (DVTA.nitfactura == "0")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre + " (MN) debe ser facturada a un NIT válido.";
                    }
                }
                else
                {
                    if (DVTA.nombcliente.Replace(" ", "") == "SINNOMBRE")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre + " (MN) debe ser facturada a un nombre válido.";
                    }

                    if (DVTA.nitfactura.Trim() == "0")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre + " (MN) debe ser facturada a un NIT válido.";
                    }
                }
            }
            if (cadena_msg.Trim().Length > 0 || cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = cadena_msg;
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }


        public async Task<ResultadoValidacion> Validar_NIT_Es_Cliente_CompetenciaAsync(DBContext _context, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //var es_cliente_competencia = await Cliente.EsClienteCompetencia(userConnectionString, DVTA.nitfactura);
            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                objres.resultado = false;
                objres.observacion = ("El NIT: " + (DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia, necesita permiso especial para dar continuar!!!"));
                objres.obsdetalle = "";
                if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente + "-" + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    objres.datoB = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }

                objres.accion = Acciones_Validar.Pedir_Servicio;
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Tipo_ClienteAsync(DBContext _context, string codcliente)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string _tipo = "";
            bool _esgrupo = false;
            string _cadena = "";
            string _obs = "";

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            List<string> liguales = new List<string>();
            liguales = await cliente.CodigosIgualesListAsync(_context, codcliente);

            if ((liguales.Count > 1))
            {
                _esgrupo = true;
                _obs = "El cliente pertenece a un grupo de clientes con observacion:";
            }
            else
            {
                _esgrupo = false;
                _obs = "El cliente tiene observacion:";
            }
            for (int i = 0; (i <= (liguales.Count - 1)); i++)
            {
                _tipo = await cliente.Tipo_Cliente(_context, liguales[i], false);
                if ((_tipo == "NORMAL"))
                {

                }
                else
                {
                    _cadena += "\r\n" + "El cliente: " + liguales[i] + " es tipo: " + _tipo;
                }

            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_De_LineaAsync(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                bool Cliente_Competencia_Permite_Descto_Linea = await cliente.ClienteCompetenciaPermiteDesctoLinea(_context, DVTA.nitfactura);
                if (ProformaTieneDescuentoLinea(tabladetalle) && !Cliente_Competencia_Permite_Descto_Linea)
                {
                    objres.resultado = false;
                    objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                    objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos de linea.";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_ProveedorAsync(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                bool Cliente_Competencia_Permite_Descto_Proveedor = await cliente.ClienteCompetenciaPermiteDesctoProveedor(_context, DVTA.nitfactura);
                if (ProformaTieneDescuentoEspeciales(tabladetalle) && !Cliente_Competencia_Permite_Descto_Proveedor)
                {
                    objres.resultado = false;
                    objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                    objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos especiales Proveedor.";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_VolumnenAsync(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                bool Cliente_Competencia_Permite_Descto_Volumen = await cliente.ClienteCompetenciaPermiteDesctoVolumen(_context, DVTA.nitfactura);
                if (ProformaTieneDescuentoEspeciales(tabladetalle) && !Cliente_Competencia_Permite_Descto_Volumen)
                {
                    objres.resultado = false;
                    objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                    objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos especiales Volumen.";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_PromocionAsync(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                List<string> lista_promos_aplicadas = new List<string>();
                int coddesextra_depo = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                lista_promos_aplicadas = await ventas.DescuentosPromocionAplicadosAsync(_context, tabladescuentos, coddesextra_depo);
                if (lista_promos_aplicadas.Count > 0)
                {
                    bool Cliente_Competencia_Permite_Descto_Promocion = await cliente.ClienteCompetenciaPermiteDesctoPromocion(_context, DVTA.nitfactura);
                    if (!Cliente_Competencia_Permite_Descto_Promocion)
                    {
                        objres.resultado = false;
                        objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                        objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos de Promocion.";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_ExtraAsync(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                if (await Proforma_Tiene_Descuentos_ExtrasAsync(_context, tabladescuentos))
                {
                    bool Cliente_Competencia_Permite_Descto_Extra = await cliente.ClienteCompetenciaPermiteDesctoExtra(_context, DVTA.nitfactura);
                    if (!Cliente_Competencia_Permite_Descto_Extra)
                    {
                        objres.resultado = false;
                        objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                        objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos extras adicionales.";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Descuentos_Extras_Habilitados(DBContext _context, List<vedesextraDatos> tabladescuentos)
        {
            string cadena = "";
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = cadena;
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            foreach (var dato in tabladescuentos)
            {
                if (await ventas.Descuento_Extra_Habilitado(_context, dato.coddesextra) == false)
                {
                    if (cadena.Trim().Length == 0)
                    {
                        cadena = dato.coddesextra + " - " + await nombres.nombredesextra(_context, dato.coddesextra);
                    }
                    else
                    {
                        cadena += Environment.NewLine + " - " + dato.coddesextra + await nombres.nombredesextra(_context, dato.coddesextra);
                    }

                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras estan actualmente deshabilitados:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuentos_De_Nivel_Segun_SolicitudAsync(DBContext _context, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //PERMITIR PROFORMA CON DESCTOS DE NIVEL SEGUN SOLICITUD
            if (DVTA.desclinea_segun_solicitud == true)
            {
                if (DVTA.idsol_nivel.Trim().Length == 0 || DVTA.nroidsol_nivel.Trim().Length == 0)
                {
                    objres.observacion = "Ha elegido otorgar descuentos de nivel segun solicitud, debe identificar correctamente el ID-Numeroid de la solicitud!!!";
                    objres.resultado = false;
                    objres.accion = Acciones_Validar.Ninguna;
                }
                //verificar si la solicitud de descuentos de linea existe
                if (objres.resultado == true)
                {
                    bool existe_sol_Descuento_Nivel = await ventas.Existe_Solicitud_Descuento_Nivel(_context, DVTA.idsol_nivel, int.Parse(DVTA.nroidsol_nivel));
                    if (!existe_sol_Descuento_Nivel)
                    {
                        objres.observacion = "Ha elegido utilizar la solicitud de descuentos de nivel: " + DVTA.idsol_nivel.Trim() + "-" + DVTA.nroidsol_nivel.Trim() + " para aplicar descuentos de linea, pero la solicitud indicada no existe!!!";
                        objres.resultado = false;
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                //verificar que el codigo de cliente real de la proforma sea el mismo al de la solicitud
                if (objres.resultado == true)
                {
                    string cliente_Sol_Descuento_Nivel = await ventas.Cliente_Solicitud_Descuento_Nivel(_context, DVTA.idsol_nivel, int.Parse(DVTA.nroidsol_nivel));
                    if (!(cliente_Sol_Descuento_Nivel == DVTA.codcliente))
                    {
                        objres.observacion = "La solicitud de descuentos de nivel: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " a la que hace referencia no pertenece al mismo cliente de esta proforma!!!";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.resultado = false;
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                //validar si la solicitud de descuentos ya fue utilizada en otra proforma
                if (objres.resultado == true)
                {
                    bool Sol_ya_Utilizada = await ventas.Solicitud_Descuento_Ya_Utilizada(_context, DVTA.idsol_nivel, DVTA.nroidsol_nivel);
                    if (Sol_ya_Utilizada)
                    {
                        //obtiene el id-nroid de la proforma en la cual se utilizo ya la solicitud
                        string doc_pf = await ventas.ProformaConSolicitudDescuentoYaUtilizada(_context, DVTA.idsol_nivel, DVTA.nroidsol_nivel);
                        //SI ES PROFORMA NUEVA
                        if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                        {
                            objres.observacion = "La solicitud de descuentos: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " ya fue utilizada en la proforma:" + doc_pf + " por tanto no puede utilizar en esta proforma!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.resultado = false;
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else
                        {
                            //EDITAR
                            //si se esta modificando la proforma y es la misma en la cual se aplico la sol de descuentos de nivel
                            //entonces esta bien
                            if (doc_pf == DVTA.id.Trim() + "-" + DVTA.numeroid.Trim())
                            {
                                objres.observacion = "";
                                objres.obsdetalle = "";
                                objres.datoA = "";
                                objres.datoB = "";
                                objres.resultado = true;
                                objres.accion = Acciones_Validar.Ninguna;
                            }
                            else
                            {
                                objres.observacion = "La solicitud de descuentos: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " ya fue utilizada en la proforma: " + doc_pf + " por tanto no puede utilizar en esta proforma!!!";
                                objres.obsdetalle = "";
                                objres.datoA = "";
                                objres.datoB = "";
                                objres.resultado = false;
                                objres.accion = Acciones_Validar.Ninguna;
                            }

                        }

                    }
                }
                //pedir la clave para dar curso a proforma con descuentos de nivel segun solicitud
                if (objres.resultado == true)
                {
                    objres.observacion = "Ha elegido aplicar descuentos de linea segun solicitud: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " lo cual requiere permiso especial, ingrese el permiso especial!!!";
                    objres.obsdetalle = "";
                    objres.resultado = false;
                    if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                    {
                        objres.datoA = DVTA.codcliente + "-" + DVTA.codcliente_real + " " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel;
                    }
                    else
                    {
                        objres.datoA = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel;
                    }

                    objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }


            }
            return objres;
        }
        public bool ProformaTieneDescuentoLinea(List<itemDataMatriz> tabladetalle)
        {
            if (!tabladetalle.Any(item => item.porcendesc != 0.0f))
            {
                //EN EL CASO DE LAS TIENDAS NO SE TIENE ESTA COLUMNA EN LA FACTURACION DE MOSTRADOR
                //ENTONCES SI NO TIENE ESTA COLUMNA DE HECHO NO HAY DESCTO DE LINEA
                return false;
            }
            else
            {
                //if (tabladetalle.Any(item => item.porcen > 0))
                //{
                //    return true;
                //}
                foreach (var item in tabladetalle)
                {
                    if (item.porcendesc > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool ProformaTieneDescuentoEspeciales(List<itemDataMatriz> tabladetalle)
        {

            foreach (var item in tabladetalle)
            {
                if (item.coddescuento != 0)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task<bool> Proforma_Tiene_Descuentos_ExtrasAsync(DBContext _context, List<vedesextraDatos> tabladescuentos)
        {

            foreach (var item in tabladescuentos)
            {
                //si no es uno de promocion es un descuento extra
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, item.coddesextra))
                {
                    return true;
                }
            }
            return false;
        }
        public async Task<ResultadoValidacion> Validar_Vigencia_DesctosExtras_Segun_FechasAsync(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DateTime valido_desdeF;
            DateTime valido_hastaF;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            foreach (var descuentos in tabladescuentos)
            {
                //obtener desde que fecha esta habilitado el descto extra
                valido_desdeF = await ventas.Descuento_Extra_Valido_Desde_Fecha(_context, descuentos.coddesextra);
                //verificar si la fecha de la proforma es posterior a la habilitacion del descto
                if (DVTA.fechadoc.Date < valido_desdeF.Date)
                {
                    cadena += Environment.NewLine + "El Desc: " + descuentos.coddesextra + "-" + descuentos.descrip + " es válido desde fecha: " + valido_desdeF.ToShortDateString();
                }

                valido_hastaF = await ventas.Descuento_Extra_Valido_Hasta_Fecha(_context, descuentos.coddesextra);
                //verificar si la fecha de la proforma es posterior a la habilitacion del descto
                if (DVTA.fechadoc.Date > valido_hastaF.Date)
                {
                    cadena += Environment.NewLine + "El Desc: " + descuentos.coddesextra + "-" + descuentos.descrip + " es válido hasta fecha: " + valido_hastaF.ToShortDateString();
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras aplicados tienen observaciones en sus fechas de vigencia:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Promociones_Por_Aplicar(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<itemDataMatriz> tabladetalle, bool notificar_sinohay, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_aplicados = "";
            string cadena_aplicable = "";
            string por_aplicar = "";
            string niveles_por_aplicar = "";
            DataTable dt_promo = new DataTable();

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (DVTA.codtarifadefecto.ToString().Trim().Length == 0)
            {
                objres.resultado = false;
                objres.observacion = "Debe indicar el precio para el cual se verificara que descuentos-promociones se pueden aplicar.";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                return objres;
            }
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{

            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            List<string> lista_promos_aplicadas = new List<string>();
            lista_promos_aplicadas = await ventas.DescuentosPromocionAplicadosAsync(_context, tabladescuentos, coddesextra_depositos);

            List<string> lista_items = new List<string>();
            lista_items = ListaItemsPedido(tabladetalle);

            List<string> lista_lineas = new List<string>();
            lista_lineas = await ventas.AlinearItemsAsync(_context, lista_items);


            DataTable dt_promo_aux = new DataTable();

            dt_promo.Columns.Add("codgrupo", typeof(string));
            dt_promo.Columns.Add("codlinea", typeof(string));
            dt_promo.Columns.Add("desclinea", typeof(string));
            dt_promo.Columns.Add("nivel", typeof(string));
            dt_promo.Columns.Add("promo_aplicado", typeof(string));
            dt_promo.Columns.Add("promo_aplicable", typeof(string));
            dt_promo.Columns.Add("por_aplicar", typeof(string));
            dt_promo.Columns.Add("nivel_sugerido", typeof(string));
            dt_promo.Columns.Add("nivel_por_aplicar", typeof(string));

            List<string> lista_aplicables_por_linea = new List<string>();

            foreach (string linea in lista_lineas)
            {
                lista_aplicables_por_linea.Clear();
                lista_aplicables_por_linea = await ventas.ListaPromocionesPorLineaAplicablesAsync(_context, linea, DVTA.codtarifadefecto, DVTA.codcliente_real, DVTA.fechadoc, DVTA.subtotaldoc, DVTA.codmoneda, DVTA.tipo_vta);

                cadena_aplicados = "";
                cadena_aplicable = "";
                por_aplicar = "";

                foreach (var promocion in lista_aplicables_por_linea)
                {
                    cadena_aplicable += (cadena_aplicable.Length == 0) ? promocion : ", " + promocion;

                    if (!lista_promos_aplicadas.Contains(promocion))
                    {
                        por_aplicar += (por_aplicar.Length == 0) ? promocion : ", " + promocion;
                    }

                    if (lista_promos_aplicadas.Contains(promocion))
                    {
                        cadena_aplicados += (cadena_aplicados.Length == 0) ? promocion : ", " + promocion;
                    }
                }
                DataRow nuevoreg = dt_promo.NewRow();
                List<string> milista_niveles = new List<string> { "A", "B", "C", "D", "E" };
                string nivel_actual = "";

                nuevoreg["codgrupo"] = await items.lineagrupo(_context, linea);
                nuevoreg["codlinea"] = linea;
                nuevoreg["desclinea"] = await nombres.nombrelinea(_context, linea);
                nuevoreg["nivel"] = await cliente.NivelDescclienteLinea(_context, DVTA.codcliente_real, linea, DVTA.codtarifadefecto);
                nuevoreg["nivel_sugerido"] = await cliente.NiveldescClientesugeridoSegunAuditoria(_context, DVTA.codcliente_real, linea, DVTA.codtarifadefecto);
                nuevoreg["promo_aplicado"] = cadena_aplicados;
                nuevoreg["promo_aplicable"] = cadena_aplicable;
                nuevoreg["por_aplicar"] = por_aplicar;

                if (nuevoreg["nivel"].ToString() == "Z" || nuevoreg["nivel"].ToString() == "X")
                    nivel_actual = "";
                else
                    nivel_actual = nuevoreg["nivel"].ToString();

                if ((nuevoreg["nivel_sugerido"].ToString() != "Z" || nuevoreg["nivel_sugerido"].ToString() != "X") && (nivel_actual.CompareTo(nuevoreg["nivel_sugerido"]) < 0) && milista_niveles.Contains(nuevoreg["nivel_sugerido"].ToString()))
                    nuevoreg["nivel_por_aplicar"] = nuevoreg["nivel_sugerido"];
                else
                    nuevoreg["nivel_por_aplicar"] = "";

                dt_promo.Rows.Add(nuevoreg);
            }
            DataRow reg;

            dt_promo_aux = dt_promo.Copy();
            dt_promo_aux.Clear();
            niveles_por_aplicar = "";
            foreach (DataRow reg_loopVariable in dt_promo.Rows)
            {
                reg = reg_loopVariable;

                // Si se quiere obligar a dar descuento de línea con los niveles sugeridos, esta es la línea de código a habilitar
                // La cual se deshabilita por instrucción de JRA en fecha 19-04-2022
                // If (CStr(reg["por_aplicar"]).Trim().Length > 0) Or CStr(reg["nivel_por_aplicar"]).Trim().Length > 0 Then

                // Solo debe obligar a dar descuentos extras por instrucción de JRA en fecha tb 19-04-2022
                if (Convert.ToString(reg["por_aplicar"]).Trim().Length > 0)
                {
                    dt_promo_aux.ImportRow(reg);
                    //if (niveles_por_aplicar.Trim().Length == 0)
                    //{
                    //    niveles_por_aplicar = Convert.ToString(reg["nivel_por_aplicar"]);
                    //}
                    //else
                    //{
                    //    niveles_por_aplicar = ", " + Convert.ToString(reg["nivel_por_aplicar"]);
                    //}
                }
            }

            dt_promo.Clear();
            dt_promo = dt_promo_aux.Copy();
            dt_promo_aux.Clear();
            //}
            string cadena_msg = "";
            cadena_msg = ventas.MostrarMensajesPromocionesAplicar(dt_promo);
            if (cadena_msg.Trim().Length > 0)
            {
                objres.resultado = false;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente_real;
                    objres.datoB = "DESC/NIV NO APLICADOS: " + por_aplicar + " " + niveles_por_aplicar;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                    objres.datoB = "DESC/NIV NO APLICADOS: " + por_aplicar + " " + niveles_por_aplicar;
                }

                objres.accion = Acciones_Validar.Pedir_Servicio;
                objres.observacion = "Se verifico que existen Promociones Por Aplicar y/o Descuentos de Linea Por Aplicar:";
                objres.obsdetalle = cadena_msg;
            }
            else
            {
                objres.resultado = true;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                objres.observacion = "";
                objres.obsdetalle = "";
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuento_Por_Deposito(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string resultado_cadena = "";
            string cadena = "";
            int codesextradeposito = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            string[] doc_cbza = new string[2];
            DateTime fecha_cbza;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //esta es la fecha en la que JRA Y GVR enviaron corrreo indicando que todo deposito desde esta fecha
            //es aplicable solo si el cliente tiene linea de credito regularizada, vigente y no revertida
            DateTime mifecha = new DateTime(2015, 6, 3);
            foreach (var descuentos in tabladescuentos)
            {
                if (descuentos.coddesextra == codesextradeposito)
                {
                    // Primero verificar si el depósito existe
                    doc_cbza[0] = "";
                    doc_cbza[1] = "";

                    // Si es cobza credito
                    if (descuentos.codcobranza != 0)
                        doc_cbza = await cobranzas.Id_Nroid_Cobranza(_context, descuentos.codcobranza);
                    else if (descuentos.codcobranza_contado != 0)
                        doc_cbza = await cobranzas.Id_Nroid_Cobranza_Contado(_context, descuentos.codcobranza_contado);
                    else if (descuentos.codanticipo != 0)
                        doc_cbza = await cobranzas.Id_Nroid_Anticipo(_context, descuentos.codanticipo);

                    if (doc_cbza == null)
                    {
                        resultado_cadena += "\nLa cobranza-deposito: " + doc_cbza[0] + "-" + doc_cbza[1] + " No existe por tanto no se puede aplicar el descuento por deposito.";
                    }

                    // Si es cobza CREDITO
                    if (descuentos.codcobranza != 0)
                    {
                        if (await cobranzas.Existe_Cobranza(_context, doc_cbza[0], doc_cbza[1]) == true)
                        {
                            fecha_cbza = await cobranzas.Fecha_De_Cobranza(_context, doc_cbza[0], doc_cbza[1]);
                            // validar_descuento por deposito, si el desposito es posterior al 03 - 06 - 2105
                            //debe controlarse si el cliente tiene linea de credito valida
                            //si el cliente deberia tener linea de credito valida
                            if (await ventas.Descuento_Extra_Valida_Linea_Credito(_context, descuentos.coddesextra) == true)
                            {
                                //validar que el cliente tenga linea de credito, vigente no revertida
                                if (!await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, DVTA.codcliente_real))
                                {
                                    resultado_cadena += "\nEl descuento por deposito que corresponde a la cobranza-deposito:" + doc_cbza[0] + "-" + doc_cbza[1] + " no puede ser añadido, porque todo deposito de cliente posterior al 03-06-2015 que se quiera aplicar debe tener linea de credito Fija regularizada.";
                                }
                            }
                        }
                        else
                        {
                            resultado_cadena += "\nLa cobranza-deposito: " + doc_cbza[0] + "-" + doc_cbza[1] + " No existe por tanto no se puede aplicar el descuento por deposito.";
                            break;
                        }
                    }

                    // Si es cobza CONTADO
                    if (descuentos.codcobranza_contado != 0)
                    {
                        if (await cobranzas.Existe_Cobranza_Contado(_context, doc_cbza[0], doc_cbza[1]) == true)
                        {
                            fecha_cbza = await cobranzas.Fecha_De_Cobranza_Contado(_context, doc_cbza[0], doc_cbza[1]);
                            //validar_descuento por deposito, si el desposito es posterior al 03-06-2105
                            //debe controlarse si el cliente tiene linea de credito valida
                            if (fecha_cbza > mifecha)
                            {
                                if (await ventas.Descuento_Extra_Valida_Linea_Credito(_context, descuentos.coddesextra) == true)
                                {
                                    if (!await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, DVTA.codcliente_real))
                                    {
                                        resultado_cadena += "\nEl descuento por deposito que corresponde a la Cobranza Contado-Deposito:" + doc_cbza[0] + "-" + doc_cbza[1] + " no puede ser añadido, porque todo deposito de cliente posterior al 03-06-2015 que se quiera aplicar debe tener linea de credito Fija regularizada.";
                                    }
                                }
                            }
                        }
                        else
                        {
                            resultado_cadena += "\nLa Cobranza Contado-Deposito: " + doc_cbza[0] + "-" + doc_cbza[1] + " No existe por tanto no se puede aplicar el descuento por deposito.";
                            break;
                        }
                    }
                }
            }
            if (resultado_cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuento_Extra_Requiere_Credito_Valido(DBContext _context, string codcliente_real, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string resultado_cadena = "";
            string cadena = "";
            bool requiere_credito = false;
            bool es_cliente_pertec = false;
            bool cliente_tiene_credito = false;
            bool tiene_casa_matriz = false;
            bool tiene_casa_matriz_nal = false;
            //esta es la fecha en la que JRA Y GVR enviaron corrreo indicando que todo deposito desde esta fecha
            //es aplicable solo si el cliente tiene linea de credito regularizada, vigente y no revertida
            DateTime mifecha = new DateTime(2015, 6, 3);
            string codigoPrincipal = "";
            string codigoPrincipal_nal = "";

            codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente_real);
            codigoPrincipal_nal = await cliente.CodigoPrincipal_Nacional(_context, codcliente_real);

            if (!(codcliente_real == codigoPrincipal))
            {
                tiene_casa_matriz = true;
            }

            if (!(codcliente_real == codigoPrincipal_nal))
            {
                tiene_casa_matriz = true;
            }

            //Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
            if ((cliente.NIT(_context, codigoPrincipal) == cliente.NIT(_context, codcliente_real)))
            { }
            else
            {
                codigoPrincipal = codcliente_real;
            }
            es_cliente_pertec = await cliente.EsClientePertec(_context, codigoPrincipal);
            cliente_tiene_credito = await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, codcliente_real);

            foreach (var descuentos in tabladescuentos)
            {
                requiere_credito = await ventas.Descuento_Extra_Valida_Linea_Credito(_context, descuentos.coddesextra);
                if (requiere_credito)
                {
                    if (es_cliente_pertec || cliente_tiene_credito)
                    { }
                    else
                    {
                        cadena = "El descuento: " + descuentos.coddesextra + "-" + descuentos.descrip + " requiere Credito Fijo valido o ser cliente pertec!!!";
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "Los siguientes descuentos extras, requieren que el cliente: " + codcliente_real + " o su casa matriz local:" + codigoPrincipal + " o su casa matriz nacional: " + codigoPrincipal_nal + " tenga linea de credito valida o sea cliente Pertec";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Ninguna;
            }
            else
            {
                objres.resultado = true;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuentos_Extra_Para_TipoVenta(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<vedetalleanticipoProforma> dt_anticipo_pf, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string tipovta = "";
            string cadena = "";
            string cadena_aux = "";
            string cadenaA = "";
            string cadenaB = "";
            int cant_anticipos = 0;
            int cant_anticipos_usados = 0;
            int ttl_anticipos = 0;
            bool pedir_clave = true;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            int cod_desextra_contado = await configuracion.emp_coddesextra_x_deposito_contado(_context, codempresa);
            //esta rutina valida si el(los) descuentos extras aplicados a la proforma son validos para el tipo de venta de proforma(contado-credito-contra entrega)

            foreach (var descuentos in tabladescuentos)
            {
                tipovta = await ventas.Descuento_Extra_Tipo_Venta(_context, descuentos.coddesextra);
                if ((tipovta == "SIN RESTRICCION"))
                { }
                else
                {
                    if (!(tipovta == DVTA.tipo_vta))
                    {
                        cadena += "\r\n" + "La venta que esta realizando es tipo: " + DVTA.tipo_vta + " y el descuento aplicado: " + descuentos.coddesextra + "-" + descuentos.descrip + " es para venta tipo: " + tipovta;
                    }

                    if ((tipovta == DVTA.tipo_vta) && (DVTA.contra_entrega == "SI") && (descuentos.coddesextra == cod_desextra_contado))
                    {
                        cadena += "\r\n" + "La venta que esta realizando es tipo: " + DVTA.tipo_vta + " - CONTRA ENTREGA y el descuento aplicado: " + descuentos.coddesextra + "-" + descuentos.coddesextra + " es para venta tipo SOLO: " + tipovta;
                    }

                    if (("CONTADO" == DVTA.tipo_vta) && (DVTA.contra_entrega == "NO") && (descuentos.coddesextra == cod_desextra_contado))
                    {
                        cant_anticipos = 0;
                        cant_anticipos_usados = 0;
                        ttl_anticipos = dt_anticipo_pf.Count;
                        if (dt_anticipo_pf.Count > 0)
                        {
                            foreach (var anticipos in dt_anticipo_pf)
                            {
                                string[] id_nroid_deposito = new string[2];
                                id_nroid_deposito = await depositos_cliente.IdNroid_Deposito_Asignado_Anticipo(_context, anticipos.id_anticipo, anticipos.nroid_anticipo);
                                if (id_nroid_deposito[0] == "NSE")
                                {
                                    cadena += "\r\n" + "El anticipo: " + anticipos.id_anticipo + "-" + anticipos.nroid_anticipo + " asignado a la proforma no esta enlazado a algun deposito de cliente!!!";
                                    cadenaA = anticipos.id_anticipo + "-" + anticipos.nroid_anticipo;
                                    cadenaB = DVTA.codcliente;
                                }
                                else
                                {
                                    // Desde 07/12/2023 controlar q un anticipo ya aplicado a una proforma CONTADO con el descuento 74 no pueda volver aplicarse solicitado por JRA
                                    string doc_anticipo_ya_aplicado;
                                    doc_anticipo_ya_aplicado = await depositos_cliente.Anticipo_Asignado_A_Deposito_a_Proforma(_context, id_nroid_deposito[0], id_nroid_deposito[1], true);
                                    if (doc_anticipo_ya_aplicado.Contains("->"))
                                    {
                                        cadena_aux += "\r\n" + "El anticipo: " + anticipos.id_anticipo + "-" + anticipos.nroid_anticipo + " ya fue aplicado al documento: " + doc_anticipo_ya_aplicado + ", no puede utilizar este anticipo para el descuento: " + descuentos.coddesextra + " !!!";
                                        cant_anticipos_usados = cant_anticipos_usados + 1;
                                    }
                                    if (cant_anticipos_usados == ttl_anticipos)
                                    {
                                        cadena += cadena_aux;
                                        cadenaA = anticipos.id_anticipo + "-" + anticipos.nroid_anticipo;
                                        cadenaB = DVTA.codcliente;
                                    }

                                }

                            }
                        }
                        else
                        {
                            cadena += "\r\n" + "No existen anticipos asignados para validar si ya fueron utilizados!!!";
                            cadenaA = "NSE";
                            cadenaB = DVTA.codcliente;
                        }


                    }

                    // If reg("coddesextra") = cod_desextra_contado Then
                    //     If DVTA.contra_entrega = True And DVTA.tipo_vta = "CONTADO" Then
                    //         cadena &= vbCrLf & "La venta que esta realizando es tipo: " & DVTA.tipo_vta & " - CONTRA ENTREGA y el descuento aplicado: " & reg("coddesextra") & "-" & reg("descrip") & " es para venta tipo SOLO: " & tipovta
                    //     End If
                    // End If
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los descuentos extras aplicados no corresponden con el tipo de venta: " + DVTA.tipo_vta;
                objres.obsdetalle = cadena;
                objres.datoA = cadenaA;
                objres.datoB = cadenaB;
                //Desde 27/02/2024 segun autorizacion JRA solicitado por servicio al cliente que un anticipo ya utilizado para una proforma si esta tiene saldo, ese saldo puede utilizarce
                //para otra proforma y permitir el descuento respectivo.
                if (pedir_clave == true)
                {
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.accion = Acciones_Validar.Confirmar_SiNo;
                }

            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Monto_Minimos_Segun_Lista_Precio(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            decimal SUBTTL_GRAL_PEDIDO = 0;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real) == false)
            {
                return objres;
            }
            tabla_unidos.Columns.Add("codtarifa", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["codtarifa"] = detalle.codtarifa;
                dr["total"] = detalle.total;
                SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(dr["total"]);
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<int> precios = new List<int>();
                ArrayList totales = new ArrayList();
                decimal montomin, dif;

                // Obtener los diferentes precios que hay en el detalle
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (!precios.Contains(int.Parse(row["codtarifa"].ToString())))
                    {
                        precios.Add(int.Parse(row["codtarifa"].ToString()));
                    }
                }

                // Totalizar el monto por tipo de precio
                for (i = 0; i < precios.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["codtarifa"]) == Convert.ToInt32(precios[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                // Ahora es así desde mayo-2021
                if (DVTA.tipo_vta == "CONTADO")
                {
                    // Contado
                    if (cliente_nuevo)
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_nuevo_contado, moneda = t.codmoneda_min_nuevo_contado };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                    else
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_contado, moneda = t.codmoneda_min_contado };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                }
                else
                {
                    // Crédito
                    if (cliente_nuevo)
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_nuevo_credito, moneda = t.codmoneda_min_nuevo_credito };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                    else
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_credito, moneda = t.codmoneda_min_credito };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                }

                // Verificar el monto si cumple con el requerido
                // Antes se obtiene datos del complemento mayorista-Dimediado si tiene
                int _codproforma = 0;
                decimal _subtotal_pfcomplemento = 0;
                string _moneda_total_pfcomplemento = "";
                bool hay_enlace = false;

                try
                {
                    if (!string.IsNullOrEmpty(DVTA.idpf_complemento) && !string.IsNullOrEmpty(DVTA.nroidpf_complemento) && DVTA.tipo_complemento == "complemento_mayorista_dimediado")
                    {
                        _codproforma = await ventas.codproforma(_context, DVTA.idpf_complemento, int.Parse(DVTA.nroidpf_complemento));
                        if (Convert.ToInt32(DVTA.nroidpf_complemento) > 0)
                        {
                            _moneda_total_pfcomplemento = await ventas.MonedaPF(_context, _codproforma);
                            _subtotal_pfcomplemento = await ventas.SubTotal_Proforma(_context, _codproforma);
                            _subtotal_pfcomplemento = await tipocambio._conversion(_context, DVTA.codmoneda, _moneda_total_pfcomplemento, DVTA.fechadoc.Date, (decimal)_subtotal_pfcomplemento);
                            hay_enlace = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _moneda_total_pfcomplemento = "";
                    _subtotal_pfcomplemento = 0;
                }

                // Obtener el precio que tiene monto min mayor para validar con ese
                int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, precios);
                List<int> precios_para_validar = new List<int>();
                precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                i = 0;
                foreach (int precio in precios_para_validar)
                {

                    registro = tabla.Select("codigo=" + precio + " ");

                    // Verificar si recupero los datos del tipo de precio
                    if (registro.Length > 0)
                    {
                        montomin = await tipocambio._conversion(_context, DVTA.codmoneda, (string)registro[0]["moneda"], DVTA.fechadoc.Date, Convert.ToDecimal(registro[0]["montomin"]));

                        // Implementado en fecha: 07-07-2021
                        // Se añade el subtotal del complemento
                        dif = (Convert.ToDecimal(totales[i]) + _subtotal_pfcomplemento);
                        dif = SUBTTL_GRAL_PEDIDO + _subtotal_pfcomplemento;
                        dif -= montomin;
                        if (dif < 0)
                        {
                            cadena += "\nMonto mínimo de venta a precio: " + precio.ToString() + " es de: " + montomin.ToString() + "(" + DVTA.codmoneda + ") el monto actual es de: " + Math.Round(SUBTTL_GRAL_PEDIDO, 2).ToString();
                            cadena += "\nSi el pedido tiene diferentes precios, se valida con el monto minimo mayor de listas de precios.";
                        }
                    }
                    else
                    {
                        montomin = 9999999999;
                        cadena += "\nNo se encontró en la tabla de precios, los parámetros del tipo de precio: " + precio.ToString() + ", consulte con el administrador del sistema!!!";
                    }
                    i = i + 1;
                }
                registro = null;
                tabla.Dispose();
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "Los descuentos extras aplicados no corresponden con el tipo de venta: " + DVTA.tipo_vta;
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<string> Validar_Precios_Permitidos_Usuario(DBContext _context, string usuario, List<itemDataMatriz> tabladetalle)
        {
            string cadena_precios_no_autorizados_al_us = "";
            List<int> lista_precios = new List<int>();
            foreach (var reg in tabladetalle)
            {
                if (!await ventas.UsuarioTarifa_Permitido(_context, usuario, reg.codtarifa))
                {
                    //arma cadena de precios no permitidos
                    if (!lista_precios.Contains(reg.codtarifa))
                    {
                        lista_precios.Add(reg.codtarifa);
                    }
                    cadena_precios_no_autorizados_al_us = cadena_precios_no_autorizados_al_us + reg.codtarifa + ", ";
                }
            }
            return cadena_precios_no_autorizados_al_us;
        }
        public async Task<List<int>> Lista_Precios_En_El_Documento(List<itemDataMatriz> tabladetalle)
        {
            var elementosUnicos = tabladetalle
                .Select(objeto => objeto.codtarifa)
                .Distinct()
                .ToList();
            return elementosUnicos;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Minimo_Para_Entrega_Pedido(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            bool resultado = true;
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            decimal SUBTTL_GRAL_PEDIDO = 0;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //si el codalmacen es tienda, directo se validar como verdadero porque siginifica que es tienda y las ventas en mostrador
            //se entregan al momento de la venta en mostrador
            if (await almacen.Es_Tienda(_context, int.Parse(DVTA.codalmacen)))
            {
                return objres;
            }
            tabla_unidos.Columns.Add("codtarifa", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["codtarifa"] = detalle.codtarifa;
                dr["total"] = detalle.total;
                SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(dr["total"]);
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<int> precios = new List<int>();
                ArrayList totales = new ArrayList();
                decimal montomin, dif;

                // Obtener los diferentes precios que hay en el detalle
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (!precios.Contains(int.Parse(row["codtarifa"].ToString())))
                    {
                        precios.Add(int.Parse(row["codtarifa"].ToString()));
                    }
                }

                // Totalizar el monto por tipo de precio
                for (i = 0; i < precios.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["codtarifa"]) == Convert.ToInt32(precios[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                //DESDE EL 20-02-2023
                if (DVTA.tipo_vta == "CONTADO")
                {
                    // Contado
                    var query = from t in _context.intarifa
                                orderby t.codigo
                                select new { t.codigo, montomin = t.min_entrega_contado, moneda = t.codmoneda_min_entrega_contado };
                    var result = query.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);
                }
                else
                {
                    // Crédito
                    var query = from t in _context.intarifa
                                orderby t.codigo
                                select new { t.codigo, montomin = t.min_entrega_credito, moneda = t.codmoneda_min_entrega_credito };
                    var result = query.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);
                }

                // Obtener el precio que tiene monto min mayor para validar con ese
                int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, precios);
                List<int> precios_para_validar = new List<int>();
                precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                foreach (int precio in precios_para_validar)
                {
                    registro = tabla.Select("codigo=" + precio + " ");

                    // Verificar si recupero los datos del tipo de precio
                    if (registro.Length == 0)
                    {
                        // verificar si hay precio
                        montomin = 9999999999;
                        cadena += "\nNo se encontró en la tabla de precios, los parámetros del tipo de precio: " + precio.ToString() + ", consulte con el administrador del sistema!!!";
                    }
                    else
                    {
                        //obtiene el monto minimo requerido de la lista de precio
                        montomin = await tipocambio._conversion(_context, DVTA.codmoneda, (string)registro[0]["moneda"], DVTA.fechadoc.Date, Convert.ToDecimal(registro[0]["montomin"]));
                        dif = Math.Round(SUBTTL_GRAL_PEDIDO, 2) - montomin;
                        if (dif < 0)
                        {
                            resultado = false;
                            cadena += "\nMonto Min a precio: " + precio.ToString() + " Para Entrega es: " + montomin.ToString() + "(" + DVTA.codmoneda + ") el monto actual es de: " + Math.Round(SUBTTL_GRAL_PEDIDO, 2).ToString() + "(" + DVTA.codmoneda + ")";
                            cadena += "\nSi el pedido tiene diferentes precios, se valida con el monto minimo mayor de listas de precios.";
                        }
                    }
                }
                registro = null;
                tabla.Dispose();
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "El pedido no puede ser entregado al cliente por Pertec!!!";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales(DBContext _context, string codcliente_real, List<itemDataMatriz> tabladetalle, string codmoneda, DateTime fecha, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //a todos los clientes se les debe validar si cumple el monto minimo de los desctos especiales
            //no importa si es cliente final o no, esta aclaracion la hizo JRA fines de julio 2022
            //siempre debe controlar que cumpla el monto para descto especial

            tabla_unidos.Columns.Add("coddescuento", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["coddescuento"] = detalle.coddescuento;
                dr["total"] = detalle.total;
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<string> descuentos = new List<string>();
                ArrayList totales = new ArrayList();
                decimal montomin, dif;

                // Obtener los diferentes precios que hay en el detalle
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (int.Parse(row["coddescuento"].ToString()) > 0)
                    {
                        if (!descuentos.Contains(row["coddescuento"].ToString()))
                        {
                            descuentos.Add(row["coddescuento"].ToString());
                        }
                    }

                }

                // Totalizar el monto por tipo de precio
                for (i = 0; i < descuentos.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["coddescuento"]) == Convert.ToInt32(descuentos[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                var query = from t in _context.vedescuento
                            orderby t.codigo
                            select new { t.codigo, t.monto, t.moneda };
                var result = query.Distinct().ToList();
                tabla = funciones.ToDataTable(result);

                i = 0;
                foreach (string desc in descuentos)
                {

                    registro = tabla.Select("codigo=" + desc + " ");

                    // Verificar si recupero los datos del tipo de precio
                    if (registro.Length == 0)
                    {
                        // verificar si hay precio
                        cadena += "\nNo se encontro en la tabla(vedescuento) de descuentos especiales, los parametros del descuento: " + desc.ToString() + ", consulte con el administrador del sistema!!!";
                    }
                    else
                    {
                        montomin = await tipocambio._conversion(_context, codmoneda, (string)registro[0]["moneda"], fecha, Convert.ToDecimal(registro[0]["monto"]));
                        dif = Convert.ToDecimal(totales[i]) - montomin;
                        if (dif < 0)
                        {
                            cadena += "\nEl monto minimo para aplicar el descuento especial: " + desc.ToString() + " es de: " + montomin.ToString() + "(" + codmoneda + ") el monto actual es de: " + Math.Round(Convert.ToDecimal(totales[i]), 2).ToString();
                        }
                    }
                    i = i + 1;
                }
                registro = null;
                tabla.Dispose();
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "Se encontraron observaciones para la aplicacion del descuento especial: ";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Empaque_Minimo_Segun_Lista_Precios(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string item = "";
            int i, j;
            decimal cantidad_total;

            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (!await cliente.Controla_empaque_minimo(_context, DVTA.codcliente_real))
            {
                return objres;
            }

            foreach (var detalle in tabladetalle)
            {
                //desde 23-11-2022 implementar que de un item sume la cantidad total de los items repetidos para asi validar si el empaque del item sin descuento cumpla o no el empaque minimo
                //ya que al realizar la division de las cantidad de un item para cumplir empaque caja cerrada puede haber items q no cumplan el empaque minimo despues de la division'
                //para esto el item sin descuento empaque cerrada para validar debe sumar su propia cantidad mas la cantidad de caja cerrada y validar el empaque minimo con esa cantidad
                if (detalle.coddescuento == 0)
                {
                    //si no tiene descuento sumar las cantidad de todo el detalle con el mismo item y validar el empaque con ese total
                    cantidad_total = 0;
                    item = detalle.coditem;
                    //sacar items iguales
                    foreach (var detalle2 in tabladetalle)
                    {
                        if (item == detalle2.coditem)
                        {
                            //si es igual sumar la cantidad
                            cantidad_total = cantidad_total + (decimal)detalle2.cantidad;
                        }
                        else
                        {
                            cantidad_total = cantidad_total;
                        }
                    }
                    if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, cantidad_total, int.Parse(DVTA.codalmacen), DVTA.codcliente_real))
                    {
                        detalle.cumple = true;
                    }
                    else
                    {
                        cadena += "\n" + detalle.coditem + " precio: " + detalle.codtarifa;
                        detalle.cumple = false;
                    }
                }
                else
                {
                    if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, int.Parse(DVTA.codalmacen), DVTA.codcliente_real))
                    {
                        detalle.cumple = true;
                    }
                    else
                    {
                        cadena += "\n" + detalle.coditem + " precio: " + detalle.codtarifa;
                        detalle.cumple = false;
                    }
                }
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.nitfactura;
                }
                objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                objres.observacion = "Los siguientes items no cumplen el empaque minimo de la lista de precios: ";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Credito_Disponible_Para_Vta(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool tiene_credito_suficiente = true;
            string moneda_cliente = await cliente.monedacliente(_context, DVTA.codcliente_real, codempresa, usuario);
            decimal monto = 0;
            string monedae = await empresa.monedaext(_context, codempresa);
            string monedabase = await empresa.monedabase(_context, codempresa);


            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (!(DVTA.tipo_vta.Trim().ToUpper() == "CREDITO"))
            {
                return objres;
            }

            if ((DVTA.tipo_vta.Trim().ToUpper() == "CREDITO"))
            {
                if ((DVTA.codmoneda == monedae))
                {
                    // If DVTA.codmoneda = moneda_cliente Then
                    // tiene_credito_suficiente = sia_funciones.Creditos.Instancia.ValidarCreditoDisponible(False, DVTA.codcliente_real, True, CDbl(DVTA.totaldoc), sia_compartidos.temporales.Instancia.codempresa, sia_compartidos.temporales.Instancia.usuario, False)
                    var result = await creditos.ValidarCreditoDisponible_en_Bs(_context, false, DVTA.codcliente_real, true, DVTA.totaldoc, codempresa, usuario, monedae, DVTA.codmoneda);
                    tiene_credito_suficiente = result.resultado_func;
                }
                else
                {
                    // monto = sia_funciones.TipoCambio.Instancia.conversion(moneda_cliente, DVTA.codmoneda, DVTA.fechadoc.Date, DVTA.totaldoc)
                    var result1 = await creditos.ValidarCreditoDisponible_en_Bs(_context, false, DVTA.codcliente_real, true, (double)await tipocambio._conversion(_context, monedabase, DVTA.codmoneda, DVTA.fechadoc, (decimal)DVTA.totaldoc), codempresa, usuario, monedae, DVTA.codmoneda);
                    tiene_credito_suficiente = result1.resultado_func;
                }

            }

            if (!tiene_credito_suficiente)
            {
                objres.resultado = false;
                objres.observacion = ("El cliente:" + (DVTA.codcliente_real + " no tiene credito suficiente, verifique esta situacion!!!"));
                objres.obsdetalle = "";
                if ((DVTA.estado_doc_vta == "NUEVO"))
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = (DVTA.id + ("-" + DVTA.numeroid));
                }

                objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Etiqueta_Proforma(DBContext _context, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await empresa.Forzar_Etiquetas(_context, codempresa))
            {
                if (dt_etiqueta.Count <= 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Debe indicar los datos de la etiqueta del pedido!!!";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
                else
                {
                    objres.resultado = true;
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    //direccion 
                    objres.observacion = dt_etiqueta[0].representante;
                }
            }
            else
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Tiene_Reversiones_PP_Pendientes_de_Pago(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (await Venta_Tiene_Descuento_ProntoPago(_context, tabladescuentos))
            {
                string cadena_msg = "";
                int nro_reversiones_pendiente_permitido = 0;
                int nro_reversiones_pendiente_cliente = 0;
                nro_reversiones_pendiente_cliente = await ventas.Nro_Reversiones_Pendientes_de_Pago_Cliente(_context, DVTA.codcliente_real);
                nro_reversiones_pendiente_permitido = await ventas.Nro_Reversiones_Pendientes_Pago_Permitido(_context, codempresa);
                if ((nro_reversiones_pendiente_cliente > nro_reversiones_pendiente_permitido))
                {
                    cadena_msg = await ventas.Notas_de_Reversion_Pendientes_de_Pago(_context, DVTA.codcliente_real);
                    objres.resultado = false;
                    objres.observacion = "El cliente tiene: " + nro_reversiones_pendiente_cliente + " reversiones pronto pago pendientes de pago, y el maximo permitido es: " + nro_reversiones_pendiente_permitido + ", por lo cual no se le puede otorgar mas descuentos pronto pago.";
                    objres.obsdetalle = ("\r\n" + cadena_msg);
                    if ((DVTA.estado_doc_vta == "NUEVO"))
                    {
                        objres.datoA = DVTA.nitfactura;
                    }
                    else
                    {
                        objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente;
                    }
                    objres.datoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            return objres;
        }
        public async Task<bool> Venta_Tiene_Descuento_ProntoPago(DBContext _context, List<vedesextraDatos> tabladescuentos)
        {
            bool resultado = false;

            foreach (var row in tabladescuentos)
            {
                if (await ventas.Descuento_Extra_Es_Pronto_Pago(_context, row.coddesextra))
                {
                    resultado = true;
                    break;
                }
                else
                {
                    resultado = false;
                }
            }

            return resultado;
        }
        public async Task<int> Tarifa_Monto_Min_Mayor_Rodrigo(DBContext _context, List<int> milista_precios, veproforma DVTA)
        {
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            double MONTO_MAYOR = 0;
            int resultado = 0;
            if (milista_precios.Count() == 1)
            {
                return milista_precios[0];
            }
            foreach (var reg in milista_precios)
            {
                intarifaMonMinMay tabla = new intarifaMonMinMay();
                if (DVTA.tipopago == 0)   // "CONTADO"
                {
                    // contado
                    if (cliente_nuevo)
                    {

                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_nuevo_contado ?? 0,
                                moneda = i.codmoneda_min_nuevo_contado
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                    else
                    {
                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_contado ?? 0,
                                moneda = i.codmoneda_min_contado
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                }
                else  //  CREDITO  == 1
                {
                    // credito
                    if (cliente_nuevo)
                    {

                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_nuevo_credito ?? 0,
                                moneda = i.codmoneda_min_nuevo_credito
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                    else
                    {
                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_credito ?? 0,
                                moneda = i.codmoneda_min_credito
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                    // convertir a la moneda en la cual se esta haciendo la proforma
                    double Monto_Min_Tarifa = (double)await tipocambio._conversion(_context, DVTA.codmoneda, tabla.moneda, DVTA.fecha, tabla.montomin);

                    // verificar si el monto min del tipo de precio es mayor, si es mayo se obtiene su codtarifa
                    if (MONTO_MAYOR == 0)
                    {
                        MONTO_MAYOR = Monto_Min_Tarifa;
                        resultado = tabla.codtarifa;
                    }
                    else
                    {
                        if (Monto_Min_Tarifa > MONTO_MAYOR)
                        {
                            MONTO_MAYOR = Monto_Min_Tarifa;
                            resultado = tabla.codtarifa;
                        }
                    }
                }
            }
            return resultado;
        }
        public async Task<int> Tarifa_Monto_Min_Mayor(DBContext _context, DatosDocVta DVTA, List<int> milista_precios)
        {
            int resultado = 0;
            DataTable tabla = new DataTable();
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            decimal Monto_Min_Tarifa;
            decimal MONTO_MAYOR = 0;

            if (milista_precios.Count == 1)
            {
                resultado = milista_precios[0];
            }
            else
            {
                foreach (int precio in milista_precios)
                {
                    if (DVTA.tipo_vta == "CONTADO")
                    {
                        if (cliente_nuevo)
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_nuevo_contado, moneda = t.codmoneda_min_nuevo_contado };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_nuevo_contado AS montomin, codmoneda_min_nuevo_contado AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                        else
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_contado, moneda = t.codmoneda_min_contado };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_contado AS montomin, codmoneda_min_contado AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                    }
                    else
                    {
                        if (cliente_nuevo)
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_nuevo_credito, moneda = t.codmoneda_min_nuevo_credito };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_nuevo_credito AS montomin, codmoneda_min_nuevo_credito AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                        else
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_credito, moneda = t.codmoneda_min_credito };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_credito AS montomin, codmoneda_min_credito AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                    }

                    Monto_Min_Tarifa = 0;
                    Monto_Min_Tarifa = await tipocambio._conversion(_context, DVTA.codmoneda, (string)tabla.Rows[0]["moneda"], DVTA.fechadoc.Date, (decimal)tabla.Rows[0]["montomin"]);

                    if (MONTO_MAYOR == 0)
                    {
                        MONTO_MAYOR = Monto_Min_Tarifa;
                        resultado = int.Parse(tabla.Rows[0]["codtarifa"].ToString());
                    }
                    else
                    {
                        if (Monto_Min_Tarifa > MONTO_MAYOR)
                        {
                            MONTO_MAYOR = Monto_Min_Tarifa;
                            resultado = int.Parse(tabla.Rows[0]["codtarifa"].ToString());
                        }
                        else
                        {
                            MONTO_MAYOR = MONTO_MAYOR;
                        }
                    }
                }
            }

            return resultado;
        }

        public async Task<ResultadoValidacion> Validar_Periodo_Abierto_Para_Venta(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string periodo = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DVTA.fechadoc.Date.Month).ToUpper() + "-" + DVTA.fechadoc.Year.ToString();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            if (!await seguridad.periodo_fechaabierta_context(_context, DVTA.fechadoc.Date, 3))
            {
                objres.resultado = false;
                objres.observacion = "El periodo " + periodo + " esta cerrado, no se pueden realizar ventas con esa fecha!!!";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        //public async Task<bool> Validar_Resaltar_Empaques_Minimos_Segun_Lista_Precios(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, string codcliente)
        //{
        //    bool resultado = true;

        //    foreach (var detalle in tabladetalle)
        //    {
        //        if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codalmacen, codcliente))
        //        {
        //            detalle.cumple = true;
        //        }
        //        else
        //        {
        //            resultado = false;
        //            detalle.cumple = false;
        //        }
        //    }

        //    return resultado;
        //}
        public async Task<(bool cumple, List<itemDataMatriz> tabladetalle)> Validar_Resaltar_Empaques_Minimos_Segun_Lista_Precios(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, string codcliente)
        {
            bool resultado = true;

            foreach (var detalle in tabladetalle)
            {
                if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codalmacen, codcliente))
                {
                    detalle.cumpleMin = true;
                }
                else
                {
                    resultado = false;
                    detalle.cumpleMin = false;
                }
            }

            return (resultado, tabladetalle);
        }
        public List<string> ListaItemsPedido(List<itemDataMatriz> tabladetalle)
        {
            List<string> resultado = new List<string>();

            resultado.Clear();
            foreach (var detalle in tabladetalle)
            {
                resultado.Add(detalle.coditem);
            }
            return resultado;
        }

        public async Task<(string cadena, List<itemDataMatriz> tabladetalle)> Validar_Resaltar_Empaques_Caja_Cerrada_DesctoEspecial_detalle(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, bool quitar_descuento, string codcliente)
        {
            bool resultado = true;
            string cadena = "";

            foreach (var detalle in tabladetalle)
            {//validar el empaque del precio
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codcliente))
                {
                    detalle.cumple = true;
                }
                else
                {
                    if (quitar_descuento)
                    {
                        resultado = false;
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    else
                    {
                        resultado = false;
                    }
                    if (cadena.Length == 0)
                    {
                        cadena = "";
                        cadena = " ITEM     DESCRIPCION          MEDIDA      CANTIDAD" + "\r\n";
                        cadena += "------------------------------------------------" + "\r\n";
                        cadena += " " + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 11, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString(), 3, " ") + "\r\n";
                    }
                    else
                    {
                        cadena += " " + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 11, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString(), 3, " ") + "\r\n";
                    }

                }
            }

            return (cadena, tabladetalle);
        }

        public async Task<(bool result, List<itemDataMatriz> tabladetalle)> Validar_Resaltar_Empaques_Caja_Cerrada_DesctoEspecial(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, bool quitar_descuento, string codcliente)
        {
            bool resultado = true;

            foreach (var detalle in tabladetalle)
            {//validar el empaque del precio
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codcliente))
                {
                    detalle.cumpleMin = true;
                }
                else
                {
                    if (quitar_descuento)
                    {
                        resultado = false;
                        detalle.cumpleMin = false;
                        detalle.coddescuento = 0;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
            }

            return (resultado, tabladetalle);
        }


        public async Task<(List<itemDataSugerencia> tabla_sugerencia, List<itemDataMatriz> tabladetalle)> Sugerir_Cantidades_Empaques_Caja_Cerrada_DesctoEspecial(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, int coddescuento, string codempresa)
        {
            bool resultado = true;
            List<itemDataSugerencia> dt = new List<itemDataSugerencia>();
            int cantidad = 0;
            int cantidad_mas = 0;
            int cantidad_menos = 0;
            string sugerencia = "0";
            string obs = "Sin Obs.";
            int i;
            double porcentaje_empaque = 0;
            double cantidad_porcentaje = 0;
            double empaque_caja_cerrada = 0;
            int cant_empaques = 0;
            double diferencia = 0;
            double empaque_aux = 0;

            porcentaje_empaque = await configuracion.porcentaje_sugerencia_empaque(_context, codempresa);
            foreach (var detalle in tabladetalle)
            {//validar el empaque del precio
                cantidad = 0;
                empaque_caja_cerrada = await ventas.Empaque(_context, await ventas.Codigo_Empaque_Descuento_Especial(_context, coddescuento), detalle.coditem);
                //validar el empaque del descto especial
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, ""))
                {
                    detalle.cumple = true;
                    itemDataSugerencia nuevoItem = new itemDataSugerencia();
                    nuevoItem.coditem = detalle.coditem;
                    nuevoItem.descripcion = detalle.descripcion;
                    nuevoItem.medida = detalle.medida;
                    nuevoItem.cantidad = detalle.cantidad;
                    if (detalle.cantidad == 0 || empaque_caja_cerrada == 0)
                    {
                        nuevoItem.cantidad_sugerida = "0";
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    else
                    {
                        nuevoItem.cantidad_sugerida = "Cumple empaque.";
                    }
                    nuevoItem.cantidad_sugerida_aplicable = cantidad;
                    nuevoItem.empaque_caja_cerrada = empaque_caja_cerrada;
                    nuevoItem.porcentaje = porcentaje_empaque;
                    cantidad_porcentaje = (empaque_caja_cerrada * porcentaje_empaque) / 100;
                    nuevoItem.cantidad_porcentaje = cantidad_porcentaje;
                    nuevoItem.diferencia = 0;
                    if (detalle.cantidad == 0 || empaque_caja_cerrada == 0)
                    {
                        nuevoItem.obs = "Cantidad 0 / Empaque Cerrado 0";
                    }
                    else
                    {
                        nuevoItem.obs = "Cumple Empaque Cerrado.";
                    }
                    dt.Add(nuevoItem);
                }
                else
                {
                    sugerencia = await ventas.Sugerir_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, detalle.cantidad, "", codempresa);
                    string[] resultado_list = sugerencia.Split('/');
                    cantidad_mas = Convert.ToInt32(resultado_list[0]);
                    cantidad_menos = Convert.ToInt32(resultado_list[1]);

                    if (cantidad_mas > 0)
                    {//si la cantidad se va aumentar ingresa aca
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    itemDataSugerencia otroItem = new itemDataSugerencia();
                    otroItem.coditem = detalle.coditem;
                    otroItem.descripcion = detalle.descripcion;
                    otroItem.medida = detalle.medida;
                    otroItem.cantidad = detalle.cantidad;
                    otroItem.cantidad_sugerida = sugerencia;
                    cant_empaques = (int)(detalle.cantidad / empaque_caja_cerrada);
                    empaque_aux = cant_empaques * empaque_caja_cerrada;
                    cantidad_porcentaje = (empaque_caja_cerrada * porcentaje_empaque) / 100;
                    if (Math.Abs(cantidad_mas) <= Math.Abs(cantidad_porcentaje))
                    {
                        cantidad = cantidad_mas;
                    }
                    if (Math.Abs(cantidad_menos) <= Math.Abs(cantidad_porcentaje))
                    {
                        if (empaque_caja_cerrada - (cantidad_mas + detalle.cantidad) == 0)
                        {
                            cantidad = cantidad_mas;
                        }
                        else
                        {
                            cantidad = cantidad_menos * -1;
                        }
                    }
                    if (cantidad == 0)
                    {
                        obs = "No Cumple.";
                        if (cantidad_mas > cantidad_menos)
                        {
                            if (detalle.cantidad > empaque_caja_cerrada)
                            {
                                cantidad = cantidad_menos;
                            }
                            else
                            {
                                cantidad = cantidad_mas;
                            }

                        }
                        else
                        {
                            cantidad = cantidad_mas;
                        }
                        diferencia = Math.Abs(cantidad_porcentaje) - Math.Abs(cantidad);
                    }
                    else
                    {
                        if (cantidad > 0)
                        {
                            if (cantidad <= cantidad_porcentaje)
                            {
                                obs = "Cumple porcentaje para aumentar cantidades.";
                            }
                            else
                            {
                                obs = "No Cumple.";
                            }

                        }
                        else
                        {
                            if (detalle.cantidad < empaque_caja_cerrada)
                            {
                                obs = "No Cumple.";
                            }
                            else
                            {
                                obs = "Cumple porcentaje para reducir cantidades.";
                            }
                        }
                        diferencia = Math.Abs(cantidad) - Math.Abs(cantidad_porcentaje);
                    }
                    otroItem.cantidad_sugerida_aplicable = cantidad;
                    otroItem.empaque_caja_cerrada = empaque_caja_cerrada;
                    otroItem.porcentaje = porcentaje_empaque;
                    otroItem.cantidad_porcentaje = cantidad_porcentaje;
                    otroItem.diferencia = Math.Abs(diferencia);
                    otroItem.obs = obs;
                    dt.Add(otroItem);

                }
            }

            return (dt, tabladetalle);
        }

        public async Task<ResultadoValidacion> Validar_Enlace_Proforma_Mayorista_Dimediado(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, int coddescuento, string codempresa)
        {

        }



    }
}
