namespace siaw_DBContext.Models_Extra
{
    public class veproforma1_Report
    {
        // Report Header
        public string empresa { get; set; }
        public string hora_impresion { get; set; }
        public string fecha_impresion { get; set; }
        public string rnit { get; set; }
        public string rnota_remision { get; set; }
        public string inicial { get; set; }

        // Page Header
        public string titulo { get; set; }
        public string tipopago { get; set; }
        public string codalmacen { get; set; }
        public string rcodcliente { get; set; }
        public string rcliente { get; set; }
        public string rnombre_comercial { get; set; }
        public string rcodvendedor { get; set; }
        public string rtdc { get; set; }
        public string rmonedabase { get; set; }
        public string rfecha { get; set; }
        public string rdireccion { get; set; }
        public string rtelefono { get; set; }
        public string rpreparacion { get; set; }
        public string rptoventa { get; set; }

        // Report Footer
        public string rpesototal { get; set; }
        public string rsubtotal { get; set; }
        public string rrecargos { get; set; }
        public string rdescuentos { get; set; }
        public string riva { get; set; }
        public string rtotalimp { get; set; }
        public string rtotalliteral { get; set; }
        public string rdsctosdescrip { get; set; }
        public string rtransporte { get; set; }
        public string rfletepor { get; set; }
        public string robs { get; set; }
        public string rfacturacion { get; set; }
        public string rpagocontadoanticipado { get; set; }
        public string ridanticipo { get; set; }
        public string rimprimir_etiqueta_cliente { get; set; }
        public string crfecha_hr_inicial { get; set; }
        public string crfecha_hr_autoriza { get; set; }
    }
}
