using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CibApi.Models;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace CibApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtigoController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ArtigoController> _logger;

        public ArtigoController(IConfiguration configuration, ILogger<ArtigoController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        // ✅ GET: api/artigo
        [HttpGet]
        public IActionResult GetAll()
        {
            var artigos = new List<Artigo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM artigo";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            artigos.Add(new Artigo
                            {
                                id_artigo = reader.GetGuid(reader.GetOrdinal("id_artigo")),
                                id_sala = reader.GetGuid(reader.GetOrdinal("id_sala")),
                                codigo_barra = reader.GetString(reader.GetOrdinal("codigo_barra")),
                                num_artigo = reader.GetString(reader.GetOrdinal("num_artigo")),
                                nome_artigo = reader.GetString(reader.GetOrdinal("nome_artigo")),
                                data_registo = reader.GetDateTime(reader.GetOrdinal("data_registo")),
                                data_update = reader.GetDateTime(reader.GetOrdinal("data_update")),
                                IsSynced = reader.GetBoolean(reader.GetOrdinal("isSynced")) ? 1 : 0,
                                LastUpdated = reader.GetDateTime(reader.GetOrdinal("lastUpdated"))
                            });
                        }
                    }
                }
            }

            return Ok(artigos);
        }

        // ✅ POST: api/artigo (um artigo)
        [HttpPost]
        public IActionResult Create([FromBody] Artigo artigo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"INSERT INTO artigo 
                                (id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, isSynced, lastUpdated)
                              VALUES 
                                (@id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, @isSynced, @lastUpdated)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id_sala", artigo.id_sala);
                    command.Parameters.AddWithValue("@codigo_barra", artigo.codigo_barra);
                    command.Parameters.AddWithValue("@num_artigo", artigo.num_artigo);
                    command.Parameters.AddWithValue("@nome_artigo", artigo.nome_artigo);
                    command.Parameters.AddWithValue("@data_registo", artigo.data_registo);
                    command.Parameters.AddWithValue("@data_update", artigo.data_update);
                    command.Parameters.AddWithValue("@isSynced", artigo.IsSynced);
                    command.Parameters.AddWithValue("@lastUpdated", artigo.LastUpdated);

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Artigo inserido com sucesso" });
        }

        // ✅ POST: api/artigo/lote (vários artigos de uma só vez)
        [HttpPost("lote")]
        public IActionResult CreateBatch([FromBody] List<Artigo> artigos)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var artigo in artigos)
                {
                    var query = @"INSERT INTO artigo 
                                    (id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, isSynced, lastUpdated)
                                  VALUES 
                                    (@id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, @isSynced, @lastUpdated)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id_sala", artigo.id_sala);
                        command.Parameters.AddWithValue("@codigo_barra", artigo.codigo_barra);
                        command.Parameters.AddWithValue("@num_artigo", artigo.num_artigo);
                        command.Parameters.AddWithValue("@nome_artigo", artigo.nome_artigo);
                        command.Parameters.AddWithValue("@data_registo", artigo.data_registo);
                        command.Parameters.AddWithValue("@data_update", artigo.data_update);
                        command.Parameters.AddWithValue("@isSynced", artigo.IsSynced);
                        command.Parameters.AddWithValue("@lastUpdated", artigo.LastUpdated);

                        command.ExecuteNonQuery();
                    }
                }
            }

            return Ok(new { message = $"{artigos.Count} artigos inseridos com sucesso" });
        }

        // ✅ POST: api/artigo/sync
        [HttpPost("sync")]
        public IActionResult Sync([FromBody] ArtigoSyncRequest request)
        {
            var artigos = request.artigos;
            try { 
                if (artigos == null || artigos.Count == 0)
                    return BadRequest(new { message = "Nenhum artigo recebido para sincronização" });

                int inseridos = 0, atualizados = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var artigo in artigos)
                    {
                        // 🔹 Verifica se já existe pelo código de barras
                        var checkQuery = "SELECT COUNT(*) FROM Artigo WHERE codigo_barra = @codigo_barra";

                        using (var checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@codigo_barra", artigo.codigo_barra);

                            int existe = (int)checkCmd.ExecuteScalar();

                            if (existe > 0)
                            {
                                // 🔹 Atualizar
                                var updateQuery = @"UPDATE Artigo
                                                    SET id_sala = @id_sala,
                                                        num_artigo = @num_artigo,
                                                        nome_artigo = @nome_artigo,
                                                        data_registo = @data_registo,
                                                        data_update = @data_update,
                                                        isSynced = 1,
                                                        lastUpdated = @lastUpdated
                                                    WHERE codigo_barra = @codigo_barra";

                                using (var updateCmd = new SqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@id_sala", artigo.id_sala);
                                    updateCmd.Parameters.AddWithValue("@num_artigo", artigo.num_artigo);
                                    updateCmd.Parameters.AddWithValue("@nome_artigo", artigo.nome_artigo);
                                    updateCmd.Parameters.AddWithValue("@data_registo", artigo.data_registo);
                                    updateCmd.Parameters.AddWithValue("@data_update", artigo.data_update);
                                    updateCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@codigo_barra", artigo.codigo_barra);

                                    updateCmd.ExecuteNonQuery();
                                    atualizados++;
                                }
                            }
                            else
                            {
                                // 🔹 Inserir
                                var insertQuery = @"INSERT INTO Artigo
                                                    (id_artigo, id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, isSynced, lastUpdated)
                                                    VALUES (@id_artigo, @id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, 1, @lastUpdated)";

                                using (var insertCmd = new SqlCommand(insertQuery, connection))
                                {
                                    insertCmd.Parameters.AddWithValue("@id_artigo", artigo.id_artigo);
                                    insertCmd.Parameters.AddWithValue("@id_sala", artigo.id_sala);
                                    insertCmd.Parameters.AddWithValue("@codigo_barra", artigo.codigo_barra);
                                    insertCmd.Parameters.AddWithValue("@num_artigo", artigo.num_artigo);
                                    insertCmd.Parameters.AddWithValue("@nome_artigo", artigo.nome_artigo);
                                    insertCmd.Parameters.AddWithValue("@data_registo", artigo.data_registo);
                                    insertCmd.Parameters.AddWithValue("@data_update", artigo.data_update);
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
                    total = artigos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation("\n\nExcepção geral " + ex.Message);
                return StatusCode(500, new { message = "Erro interno no servidor", erro = ex.Message });
            }
        }
    }
}
