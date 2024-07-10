using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_DBContext.Models_Extra
{
    public class SaveNRemisionCompleta
    {
        public veremision veremision{ get; set; }
        public List<veremision1> veremision1 { get; set; }
        public List<vedesextraremi>? vedesextraremi { get; set; }
        public List<verecargoremi>? verecargoremi { get; set; }
        public List<veremision_iva>? veremision_iva { get; set; }
        public veremision_chequerechazado? veremision_chequerechazado { get; set; }

    }
}
