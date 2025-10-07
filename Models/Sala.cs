namespace CibApi.Models
{
    public class Sala
    {
        public Guid id_sala { get; set; } = Guid.NewGuid();
        public string codigo_barra { get; set; } = string.Empty;
        public string num_sala { get; set; } = string.Empty;
        public bool IsSynced { get; set; } = false;
        public DateTime LastUpdated { get; set; }
    }
}
