using Comercializacao.DTO;
using Comercializacao.Infra;

namespace Comercializacao.Servicos
{
    public class ServExemplo
    {
        private DataContext _dataContext;

        public ServExemplo()
        {
            _dataContext = GeradorDeServicos.CarregarContexto();
        }

        public ExemploDTO Exemplo(int id)
        {
            var exemploDto = new ExemploDTO()
            {
                Texto = "Hello world!"
            };

            return exemploDto;
        }
    }
}
