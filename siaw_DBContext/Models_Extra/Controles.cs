using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_DBContext.Models_Extra
{
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
        public int NroItems { get; set; }
        public double Descuentos { get; set; }
        public double Recargos { get; set; }
        public string Nit { get; set; }
        public double Subtotal { get; set; }
        public double Total { get; set; }
        public string Preparacion { get; set; }
        public string TipoVenta { get; set; }
        public string Contra_Entrega { get; set; }
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
        public List<Dtnegativos>? Dtnegativos { get; set; }
        public List<Dtnocumplen>? Dtnocumplen { get; set; }
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
}
