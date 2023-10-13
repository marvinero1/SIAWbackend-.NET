namespace siaw_DBContext.Models_Extra
{
    public class RequestValidacion
    {
        public DatosDocVta datosDocVta { get; set; }
        public List<vedetalleanticipoProforma> detalleAnticipos { get; set; }
        public List<vedesextraDatos> detalleDescuentos { get; set; }
        public List<vedetalleEtiqueta> detalleEtiqueta { get; set; }
        public List<itemDataMatriz> detalleItemsProf { get; set; }
        public List<verecargosDatos> detalleRecargos { get; set; }

    }
}
