using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_DBContext.Models_Extra
{
    public class SaveProformaCompleta
    {
        public veproforma veproforma { get; set; }
        public List<veproforma1> veproforma1 { get; set; }
        public List<veproforma_valida>? veproforma_valida { get; set; }
        public List<tabla_veproformaAnticipo>? dt_anticipo_pf { get; set; }
        public List<vedesextraprof>? vedesextraprof { get; set; }
        public List<verecargoprof>? verecargoprof { get; set; }
        public List<veproforma_iva>? veproforma_iva { get; set; }
        public veetiqueta_proforma? veetiqueta_proforma { get; set; }

        public DatosDocVta? DVTA { get; set; }
        public List<vedesextraDatos>? tabladescuentos { get; set; }
        public List<verecargosDatos>? tablarecargos { get; set; }
    }

    public class TotabilizarProformaCompleta
    {
        public veproforma veproforma { get; set; }
        public List<veproforma1_2> veproforma1_2 { get; set; }
        public List<veproforma_valida>? veproforma_valida { get; set; }
        public List<veproforma_anticipo>? veproforma_anticipo { get; set; }
        public List<tabladescuentos>? vedesextraprof { get; set; }
        public List<tablarecargos>? verecargoprof { get; set; }
        public List<veproforma_iva>? veproforma_iva { get; set; }
        public List<vedetalleanticipoProforma>? detalleAnticipos { get; set; }
    }
    public class tabla_veproformaAnticipo
    {
        public int? codproforma { get; set; }
        public int? codanticipo { get; set; }
        public string? docanticipo { get; set; }

        public string id_anticipo { get; set; }
        public int nroid_anticipo { get; set; }

        public double monto { get; set; }
        public double? tdc { get; set; }
        public string codmoneda { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string horareg { get; set; }
        public string codvendedor { get; set; }
    }

    public class RequestProformaMayor50000
    {
        public string id { get; set; }
        public int numeroid { get; set; }
        public string tipopago { get; set; }
        public bool contra_entrega { get; set; }
        public string codmoneda { get; set; }
        public DateTime fecha { get; set; }
        public decimal total { get; set; }
        public int cantidad_anticipos { get; set; }
    }
}
