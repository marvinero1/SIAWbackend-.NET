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
        { }
        public class Dtnegativos
        { }

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
        }
        private siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        //Task<ActionResult<itemDataMatriz>>
        //Task<Controles>
        public async Task<List<Controles>> DocumentoValido(string userConnectionString, string cadena_control, string tipodoc, string opcion_validar, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos)
        {
            List <Controles> resultados = new List<Controles>();
            List<Controles> controles_final = new List<Controles>();
            string validando = "";
            if (cadena_control == "vacio") { cadena_control = ""; }

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
                                     Orden = vc.orden,
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
                                     Accion = ""
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
                    Dtnocumplen dtnocumplen = new Dtnocumplen();
                    Dtnegativos dtnegativos = new Dtnegativos();
                    Control_Valido(userConnectionString, control, DVTA, tabladetalle, tabladescuentos, dt_etiqueta, dt_anticipo_pf, tablarecargos, dtnocumplen, dtnegativos);
                }
                resultados = controles_final;
            }
            return resultados;
        }
        public void Control_Valido(string userConnectionString, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos, Dtnocumplen dtnocumplenm, Dtnegativos dtnegativos)
        {
            string _codcontrol = regcontrol.CodControl;
            string _desccontrol = regcontrol.Descripcion;
            switch (_codcontrol)
            {
                case "00001":
                    _ = Control_Valido_C00001Async(userConnectionString, regcontrol, DVTA);
                    break;
                case "00002":
                    // Control_Valido_C00002(regcontrol, DVTA);
                    break;
                case "00003":
                    //  Control_Valido_C00003(regcontrol, DVTA, tabladetalle);
                    break;
                case "00004":
                    // Control_Valido_C00004(regcontrol, DVTA, tabladetalle);
                    break;
                case "00005":
                    //  Control_Valido_C00005(regcontrol, DVTA, tabladetalle);
                    break;
                case "00006":
                    // Control_Valido_C00006(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00007":
                    // Control_Valido_C00007(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00008":
                    // Control_Valido_C00008(regcontrol, DVTA);
                    break;
                case "00009":
                    // Control_Valido_C00009(regcontrol, DVTA);
                    break;
                case "00010":
                    //Control_Valido_C00010(regcontrol, DVTA);
                    break;
                case "00011":
                    // Control_Valido_C00011(regcontrol, DVTA);
                    break;
                case "00012":
                    // Control_Valido_C00012(regcontrol, DVTA);
                    break;
                case "00013":
                    //  Control_Valido_C00013(regcontrol, DVTA);
                    // //VERIFICA VENTA CON CODIGO DE CLIENTE SN Y CLIENTE REAL QUE NO ES SN
                    // Control_Valido_C00013(regcontrol, DVTA);
                    break;
                case "00014":
                    //  Control_Valido_C00014(regcontrol, tabladescuentos);
                    break;
                case "00015":
                    //  Control_Valido_C00015(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00016":
                    //  Control_Valido_C00016(regcontrol, DVTA, tabladescuentos, tabladetalle);
                    break;
                case "00017":
                    // Control_Valido_C00017(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00018":
                    //  Control_Valido_C00018(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00019":
                    //  Control_Valido_C00019(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00020":
                    // Control_Valido_C00020(regcontrol, DVTA, tabladetalle);
                    break;
                case "00021":
                    //  Control_Valido_C00021(regcontrol, DVTA, tabladetalle);
                    break;
                case "00022":
                    // Control_Valido_C00022(regcontrol, DVTA, tabladetalle);
                    break;
                case "00023":
                    // Control_Valido_C00023(regcontrol, DVTA, tabladetalle);
                    break;
                case "00024":
                    // Control_Valido_C00024(regcontrol, DVTA);
                    break;
                case "00025":
                    //  Control_Valido_C00025(regcontrol, DVTA, dt_etiqueta);
                    break;
                case "00026":
                    //  Control_Valido_C00026(regcontrol, DVTA, dt_etiqueta);
                    break;
                case "00027":
                    // Control_Valido_C00027(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00028":
                    // Control_Valido_C00028(regcontrol, DVTA);
                    break;
                case "00029":
                    // Control_Valido_C00029(regcontrol, DVTA);
                    break;
                case "00030":
                    //  Control_Valido_C00030(regcontrol, DVTA);
                    break;
                case "00031":
                    // Control_Valido_C00031(regcontrol, DVTA);
                    break;
                case "00032":
                    // Control_Valido_C00032(regcontrol, DVTA);
                    break;
                case "00033":
                    //  Control_Valido_C00033(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    break;
                case "00034":
                    // Control_Valido_C00034(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    break;
                case "00035":
                    //  Control_Valido_C00035(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    break;
                case "00036":
                    //  Control_Valido_C00036(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    break;
                case "00037":
                    //  Control_Valido_C00037(regcontrol, DVTA);
                    break;
                case "00038":
                    // Control_Valido_C00038(regcontrol, DVTA, dt_anticipo_pf);
                    break;
                case "00039":
                    // Control_Valido_C00039(regcontrol, DVTA);
                    break;
                case "00040":
                    //  Control_Valido_C00040(regcontrol, DVTA);
                    break;
                case "00041":
                    //  Control_Valido_C00041(regcontrol, DVTA);
                    break;
                case "00042":
                    //  control_valido_c00042(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00043":
                    // control_valido_c00043(regcontrol, DVTA);
                    break;
                case "00044":
                    // control_valido_c00044(regcontrol, DVTA);
                    break;
                case "00045":
                    // control_valido_c00045(regcontrol, DVTA);
                    break;
                case "00046":
                    // control_valido_c00046(regcontrol, DVTA, tabladetalle);
                    break;
                case "00047":
                    // control_valido_c00047(regcontrol, DVTA, tabladetalle);
                    break;
                case "00048":
                    // control_valido_c00048(regcontrol, DVTA, tabladetalle);
                    break;
                case "00049":
                    // control_valido_c00049(regcontrol, DVTA, tabladetalle);
                    break;
                case "00050":
                    // Control_Valido_C00050(regcontrol, DVTA, tabladetalle, tablarecargos);
                    break;
                case "00051":
                    //  Control_Valido_C00051(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00052":
                    // Control_Valido_C00052(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    break;
                case "00053":
                    // Control_Valido_C00053(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00054":
                    //  Control_Valido_C00054(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00055":
                    //  Control_Valido_C00055(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00056":
                    //  Control_Valido_C00056(regcontrol, DVTA, dt_etiqueta);
                    break;
                case "00057":
                    //  Control_Valido_C00057(regcontrol, DVTA, tabladescuentos);
                    break;
                case "00058":
                    // Control_Valido_C00058(regcontrol, DVTA, tabladetalle, dtnocumplen, dgvmaximos_vta);
                    break;
                case "00059":
                    // Control_Valido_C00059(regcontrol, DVTA);
                    break;
                case "00060":
                    //  Control_Valido_C00060(regcontrol, DVTA, tabladetalle, dtnegativos, dgvnegativos);
                    break;
                case "00061":
                    //  Control_Valido_C00061(true, regcontrol, DVTA, tabladetalle, tablarecargos);
                    break;
                case "00062":
                    //  Control_Valido_C00061(false, regcontrol, DVTA, tabladetalle, tablarecargos);
                    break;
                case "00063":
                    // Control_Valido_C00063(false, regcontrol, DVTA, tabladetalle, tablarecargos);
                    break;
                case "00064":
                    // Control_Valido_C00064(regcontrol, DVTA, dt_anticipo_pf);
                    break;
                case "00065":
                    //Control_Valido_C00065(regcontrol, DVTA, tabladetalle);
                    break;
                case "00066":
                    // Control_Valido_C00066(regcontrol, DVTA, tabladetalle);
                    break;
                case "00067":
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

        }
        private async Task<bool> Control_Valido_C00001Async(string userConnectionString, Controles regcontrol, DatosDocVta DVTA)
        {
            // //PERMITIR GRABAR PARA APROBAR PROFORMA CLIENTE COMPETENCIA
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
           objres = await Validar_NIT_Es_Cliente_CompetenciaAsync(userConnectionString,DVTA);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion= objres.accion.ToString();
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_NIT_Es_Cliente_CompetenciaAsync(string userConnectionString, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            //var es_cliente_competencia = await Cliente.EsClienteCompetencia(userConnectionString, DVTA.nitfactura);
            if (await cliente.EsClienteCompetencia(userConnectionString, DVTA.nitfactura))
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
    }
}
