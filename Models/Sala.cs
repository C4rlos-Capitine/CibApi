namespace CibApi.Models
{
    public class Sala
    {
        public int Id_Sala { get; set; }
        public string Codigo_Barra { get; set; } = string.Empty;
        public string Num_Sala { get; set; } = string.Empty;
        public bool IsSynced { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
