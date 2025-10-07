using Microsoft.AspNetCore.Mvc;
using CibApi.Models;
using Microsoft.Data.SqlClient;

namespace CibApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<SalaController> _logger;

        public SalaController(IConfiguration configuration, ILogger<SalaController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        // ✅ GET: api/sala
        [HttpGet]
        public IActionResult GetAll()
        {
            var salas = new List<Sala>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Sala";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            salas.Add(new Sala
                            {
                               id_sala = reader.GetGuid(reader.GetOrdinal("id_sala")),
                                codigo_barra = reader.GetString(reader.GetOrdinal("codigo_barra")),
                                num_sala = reader.GetString(reader.GetOrdinal("num_sala")),
                                IsSynced = reader.GetBoolean(reader.GetOrdinal("isSynced")),
                                LastUpdated = reader.GetDateTime(reader.GetOrdinal("lastUpdated"))
                            });
                        }
                    }
                }
            }

            return Ok(salas);
        }

        // ✅ POST: api/sala
        [HttpPost]
        public IActionResult Create([FromBody] Sala sala)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"INSERT INTO Sala 
                                (codigo_barra, num_sala, isSynced, lastUpdated)
                              VALUES 
                                (@codigo_barra, @num_sala, @isSynced, @lastUpdated)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@codigo_barra", sala.codigo_barra);
                    command.Parameters.AddWithValue("@num_sala", sala.num_sala);
                    command.Parameters.AddWithValue("@isSynced", sala.IsSynced);
                    command.Parameters.AddWithValue("@lastUpdated", sala.LastUpdated);

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Sala inserida com sucesso" });
        }

        // ✅ POST: api/sala/lote (inserção em lote)
        [HttpPost("lote")]
        public IActionResult CreateBatch([FromBody] List<Sala> salas)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var sala in salas)
                {
                    var query = @"INSERT INTO Sala 
                                    (codigo_barra, num_sala, isSynced, lastUpdated)
                                  VALUES 
                                    (@codigo_barra, @num_sala, @isSynced, @lastUpdated)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@codigo_barra", sala.codigo_barra);
                        command.Parameters.AddWithValue("@num_sala", sala.num_sala);
                        command.Parameters.AddWithValue("@isSynced", sala.IsSynced);
                        command.Parameters.AddWithValue("@lastUpdated", sala.LastUpdated);

                        command.ExecuteNonQuery();
                    }
                }
            }

            return Ok(new { message = $"{salas.Count} salas inseridas com sucesso" });
        }

        // ✅ POST: api/sala/sync
        [HttpPost("sync")]
        public IActionResult Sync([FromBody] SalaSyncRequest request)
        {
            var salas = request.salas;
            if (salas == null || salas.Count == 0)
                return BadRequest(new { message = "Nenhuma sala recebida para sincronização" });

            int inseridos = 0, atualizados = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var sala in salas)
                {
                    // 🔹 Verifica se a sala já existe pelo código de barras
                    var checkQuery = "SELECT COUNT(*) FROM Sala WHERE codigo_barra = @codigo_barra";

                    using (var checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@codigo_barra", sala.codigo_barra);

                        int existe = (int)checkCmd.ExecuteScalar();

                        if (existe > 0)
                        {
                            // 🔹 Atualizar
                            var updateQuery = @"UPDATE Sala 
                                        SET num_sala = @num_sala, 
                                            isSynced = 1,
                                            lastUpdated = @lastUpdated
                                        WHERE codigo_barra = @codigo_barra";

                            using (var updateCmd = new SqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@num_sala", sala.num_sala);
                                updateCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);
                                updateCmd.Parameters.AddWithValue("@codigo_barra", sala.codigo_barra);

                                updateCmd.ExecuteNonQuery();
                                atualizados++;
                            }
                        }
                        else
                        {
                            // 🔹 Inserir
                            var insertQuery = @"INSERT INTO Sala 
                                        (id_sala, codigo_barra, num_sala, isSynced, lastUpdated) 
                                        VALUES (@id_sala, @codigo_barra, @num_sala, 1, @lastUpdated)";

                            using (var insertCmd = new SqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@id_sala", sala.id_sala);
                                insertCmd.Parameters.AddWithValue("@codigo_barra", sala.codigo_barra);
                                insertCmd.Parameters.AddWithValue("@num_sala", sala.num_sala);
                                insertCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);

                                insertCmd.ExecuteNonQuery();
                                inseridos++;
                            }
                        }
                    }
                }
            }

            return Ok(new
            {
                message = "Sincronização concluída",
                inseridos,
                atualizados,
                total = salas.Count
            });
        }

    }
}
