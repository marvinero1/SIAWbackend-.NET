using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_DBContext.Models_Extra
{
    public class SaveProformaCompleta
    {
        public veproforma veproforma { get; set; }
        public List<veproforma1> veproforma1 { get; set; }
        public List<veproforma_valida> veproforma_valida { get; set; }
        public List<veproforma_anticipo> veproforma_anticipo { get; set; }
        public List<vedesextraprof> vedesextraprof { get; set; }
        public List<verecargoprof> verecargoprof { get; set; }
        public List<veproforma_iva> veproforma_iva { get; set; }
    }
}
